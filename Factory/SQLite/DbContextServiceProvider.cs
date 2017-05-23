using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using SZORM.Infrastructure;

namespace SZORM.Factory.SQLite
{
    class DbContextServiceProvider : IDbContextServiceProvider
    {
        IDbConnectionFactory _dbConnectionFactory;

        public DbContextServiceProvider(IDbConnectionFactory dbConnectionFactory)
        {
            this._dbConnectionFactory = dbConnectionFactory;
        }
        public IDbConnection CreateConnection()
        {
            return this._dbConnectionFactory.CreateConnection();
        }
        public IDbExpressionTranslator CreateDbExpressionTranslator()
        {
            return DbExpressionTranslator.Instance;
        }

        public IStructure CreateStructureCheck()
        {
            return new SQLite.StructureToSQLite();
        }
    }
    class SQLiteConnectionFactory : IDbConnectionFactory
    {
        DbConfig _config = null;
        public SQLiteConnectionFactory(DbConfig config)
        {
            this._config = config;
        }
        public IDbConnection CreateConnection()
        {
            IDbConnection conn = DbProviderFactories.GetFactory(_config.ProviderName).CreateConnection();
            conn.ConnectionString = _config.ConnectionStr;
            return conn;
        }
    }
}
