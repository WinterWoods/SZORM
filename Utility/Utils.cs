using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SZORM.DbExpressions;
using SZORM.Utility;

namespace SZORM.Utility
{
    static class Utils
    {
        public static bool AreEqual(object obj1, object obj2)
        {
            if (obj1 == null && obj2 == null)
                return true;

            if (obj1 != null)
            {
                return obj1.Equals(obj2);
            }

            if (obj2 != null)
            {
                return obj2.Equals(obj1);
            }

            return object.Equals(obj1, obj2);
        }
        public static string GenerateUniqueColumnAlias(DbSqlQueryExpression sqlQuery, string defaultAlias = UtilConstants.DefaultColumnAlias)
        {
            string alias = defaultAlias;
            int i = 0;
            while (sqlQuery.ColumnSegments.Any(a => string.Equals(a.Alias, alias, StringComparison.OrdinalIgnoreCase)))
            {
                alias = defaultAlias + i.ToString();
                i++;
            }

            return alias;
        }
        public static Dictionary<TKey, TValue> Clone<TKey, TValue>(Dictionary<TKey, TValue> source)
        {
            Dictionary<TKey, TValue> ret = new Dictionary<TKey, TValue>(source.Count);

            foreach (var kv in source)
            {
                ret.Add(kv.Key, kv.Value);
            }

            return ret;
        }
        public static Task<T> MakeTask<T>(Func<T> func)
        {
            var task = new Task<T>(func);
            task.Start();
            return task;
        }
        public static string ToMethodString(this MethodInfo method)
        {
            StringBuilder sb = new StringBuilder();
            ParameterInfo[] parameters = method.GetParameters();

            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo p = parameters[i];

                if (i > 0)
                    sb.Append(",");

                string s = null;
                if (p.IsOut)
                    s = "out ";

                sb.AppendFormat("{0}{1} {2}", s, p.ParameterType.Name, p.Name);
            }

            return string.Format("{0}.{1}({2})", method.DeclaringType.Name, method.Name, sb.ToString());
        }
    }
}
