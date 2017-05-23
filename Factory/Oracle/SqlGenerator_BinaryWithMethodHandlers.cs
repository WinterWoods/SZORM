using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SZORM.DbExpressions;
using SZORM.Utility;

namespace SZORM.Factory.Oracle
{
    partial class SqlGenerator : DbExpressionVisitor<DbExpression>
    {
        static Dictionary<MethodInfo, Action<DbBinaryExpression, SqlGenerator>> InitBinaryWithMethodHandlers()
        {
            var binaryWithMethodHandlers = new Dictionary<MethodInfo, Action<DbBinaryExpression, SqlGenerator>>();
            binaryWithMethodHandlers.Add(UtilConstants.MethodInfo_String_Concat_String_String, StringConcat);
            binaryWithMethodHandlers.Add(UtilConstants.MethodInfo_String_Concat_Object_Object, StringConcat);

            var ret = Utils.Clone(binaryWithMethodHandlers);
            return ret;
        }

        static void StringConcat(DbBinaryExpression exp, SqlGenerator generator)
        {
            generator._sqlBuilder.Append("CONCAT(");
            exp.Left.Accept(generator);
            generator._sqlBuilder.Append(",");
            exp.Right.Accept(generator);
            generator._sqlBuilder.Append(")");
        }
    }
}
