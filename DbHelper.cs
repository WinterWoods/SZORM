using System;
using System.Text;
using System.Data.Common;
using System.Collections;
using System.Configuration;
using System.Data;

namespace SZORM
{
    /// <summary>
    /// 底层访问数据库类
    /// </summary>
    internal static class DBHelper
    {
        //带事物 无返回查询
        public static int ExcuteNoQuery(this SZTransaction trans, string cmdText, params DbParameter[] commandParameters)
        {
            return trans.ExcuteNoQuery(CommandType.Text, cmdText, commandParameters);
        }
        public static int ExcuteNoQuery(this SZTransaction trans, CommandType cmdType, string cmdText, params DbParameter[] commandParameters)
        {
            using (DbCommand cmd = trans.ProviderFactory.CreateCommand())
            {

                PrepareCommand(cmd, null, trans.Transaction, cmdType, cmdText, commandParameters);
                int val = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
                return val;
            }
        }
        //带事物
        public static object ExecuteScalar(this SZTransaction trans, string cmdText, params DbParameter[] commandParameters)
        {
            return trans.ExecuteScalar(CommandType.Text, cmdText, commandParameters);
        }
        public static object ExecuteScalar(this SZTransaction trans, CommandType cmdType, string cmdText, params DbParameter[] commandParameters)
        {
            using( DbCommand cmd = trans.ProviderFactory.CreateCommand())
            {
                PrepareCommand(cmd, null, trans.Transaction, cmdType, cmdText, commandParameters);
                object val = cmd.ExecuteScalar();
                cmd.Parameters.Clear();
                return val;
            }
            

        }
        //带事物
        public static DbDataReader ExceuteDataReader(this SZTransaction trans, string cmdText, params DbParameter[] commandParameters)
        {
            return trans.ExceuteDataReader(CommandType.Text, cmdText, commandParameters);
        }
        public static DbDataReader ExceuteDataReader(this SZTransaction trans, CommandType cmdType, string cmdText, params DbParameter[] commandParameters)
        {
            using(DbCommand cmd = trans.ProviderFactory.CreateCommand())
            {
                try
                {
                    PrepareCommand(cmd, null, trans.Transaction, cmdType, cmdText, commandParameters);
                    DbDataReader dataReader = cmd.ExecuteReader();
                    cmd.Parameters.Clear();
                    return dataReader;
                }
                catch
                {
                    throw;
                }
            }
            
        }

        public static DataTable ExceuteDataTable(this SZTransaction trans, string cmdText, params DbParameter[] commandParameters)
        {
            return trans.ExceuteDataTable(CommandType.Text, cmdText, commandParameters);
        }
        //带事物
        public static DataTable ExceuteDataTable(this SZTransaction trans, CommandType cmdType, string cmdText, params DbParameter[] commandParameters)
        {
            DataTable dataTable = new DataTable();

            using( DbCommand cmd = trans.ProviderFactory.CreateCommand())
            {
                using (DbDataAdapter dataAdapter = trans.ProviderFactory.CreateDataAdapter())
                {
                    PrepareCommand(cmd, null, trans.Transaction, cmdType, cmdText, commandParameters);
                    dataAdapter.SelectCommand = cmd;
                    dataAdapter.Fill(dataTable);
                }
            }
            return dataTable;

        }
        public static void BeginTrans(this SZTransaction trans)
        {
            trans.Transaction = trans.Connection.BeginTransaction();
        }
        public static void Commit(this SZTransaction trans)
        {
            trans.Transaction.Commit();
        }
        public static void RollBack(this SZTransaction trans)
        {
            trans.Transaction.Rollback();
        }
        private static void PrepareCommand(DbCommand cmd, DbConnection conn, DbTransaction trans, CommandType cmdType, string cmdText, DbParameter[] parms)
        {
            if (trans != null)
            {
                cmd.Transaction = trans;
                cmd.Connection = trans.Connection;
            }
            else
            {
                cmd.Connection = conn;

            }
            cmd.CommandText = cmdText;
            cmd.CommandType = cmdType;
            if (parms != null)
            {
                foreach (DbParameter parm in parms)
                {
                    if (parm.Value == null) parm.Value = DBNull.Value;
                    cmd.Parameters.Add(parm);
                }

            }
        }
    }
    public class SZConnectionConfig
    {
        public string ConnectionStr { get; set; }
        public string ProviderName { get; set; }
    }
    public class SZTransaction : IDisposable
    {
        public SZTransaction(string connectionStr, string providerName)
        {
            connectionConfig = new SZConnectionConfig();
            connectionConfig.ConnectionStr = connectionStr;
            connectionConfig.ProviderName = providerName;
            providerFactory = DbProviderFactories.GetFactory(providerName);
            connection = providerFactory.CreateConnection();
            connection.ConnectionString = connectionStr;
            connection.Open();
            
        }

        private DbConnection connection;
        /// <summary>
        /// 数据库连接
        /// </summary>
        internal DbConnection Connection
        {
            get { return connection; }
            set { connection = value; }
        }
        private DbTransaction transaction;
        /// <summary>
        /// 数据库事务
        /// </summary>
        internal DbTransaction Transaction
        {
            get { return transaction; }
            set { transaction = value; }
        }

        private DbProviderFactory providerFactory;
        /// <summary>
        /// 初始化工厂
        /// </summary>
        public DbProviderFactory ProviderFactory
        {
            get { return providerFactory; }
            set { providerFactory = value; }
        }
        private SZConnectionConfig connectionConfig;
        /// <summary>
        /// 连接字符串配置
        /// </summary>
        public SZConnectionConfig ConnectionConfig
        {
            get { return connectionConfig; }
            set { connectionConfig = value; }
        }

        public void Dispose()
        {
            providerFactory = null;
            transaction.Dispose();
            transaction = null;
            connection.Close();
            connection.Dispose();
            Connection = null;
        }
    }
}
