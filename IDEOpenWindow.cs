using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace oradev
{
    [Serializable]
    [XmlRootAttribute("OpenWindow")]
    public class IDEOpenWindow
    {
        private string _header;

        public string Header
        {
            get { return _header; }
            set { _header = value; }
        }

        private string _file;

        public string File
        {
            get { return _file; }
            set { _file = value; }
        }

        private string _text;

        public string Text
        {
            get { return _text; }
            set { _text = value; }
        }

        private string _encoding;

        public string Encoding
        {
            get { return _encoding; }
            set { _encoding = value; }
        }

        private string _database;

        public string Database
        {
            get { return _database; }
            set { _database = value; }
        }
    }
}
