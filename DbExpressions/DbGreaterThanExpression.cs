using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SZORM.Utility;

namespace SZORM.DbExpressions
{
    public class DbGreaterThanExpression : DbBinaryExpression
    {
        public DbGreaterThanExpression(DbExpression left, DbExpression right)
            : this(left, right, null)
        {

        }
        public DbGreaterThanExpression(DbExpression left, DbExpression right, MethodInfo method)
            : base(DbExpressionType.GreaterThan, UtilConstants.TypeOfBoolean, left, right, method)
        {

        }

        public override T Accept<T>(DbExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
