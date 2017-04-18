using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SZORM.Factory;

namespace SZORM.Factory
{
    internal class ExcuteSqlFactory
    {
        internal static IStructure Init(string ProviderName)
        {
            IStructure sql=null;
            if (ProviderName == "Oracle.DataAccess.Client")
            {
                sql = new StructureToOracle();
            }
            else if(ProviderName == "Oracle.ManagedDataAccess.Client")
            {
                sql = new StructureToOracle();
            }
            else if (ProviderName == "System.Data.SQLite")
            {
                //throw new Exception("暂不支持的数据库");
                sql = new StructureToSqlite();
            }
            else if (ProviderName == "MySql.Data.MySqlClient")
            {
                //throw new Exception("暂不支持的数据库");
                sql = new StructureToMySql();
            }
            else if (ProviderName == "System.Data.SqlClient")
            {
                //throw new Exception("暂不支持的数据库");
                sql = new StructureToMSSql();
            }
                
            else
            {
                throw new Exception("暂不支持的数据库");
            }
            return sql;
        }
    }
}
