using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oradev.Parser
{
    public class StructureElement
    {
        public enum ElementType { Document, Package, PackageBody, SQL, Procedure, Function, Type, SubType, Variable, Block, Loop }

        public ElementType Type = ElementType.Document;

        public string Display = "Document";

        public string Identifier = "Document";

        public List<StructureElement> Children = new List<StructureElement>();

        public Expression Expression;

        public int GetOffset()
        {
            if (Expression != null)
                if (Expression.Lexemes.Count > 0)
                    return Expression.Lexemes[0].Offset;
            return 0;
        }

        public void Reorder()
        {
            Children = Children.OrderBy(o => o.Identifier).ToList();
            foreach (StructureElement e in Children)
                e.Reorder();
        }
    }
}
