using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SZORM.Query.QueryExpressions
{
    class SelectExpression : QueryExpression
    {
        LambdaExpression _selector;
        public SelectExpression(Type elementType, QueryExpression prevExpression, LambdaExpression selector)
            : base(QueryExpressionType.Select, elementType, prevExpression)
        {
            this._selector = selector;
        }
        public LambdaExpression Selector
        {
            get { return this._selector; }
        }
        public override T Accept<T>(QueryExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
