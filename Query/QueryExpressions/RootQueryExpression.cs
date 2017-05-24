using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SZORM.Query.QueryExpressions
{
    class RootQueryExpression : QueryExpression
    {
        public RootQueryExpression(Type elementType)
            : base(QueryExpressionType.Root, elementType, null)
        {

        }

        public override T Accept<T>(QueryExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
