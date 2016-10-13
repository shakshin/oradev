using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace oradev.ObjCache
{
    [XmlRoot("Package")]
    public class CachePackage
    {
        public ObservableCollection<CacheMember> Members { get; set; }

        public string Name
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
            }
        }

        public DateTime TimeStamp
        {
            get
            {
                return timeStamp;
            }

            set
            {
                timeStamp = value;
            }
        }

        private string name;

        public CachePackage()
        {
            Members = new ObservableCollection<CacheMember>();
        }


        private DateTime timeStamp;
    }
}
