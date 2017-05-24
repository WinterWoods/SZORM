using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SZORM.DbExpressions;

namespace SZORM.Factory.Oracle
{
    public static class OracleSemantics
    {
        internal static readonly PropertyInfo PropertyInfo_ROWNUM = typeof(OracleSemantics).GetProperty("ROWNUM");
        internal static readonly DbMemberExpression DbMemberExpression_ROWNUM = DbExpression.MemberAccess(OracleSemantics.PropertyInfo_ROWNUM, null);

        public static decimal ROWNUM
        {
            get
            {
                throw new NotSupportedException();
            }
        }
    }
}
