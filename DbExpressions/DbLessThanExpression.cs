using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SZORM.Utility;

namespace SZORM.DbExpressions
{
    public class DbLessThanExpression : DbBinaryExpression
    {
        public DbLessThanExpression(DbExpression left, DbExpression right)
            : this(left, right, null)
        {

        }
        public DbLessThanExpression(DbExpression left, DbExpression right, MethodInfo method)
            : base(DbExpressionType.LessThan, UtilConstants.TypeOfBoolean, left, right, method)
        {

        }

        public override T Accept<T>(DbExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
