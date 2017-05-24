using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SZORM.Query.QueryExpressions
{
    class WhereExpression : QueryExpression
    {
        LambdaExpression _predicate;
        public WhereExpression(QueryExpression prevExpression, Type elementType, LambdaExpression predicate)
            : base(QueryExpressionType.Where, elementType, prevExpression)
        {
            this._predicate = predicate;
        }
        public LambdaExpression Predicate
        {
            get { return this._predicate; }
        }
        public override T Accept<T>(QueryExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
