using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace oradev
{
    [Serializable]
    [XmlRootAttribute("OracleDeveloperConfiguation")]
    public class Config
    {

        [NonSerialized]
        private string _dbalias;

        public string DataBaseAlias
        {
            get { return _dbalias; }
            set { _dbalias = value; }
        }

        [NonSerialized]
        private string _dbuser;

        public string DataBaseUser
        {
            get { return _dbuser; }
            set { _dbuser = value; }
        }

        [NonSerialized]
        private string _dbpassword;

        public string DataBasePassword
        {
            get { return _dbpassword; }
            set { _dbpassword = value; }
        }

        [NonSerialized]
        private string _fenc = Encoding.GetEncoding(866).WebName;

        public string FileEncoding
        {
            get { return _fenc; }
            set { _fenc = value; }
        }

        [NonSerialized]
        private Boolean _save_on_compile;

        public Boolean SaveOnCompile
        {
            get { return _save_on_compile; }
            set { _save_on_compile = value; }
        }

        private Boolean useObjectCache;

        private Int16 cacheExpirePeriod;

        public ObservableCollection<DataBaseConfig> Databases { get; set; }

        public ObservableCollection<TextMacro> Macros { get; set; }

        public bool UseObjectCache
        {
            get
            {
                return useObjectCache;
            }

            set
            {
                useObjectCache = value;
            }
        }

        public short CacheExpirePeriod
        {
            get
            {
                return cacheExpirePeriod;
            }

            set
            {
                cacheExpirePeriod = value;
            }
        }


        public Config()
        {
            Databases = new ObservableCollection<DataBaseConfig>();
            Macros = new ObservableCollection<TextMacro>();
            SaveOnCompile = false;
            UseObjectCache = true;
            CacheExpirePeriod = 2;
        }

        public static Config LoadFromFile()
        {
            string file = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "oradev-configuration.xml");
            try
            {
                if (File.Exists(file) && (new FileInfo(file)).Length > 0)
                {
                    XmlSerializer xml = new XmlSerializer(typeof(Config));
                    FileStream stream = new FileStream(file, FileMode.Open);
                    Config inst = new Config();
                    inst = (Config)xml.Deserialize(stream);
                    stream.Close();

                    foreach (DataBaseConfig db in inst.Databases)
                    {
                        if (db.Guid == null) db.Guid = Guid.NewGuid().ToString();
                    }


                    inst.SaveToFile();

                    return inst;
                }
                Config cfg = new Config();
                return cfg;
            }
            catch (Exception )
            {
                Config inst = new Config();
                return inst;
            }
        }

        public void SaveToFile()
        {
            for (int i = Databases.Count - 1; i >= 0; i--)
            {
                if (!string.IsNullOrEmpty(Databases[i].DataBaseName)) Databases[i].DataBaseName = Databases[i].DataBaseName.Trim();
                if (!string.IsNullOrEmpty(Databases[i].DataBaseAlias)) Databases[i].DataBaseAlias = Databases[i].DataBaseAlias.Trim();
                if (!string.IsNullOrEmpty(Databases[i].DataBaseUser)) Databases[i].DataBaseUser = Databases[i].DataBaseUser.Trim();
                if (string.IsNullOrEmpty(Databases[i].DataBaseName)) Databases[i].DataBaseName = "Untitled database";
                if (string.IsNullOrEmpty(Databases[i].DataBaseAlias))
                {
                    Databases.Remove(Databases[i]);
                    continue;
                }
            }
            XmlSerializer xml = new XmlSerializer(typeof(Config));
            StreamWriter stream = new StreamWriter(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "oradev-configuration.xml"));
            xml.Serialize(stream, this);
            stream.Close();
        }
    }
}
