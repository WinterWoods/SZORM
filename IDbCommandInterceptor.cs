using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SZORM
{
    public interface IDbCommandInterceptor
    {
        void ExecSql(string sql, TimeSpan timerSpan, params DbParameter[] parameters);
    }
}
