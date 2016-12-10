using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace oradev.Parser
{
    public class Parser
    {
        private enum State { Code, Comment, String, Number, Quoted };
        private string _source;
        private List<Lexeme> _lexems;
        private int _index;
        private string _buffer;
        private char _char;
        private State _state;

        private int _offset;

        public Parser(string src)
        {
            this._source = src;
            ParseLexems();
        }

        public List<Lexeme> GetLexemes()
        {
            return _lexems;
        }

        private void ParseLexems()
        {
            _lexems = new List<Lexeme>();
            _index = 0;
            _buffer = string.Empty;
            _state = State.Code;
            _offset = 0;

            while (_index < _source.Length)
            {
                _char = _source[_index];
                Check();
                _index++;               
            }
            if (_buffer != string.Empty)
            {
                _char = '\n';
                Check();
            }
        }

        private void Check()
        {
            if (_offset == -1 && !Regex.IsMatch(_char.ToString(), @"(\n|\s|\r)")) _offset = _index;
            switch (_state)
            {
                case State.Code:
                    CheckCode();
                    break;
                case State.Comment:
                    CheckComment();
                    break;
                case State.Number:
                    CheckNumber();
                    break;
                case State.Quoted:
                    CheckQuoted();
                    break;
                case State.String:
                    CheckString();
                    break;
            }
            
        }

        private bool IsCommentStarting()
        {
            if (_source.Length > _index+1)
            {
                if (_char == '/' && _source[_index + 1] == '*' || _char == '-' && _source[_index + 1] == '-')
                    return true;
            }
            return false;
        }
        
        private void CheckCode()
        {
            if (Regex.IsMatch(_char.ToString(), @"(\n|\s|\r)"))
            {
                BufferToLexeme(false);
            }
            else if (Regex.IsMatch(_char.ToString(), @"\d") && _buffer == string.Empty)
            {
                _buffer += _char;
                _state = State.Number;
                _offset = _index;
            }
            else if (IsCommentStarting())
            {
                BufferToLexeme(true);
                _buffer += _char;
                _state = State.Comment;
                _offset = _index;
            }
            else if (_char == '\'' )
            {
                BufferToLexeme(false);
                _state = State.String;
                _offset = _index;
            }
            else if (_char == '"')
            {
                BufferToLexeme(false);
                _state = State.Quoted;
            }
            else if (_char == '.' || _char == ',' || _char == '%' || _char == '(' || _char == ')' || _char == ';')
            {
                BufferToLexeme(false);
                _offset = _index;
                _buffer += _char;
                BufferToLexeme(false);
            }
            else
            {
                _buffer += _char;
            }
        }

        private void CheckComment()
        {
            if ((_char == '\n' || _char == '\r')  && _buffer[0] == '-')
            {
                BufferToLexeme(false);
                _state = State.Code;
            }
            else if (_char == '/' && _buffer[_buffer.Length - 1] == '*' && _buffer[0] == '/')
            {
                BufferToLexeme(true);
                _state = State.Code;
            }
            else
            {
                _buffer += _char;
            }
        }

        private void CheckNumber()
        {
            if (Regex.IsMatch(_char.ToString(), @"(\d|\.)"))
            {
                _buffer += _char;
            }
            else
            {
                BufferToLexeme(false);
                _state = State.Code;
                _offset = _index;
                CheckCode();
            }
        }

        private void CheckQuoted()
        {
            if (_char == '"')
            {
                BufferToLexeme(false);
                _state = State.Code;
            }
            else
            {
                _buffer += _char;
            }
        }

        private bool IsStringEnd()
        {
            if (_char == '\'')
            {
                if (_source.Length > _index + 1) {
                    if (_source[_index + 1] != '\'')
                        return true;
                }
            }
            return false;
        }

        private void CheckString()
        {
            if (IsStringEnd())
            {
                BufferToLexeme(false);
                _state = State.Code;
            }
            else
            {
                _buffer += _char;
            }
        }

        private void BufferToLexeme(bool includeChar)
        {
            if (_buffer == string.Empty)
            {
                _offset = -1;
                return;
            }
            if (includeChar) _buffer += _char;

            Lexeme.LexemeType t = Lexeme.LexemeType.Code;
            switch (_state)
            {
                case State.Code:
                    t = Lexeme.LexemeType.Code;
                    break;
                case State.Comment:
                    t = Lexeme.LexemeType.Comment;
                    break;
                case State.Number:
                    t = Lexeme.LexemeType.Number;
                    break;
                case State.Quoted:
                    t = Lexeme.LexemeType.Quoted;
                    break;
                case State.String:
                    t = Lexeme.LexemeType.String;
                    break;
            }

            _lexems.Add(new Lexeme() {
                Type = t,
                Content = _buffer,
                Offset = _offset
            });
            _buffer = string.Empty;
            _offset = -1;
        }
    }
}
