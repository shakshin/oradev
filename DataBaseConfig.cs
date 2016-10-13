using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Timers;
using System.Xml.Serialization;

namespace oradev
{
    [Serializable]
    [XmlRootAttribute("DataBase")]
    public class DataBaseConfig
    {
        [NonSerialized]
        private string _dbname;

        public string DataBaseName
        {
            get { return _dbname; }
            set { _dbname = value; }
        }

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

        public string Guid
        {
            get
            {
                return guid;
            }

            set
            {
                guid = value;
            }
        }

        private string guid;

        public event EventHandler ObjectsLoaded;

        [XmlIgnore]
        public ObservableCollection<DBObject> objs = new ObservableCollection<DBObject>();

        public void ReCache()
        {
            Oracle.GetObjectsAsync("", delegate (ObservableCollection<DBObject> result) {
                objs = result;
                if (ObjectsLoaded != null) ObjectsLoaded(this, null);
            }, this);
        }

        private int RecacheCounter = 0;
        public DataBaseConfig()
        {
            ReCache();
            Timer timer = new Timer(60 * 1000);
            timer.Elapsed += delegate (object sender, ElapsedEventArgs e)
            {
                RecacheCounter++;
                if (RecacheCounter >= (App.Current as App).Configuration.CacheExpirePeriod)
                    ReCache();
            };
            timer.Start();
        }

    }
}
