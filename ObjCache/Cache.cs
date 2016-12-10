using oradev.Parser;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Serialization;

namespace oradev.ObjCache
{
    [XmlRoot("ObjectCache")]
    public class Cache
    {
        private AutoResetEvent TaskEvent = new AutoResetEvent(false);

        public delegate void CachePackageCallback(CachePackage pkg);

        public ObservableCollection<CacheConnection> Connections { get; set; }

        private DateTime TimeStamp = DateTime.Now;

        private Thread Worker;

        public Cache()
        {
            Connections = new ObservableCollection<CacheConnection>();
        }

        private ConcurrentQueue<CacheTask> Queue = new ConcurrentQueue<CacheTask>();

        public void RunWorker()
        {
            Worker = new Thread(delegate () {
                try
                {
                    while (true)
                    {
                        CacheTask task;
                        if (Queue.TryDequeue(out task))
                        {
                            CacheConnection conn = GetCacheConnection(task.DataBase);
                            CachePackage pkg = GetCachePackage(task.PackageName, conn, false);
                            if (pkg == null)
                            {
                                UpdatePackageData(task.DataBase, task.PackageName, task.Callback);
                            }
                            else
                            {
                                InvokeCallback(task.Callback, pkg);
                                if (pkg.TimeStamp.AddHours((App.Current as App).Configuration.CacheExpirePeriod) < DateTime.Now)
                                {
                                    UpdatePackageData(task.DataBase, task.PackageName, null);
                                }
                            }
                            
                        }
                        else
                        {
                            SaveToFile(true);
                            TaskEvent.WaitOne();
                        }
                    }
                }
                catch (ThreadAbortException) {  }
            });
            Worker.Start();
        }

        public static Cache LoadFromFile()
        {
            string file = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "oradev-cache.xml");
            try
            {
                if (File.Exists(file) && (new FileInfo(file)).Length > 0)
                {
                    XmlSerializer xml = new XmlSerializer(typeof(Cache));
                    FileStream stream = new FileStream(file, FileMode.Open);
                    Cache inst = new Cache();
                    inst = (Cache)xml.Deserialize(stream);
                    stream.Close();
                    return inst;
                }
                return new Cache(); ;
            }
            catch (Exception)
            {
                return new Cache();
            }
        }

        private void SaveToFile(Boolean force = false)
        {
            if (TimeStamp.AddMinutes(1) > DateTime.Now && ! force) return;
            TimeStamp = DateTime.Now;
            XmlSerializer xml = new XmlSerializer(typeof(Cache));
            StreamWriter stream = new StreamWriter(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "oradev-cache.xml"));
            xml.Serialize(stream, this);
            stream.Close();
        }

        private DataBaseConfig GetDBConfig(String guid)
        {
            Config cfg = (App.Current as App).Configuration;
            foreach (DataBaseConfig x in cfg.Databases)
            {
                if (x.Guid == guid) return x;
            }
            return null;
        }

        private CacheConnection GetCacheConnection(String guid)
        {
            foreach (CacheConnection c in Connections) if (c.Guid == guid) return c;
             
            CacheConnection _conn = new CacheConnection()
            {
                Guid = guid
            };

            Connections.Add(_conn);

            return _conn;
        }

        private CachePackage GetCachePackage(String package, CacheConnection connection, Boolean createNew = true)
        {
            foreach (CachePackage p in connection.Packages)
                if (p.Name.ToUpper() == package.ToUpper()) return p;

            if (!createNew) return null;

            CachePackage _package = new CachePackage()
            {
                Name = package
            };
            connection.Packages.Add(_package);
            return _package;
            
        }
                
        private void InvokeCallback(CachePackageCallback callback, CachePackage result)
        {
            if (callback != null) App.Current.Dispatcher.Invoke((Action)delegate
            {
                try
                {
                    callback(result);
                }
                catch (Exception) { }
            });
        }

        private void UpdatePackageData(string connection, string package, CachePackageCallback callback)
        {
            Config cfg = (App.Current as App).Configuration;

            DataBaseConfig db = GetDBConfig(connection);
            if (db == null) {
                InvokeCallback(callback, null);
                return;
            }

            String text = Oracle.GetPackageHead(package, db);

            text = Regex.Replace(text, @"(--.*)|(((/\*)+?[\w\W]+?(\*/)+))", "");
            text = Regex.Replace(text, @"\s+", " ");

            if (text == "")
            {
                InvokeCallback(callback, null);
                return;
            }

            CacheConnection _conn = GetCacheConnection(connection);
            CachePackage _package = GetCachePackage(package, _conn);

            int cnt = 0;

            Parser.Parser parser = new Parser.Parser(text);
            Parser.StructureParser sparser = new Parser.StructureParser(parser.GetLexemes());
            if (sparser.GetStructure().Children.Count > 0)
                if (sparser.GetStructure().Children[0].Type == Parser.StructureElement.ElementType.Package)
                    foreach (StructureElement elem in sparser.GetStructure().Children[0].Children)
                    {
                        if (cnt == 0)
                        {
                            _package.TimeStamp = DateTime.Now;
                            _package.Members.Clear();
                        }
                        string prot = string.Empty;
                        int i = 2;
                        while (i < elem.Expression.Lexemes.Count)
                        {
                            if (prot != string.Empty) prot += "";
                            prot += elem.Expression.Lexemes[i];
                            i++;
                        }
                        CacheMember method = new CacheMember()
                        {
                            Type = elem.Type.ToString(),
                            Name = elem.Identifier,
                            Prototype = prot
                        };
                        _package.Members.Add(method);
                        cnt++;
                    }

            InvokeCallback(callback, _package);
            SaveToFile();
        }

        public void GetPackageCache(string connection, string package, CachePackageCallback callback)
        {
            if (!(App.Current as App).Configuration.UseObjectCache) {
                if (callback != null)  callback(null);
                return;
            };
            CacheTask task = new CacheTask()
            {
                DataBase = connection,
                PackageName = package,
                Callback = callback
            };
            Queue.Enqueue(task);
            TaskEvent.Set();
        } 

        public void StopWorker()
        {
            Worker.Abort();
        }
    }
}
