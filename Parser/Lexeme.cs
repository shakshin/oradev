using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oradev.Parser
{
    public class Lexeme
    {
        public enum LexemeType { Code, Number, String, Quoted, Comment };

        public LexemeType Type;

        public int Offset;

        public string Content;
    }
}
