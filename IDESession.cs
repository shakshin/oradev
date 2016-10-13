using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace oradev
{
    [Serializable]
    [XmlRootAttribute("OracleDeveloperSessionData")]
    public class IDESession
    {
        public ObservableCollection<IDEOpenWindow> Windows { get; set; }

        public IDESession()
        {
            Windows = new ObservableCollection<IDEOpenWindow>();
        }

        public static IDESession LoadFromFile()
        {
            string file = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "oradev-session.xml");
            //string file = System.IO.Path.GetDirectoryName(Application.ResourceAssembly.Location) + "\\session.xml";
            try
            {
                if (File.Exists(file) && (new FileInfo(file)).Length > 0)
                {
                    XmlSerializer xml = new XmlSerializer(typeof(IDESession));
                    FileStream stream = new FileStream(file, FileMode.Open);
                    IDESession inst = new IDESession();
                    inst = (IDESession)xml.Deserialize(stream);
                    stream.Close();
                    return inst;
                }
                return new IDESession();
            }
            catch (Exception )
            {
                return new IDESession();
            }
        }

        public static void Restore()
        {
            IDESession inst = IDESession.LoadFromFile();
            foreach (IDEOpenWindow wnd in inst.Windows)
            {
                DataBaseConfig db = null;
                foreach (DataBaseConfig cfg in (App.Current as App).Configuration.Databases)
                {
                    if (cfg.DataBaseName == wnd.Database)
                    {
                        db = cfg;
                        break;
                    }
                }
                CustomTab tab = (App.Current.MainWindow as MainWindow).NewTab(false, db);
                if (string.IsNullOrEmpty(wnd.File))
                {
                    (tab.Header as CustomTabHeader).Title = wnd.Header;
                    (tab.Content as SQLEdit).txtCode.Text = wnd.Text;
                    (tab.Content as SQLEdit).txtCode.IsModified = false;
                }
                else
                {
                    try
                    {
                        tab.LoadFile(wnd.File, Encoding.GetEncoding(wnd.Encoding));
                        (tab.Content as SQLEdit).txtCode.IsModified = false;
                    }
                    catch (Exception )
                    {
                        (tab.Header as CustomTabHeader).Title = wnd.Header;
                        (tab.Content as SQLEdit).txtCode.Text = wnd.Text;
                        (tab.Content as SQLEdit).txtCode.IsModified = true;
                    }
                }
                tab.SaveFile = wnd.File;
                try
                {
                    tab.FileEncoding = Encoding.GetEncoding(wnd.Encoding);
                }
                catch (Exception)
                {
                    tab.FileEncoding = Encoding.GetEncoding(866);
                }
                
                (tab.Content as SQLEdit).InitTagsRescan();
            }
        }

        public static void Save()
        {
            App.Current.Dispatcher.Invoke((Action)delegate {
                IDESession inst = new IDESession();
                foreach (CustomTab tab in (Application.Current.MainWindow as MainWindow)._tabs.Items)
                {
                    IDEOpenWindow wnd = new IDEOpenWindow();

                    wnd.Header = (tab.Header as CustomTabHeader).Title;
                    wnd.Text = (tab.Content as SQLEdit).txtCode.Text;
                    wnd.File = tab.SaveFile;
                    wnd.Encoding = tab.FileEncoding.WebName;
                    if ((tab.Content as SQLEdit).dbconfig.SelectedItem != null)
                        wnd.Database = ((tab.Content as SQLEdit).dbconfig.SelectedItem as DataBaseConfig).DataBaseName;

                    inst.Windows.Add(wnd);
                }

                XmlSerializer xml = new XmlSerializer(typeof(IDESession));
                StreamWriter stream = new StreamWriter(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "oradev-session.xml"));
                xml.Serialize(stream, inst);
                stream.Close();
            });
        }
    }
}
