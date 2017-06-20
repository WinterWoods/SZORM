using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace SZORM
{
    public interface IDbContext:IDisposable
    {
        void Save();
        void Rollback();

        int ExecuteNoQuery(string cmdText, params DbParam[] parameters);
        int ExecuteNoQuery(string cmdText, CommandType cmdType, params DbParam[] parameters);

        object ExecuteScalar(string cmdText, params DbParam[] parameters);
        object ExecuteScalar(string cmdText, CommandType cmdType, params DbParam[] parameters);

        IDataReader ExecuteReader(string cmdText, params DbParam[] parameters);
        IDataReader ExecuteReader(string cmdText, CommandType cmdType, params DbParam[] parameters);

        DataTable ExecuteDataTable(string cmdText, params DbParam[] parameters);
        DataTable ExecuteDataTable(string cmdText, CommandType cmdType, params DbParam[] parameters);

        IEnumerable<T> ExecuteSqlToList<T>(string cmdText, params DbParam[] parameters);
        IEnumerable<T> ExecuteSqlToList<T>(string cmdText, CommandType cmdType, params DbParam[] parameters);
    }
}
