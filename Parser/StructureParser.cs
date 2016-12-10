using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace oradev.Parser
{
    public class StructureParser
    {
        private enum State { Document, Package, PackageBody, Procedure, Function, ProcedureBody, FunctionBody, Block, Loop, If, SQL}
        private List<Lexeme> _source;
        private StructureElement _doc;
        private int _index;
        private List<Expression> _expressions;

        public StructureParser(List<Lexeme> src)
        {
            _source = new List<Lexeme>();
            _expressions = new List<Expression>();
            for (int i = 0; i < src.Count; i++)
                if (src[i].Type != Lexeme.LexemeType.Comment)
                    _source.Add(src[i]);

            Parse();
        }

       public StructureElement GetStructure()
        {
            return _doc;
        }

        public List<Expression> GetExpressions()
        {
            return _expressions;
        }

        public List<Lexeme> GetDryLexemes()
        {
            return _source;
        }

        private void Parse()
        {
            while (_index < _source.Count)
                _expressions.Add(GetExpression(true));
            ParseDocument();
        }

        private bool IsEndOfExpression(Lexeme lex, List<Lexeme> lexemes)
        {
            if (lex.Type != Lexeme.LexemeType.Code) return false;
            string content = lex.Content.ToUpper();

            switch (lex.Content.ToUpper())
            {
                case ";":
                case "THEN":
                case "ELSE":
                case "DECLARE":
                case "BEGIN":
                    return true;
                case "LOOP":
                    if (lexemes.Count > 1)
                    {
                        if (lexemes[lexemes.Count - 2].Content.ToUpper() == "END")
                            return false;
                    }
                    return true;
                case "AS":
                case "IS":
                    if (lexemes[0].Content.ToUpper() == "TYPE")
                        return false;
                    if (lexemes[0].Content.ToUpper() == "SUBTYPE")
                        return false;
                    return true;
                
                    
            }

            return false;
        }

        private Expression GetExpression(bool shiftIndex)
        {
            Expression e = new Expression();
            int i = _index;
            string result = string.Empty;
            List<Lexeme> lx = new List<Lexeme>();
            while (i < _source.Count)
            {
                Lexeme lex = _source[i];
                string add = string.Empty;
                if (lex.Type == Lexeme.LexemeType.String) add = "'";
                if (lex.Type == Lexeme.LexemeType.Quoted) add = "\"";
                add += lex.Content;
                if (lex.Type == Lexeme.LexemeType.String) add += "'";
                if (lex.Type == Lexeme.LexemeType.Quoted) add += "\"";

                if (lex.Type == Lexeme.LexemeType.Code) add = add.ToUpper();

                if (result != string.Empty) result += " ";
                result += add;

                lx.Add(lex);

                i++;
                if (shiftIndex) _index++;

                if (IsEndOfExpression(lex, lx)) break;
            }
            e.Content = result;
            e.Lexemes = lx;
            return e;
        }

        private Expression GetNextExpression()
        {
            return _expressions[_index++];
        }

        private bool CheckBeginsWith(Expression exp, string begin)
        {
            return Regex.IsMatch(exp.Content, "^" + begin, RegexOptions.IgnoreCase);
        }

        private bool CheckContains(Expression exp, string str)
        {
            return Regex.IsMatch(exp.Content, str, RegexOptions.IgnoreCase);
        }

        private void ParseDocument()
        {
            _doc = new StructureElement()
            {
                Display = "Document",
                Type = StructureElement.ElementType.Document
            };
            _index = 0;
            while (_index < _expressions.Count)
            {
                Expression e = GetNextExpression();
                if (CheckBeginsWith(e, "create or replace package body"))
                    _doc.Children.Add(ParsePackageBody(e));
                else if (CheckBeginsWith(e, "create package body"))
                    _doc.Children.Add(ParsePackageBody(e));
                else if (CheckBeginsWith(e, "create or replace package"))
                    _doc.Children.Add(ParsePackage(e));
                else if (CheckBeginsWith(e, "create package"))
                    _doc.Children.Add(ParsePackage(e));
                else if (CheckBeginsWith(e, "select"))
                    _doc.Children.Add(AddAsSQL(e));
                else if (CheckBeginsWith(e, "update"))
                    _doc.Children.Add(AddAsSQL(e));
                else if (CheckBeginsWith(e, "delete"))
                    _doc.Children.Add(AddAsSQL(e));
                else if (CheckBeginsWith(e, "insert"))
                    _doc.Children.Add(AddAsSQL(e));
            }
        }

        private StructureElement AddAsSQL(Expression e)
        {
            StructureElement sql = new StructureElement();
            sql.Display = "SQL";
            sql.Type = StructureElement.ElementType.SQL;
            sql.Expression = e;
            return sql;
        }

        private StructureElement ParsePackageBody(Expression e)
        {
            StructureElement p = new StructureElement();
            p.Type = StructureElement.ElementType.PackageBody;
            p.Identifier = e.Lexemes[e.Lexemes.Count - 2].Content;
            p.Display = "Package body " + p.Identifier;
            p.Expression = e;
            return ParsePkg(p);
        }

        private StructureElement ParsePackage(Expression e)
        {
            StructureElement p = new StructureElement();
            p.Type = StructureElement.ElementType.Package;
            p.Identifier = e.Lexemes[e.Lexemes.Count - 2].Content;
            p.Display = "Package head " + p.Identifier;
            p.Expression = e;
            return ParsePkg(p);
        }

        private StructureElement ParsePkg(StructureElement pkg)
        {
            while (_index < _expressions.Count)
            {
                Expression e = GetNextExpression();
                if (CheckBeginsWith(e, "TYPE"))
                {
                    StructureElement p = new StructureElement();
                    p.Type = StructureElement.ElementType.Type;
                    p.Identifier = e.Lexemes[1].Content;
                    p.Display = p.Identifier + " (type)";
                    p.Expression = e;
                    pkg.Children.Add(p);
                }
                else if (CheckBeginsWith(e, "SUBTYPE"))
                {
                    StructureElement p = new StructureElement();
                    p.Type = StructureElement.ElementType.SubType;
                    p.Identifier = e.Lexemes[1].Content;
                    p.Display = p.Identifier + " (subtype)";
                    p.Expression = e;
                    pkg.Children.Add(p);
                }
                else if (CheckBeginsWith(e, "FUNCTION"))
                {
                    StructureElement p = new StructureElement();
                    p.Type = StructureElement.ElementType.Function;
                    p.Identifier = e.Lexemes[1].Content;
                    p.Display = p.Identifier + " (function)";
                    p.Expression = e;
                    pkg.Children.Add(ParseBlk(p));
                }
                else if (CheckBeginsWith(e, "PROCEDURE"))
                {
                    StructureElement p = new StructureElement();
                    p.Type = StructureElement.ElementType.Function;
                    p.Identifier = e.Lexemes[1].Content;
                    p.Display = p.Identifier + " (procedure)";
                    p.Expression = e;
                    pkg.Children.Add(ParseBlk(p));
                }
            }
            return pkg;
        }

        private StructureElement ParseBlk(StructureElement b)
        {

            return b;
        }
    }
}
