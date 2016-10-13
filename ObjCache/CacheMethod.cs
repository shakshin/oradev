using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace oradev.ObjCache
{
    [XmlRoot("Member")]
    public class CacheMember
    {
        private string name;

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

        public string Prototype
        {
            get
            {
                return prototype;
            }

            set
            {
                prototype = value;
            }
        }

        public string Type
        {
            get
            {
                return type;
            }

            set
            {
                type = value;
            }
        }

        private string prototype;

        private string type;
    }
}
