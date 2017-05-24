using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SZORM.Infrastructure;
using SZORM.Utility;

namespace SZORM.Factory
{
    internal class DbContextServiceProviderFactory
    {
        public static IDbContextServiceProvider CreateDbConnection(DbConfig config)
        {
            if (config.ProviderName == "MySql.Data.MySqlClient")
            {
                return new MySql.DbContextServiceProvider(new MySql.MySqlConnectionFactory(config));
            }
            else if (config.ProviderName == "Oracle.ManagedDataAccess.Client")
            {
                return new Oracle.DbContextServiceProvider(new Oracle.OracleConnectionFactory(config));
            }
            else if (config.ProviderName == "System.Data.SQLite")
            {
                return new SQLite.DbContextServiceProvider(new SQLite.SQLiteConnectionFactory(config));
            }
            else if (config.ProviderName == "System.Data.SqlClient")
            {
                return new SqlServer.DbContextServiceProvider(new SqlServer.SqlServerConnectionFactory(config));
            }
            else
            {
                throw new Exception("暂不支持的数据库");
            }
        }
    }
}
