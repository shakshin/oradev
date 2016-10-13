using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace oradev.ObjCache
{
    [XmlRoot("Connection")]
    public class CacheConnection
    {
        private string guid;

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

        public ObservableCollection<CachePackage> Packages { get; set; }

        public CacheConnection()
        {
            Packages = new ObservableCollection<CachePackage>();
        }
    }
}
