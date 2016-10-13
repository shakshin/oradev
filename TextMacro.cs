using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace oradev
{
    [Serializable]
    [XmlRootAttribute("OracleDeveloperConfiguation")]
    public class TextMacro
    {
        private string _key = string.Empty;
        public string Key
        {
            get { return _key; }
            set { _key = value; }
        }

        private bool _control = false;
        public bool Control
        {
            get { return _control; }
            set { _control = value; }
        }

        private bool _shift = false;
        public bool Shift
        {
            get { return _shift; }
            set { _shift = value; }
        }

        private string _text = string.Empty;
        public string Text
        {
            get { return _text; }
            set { _text = value; }
        }

        private bool _atEOL = false;

        public bool AtEOL
        {
            get { return _atEOL; }
            set { _atEOL = value; }
        }

    }
}
