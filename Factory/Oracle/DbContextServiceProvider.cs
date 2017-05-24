using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using SZORM.Infrastructure;

namespace SZORM.Factory.Oracle
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
            var conn = this._dbConnectionFactory.CreateConnection();
            if ((conn is OracleConnection) == false)
                conn = new OracleConnection(conn);
            return conn;
        }

        public IDbExpressionTranslator CreateDbExpressionTranslator()
        {
            return DbExpressionTranslator.Instance;
        }

        public IStructure CreateStructureCheck()
        {
            return new Oracle.StructureToOracle();
        }
    }
    class OracleConnectionFactory : IDbConnectionFactory
    {
        DbConfig _config = null;
        public OracleConnectionFactory(DbConfig config)
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
