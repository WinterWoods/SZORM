using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SZORM.Utility;

namespace SZORM.DbExpressions
{
    public class DbCoalesceExpression : DbExpression
    {
        public DbCoalesceExpression(DbExpression checkExpression, DbExpression replacementValue)
            : base(DbExpressionType.Coalesce, replacementValue.Type)
        {
            Checks.NotNull(checkExpression, "checkExpression");
            Checks.NotNull(replacementValue, "replacementValue");

            this.CheckExpression = checkExpression;
            this.ReplacementValue = replacementValue;
        }

        public DbExpression CheckExpression { get; private set; }
        public DbExpression ReplacementValue { get; private set; }

        public override T Accept<T>(DbExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
