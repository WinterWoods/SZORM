using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using SZORM.Core;
using SZORM.Core.Emit;
using SZORM.Descriptors;
using SZORM.Factory;
using SZORM.Infrastructure;
using SZORM.InternalExtensions;
using SZORM.Query.Internals;
using SZORM.Utility;

namespace SZORM
{
    public abstract partial  class DbContext : IDbContext
    {
        
        bool _disposed = false;
        internal InternalAdoSession _internalAdoSession;
        internal DbConfig _dbConfig;
        IStructure _dbStructCheck;
        internal ConcurrentDictionary<Type, TypeDescriptor> _typeDescriptors;
        IsolationLevel _il;

        internal IDbContextServiceProvider _dbContextServiceProvider;
        /// <summary>
        /// 默认打开事物处理
        /// </summary>
        public DbContext(IsolationLevel il=IsolationLevel.ReadCommitted)
        {
            Type type = GetType();
            var obj = Cache.Get(type.Name);
            if (obj == null)
            {
                ConnectionStringSettingsCollection settings = ConfigurationManager.ConnectionStrings;
                if (settings[type.Name] == null) throw new Exception("没有配置连接字符串!");
                Cache.Add(type.Name,settings);
                obj = settings;
            }
            ConnectionStringSettingsCollection ConnSetting = (ConnectionStringSettingsCollection)obj;
            //获取到连接字符串
            Init(ConnSetting[type.Name].ConnectionString, ConnSetting[type.Name].ProviderName, il);
        }
        public DbContext(string settingName, IsolationLevel il = IsolationLevel.ReadCommitted)
        {
            var obj = Cache.Get(settingName);
            if (obj == null)
            {
                ConnectionStringSettingsCollection settings = ConfigurationManager.ConnectionStrings;
                if (settings[settingName] == null) throw new Exception("没有配置连接字符串!");
                Cache.Add(settingName, settings);
                obj = settings;
            }
            ConnectionStringSettingsCollection ConnSetting = (ConnectionStringSettingsCollection)obj;
            //获取到连接字符串
            Init(ConnSetting[settingName].ConnectionString, ConnSetting[settingName].ProviderName, il);
        }
        public DbContext(string ConnectionStr, string ProviderName, IsolationLevel il = IsolationLevel.ReadCommitted)
        {
            //直接进行初始化
            Init(ConnectionStr, ProviderName, il);
        }
        private void Init(string ConnectionStr, string ProviderName, IsolationLevel il)
        {
            _il = il;
            Checks.NotNull(ConnectionStr, "ConnectionStr");
            Checks.NotNull(ProviderName, "ProviderName");
            //开始初始化
            _dbConfig = new DbConfig();
            _dbConfig.ConnectionStr = ConnectionStr;
            _dbConfig.ProviderName = ProviderName;

            //开始缓存数据结构,并初始化dbset
            _typeDescriptors = TypeDescriptors.GetTypeDescriptors(this);

            this._dbContextServiceProvider = DbContextServiceProviderFactory.CreateDbConnection(_dbConfig);
            //初始化数据链接
            InitDb();
            //初始化数据库结构
            InternalAdoSession.BeginTransaction(_il);
        }
        
        



        internal InternalAdoSession InternalAdoSession
        {
            get
            {
                this.CheckDisposed();
                if (this._internalAdoSession == null)
                {
                    //如果为空,就去初始化工厂进行初始化
                    this._internalAdoSession = new InternalAdoSession(this._dbContextServiceProvider.CreateConnection());
                }
                return this._internalAdoSession;
            }
        }

        public void Dispose()
        {
            this._internalAdoSession.Dispose();
        }
        public void SetCommandTimeout(int time)
        {
            InternalAdoSession.CommandTimeout = time;
        }
        public int ExecuteNoQuery(string cmdText, params DbParam[] parameters)
        {
            return ExecuteNoQuery(cmdText, CommandType.Text, parameters);
        }

        public int ExecuteNoQuery(string cmdText, CommandType cmdType, params DbParam[] parameters)
        {
            Checks.NotNull(cmdText, "cmdText");
            return InternalAdoSession.ExecuteNonQuery(cmdText, parameters, cmdType);
        }

        public IDataReader ExecuteReader(string cmdText, params DbParam[] parameters)
        {
            return ExecuteReader(cmdText, CommandType.Text, parameters);
        }

        public IDataReader ExecuteReader(string cmdText, CommandType cmdType, params DbParam[] parameters)
        {
            Checks.NotNull(cmdText, "cmdText");
            return InternalAdoSession.ExecuteReader(cmdText, parameters, cmdType);
        }

        public object ExecuteScalar(string cmdText, params DbParam[] parameters)
        {
            return ExecuteScalar(cmdText, CommandType.Text, parameters);
        }
        

        public object ExecuteScalar(string cmdText, CommandType cmdType, params DbParam[] parameters)
        {
            Checks.NotNull(cmdText, "cmdText");
            return InternalAdoSession.ExecuteScalar(cmdText, parameters, cmdType);
        }

        public void Rollback()
        {
            this._internalAdoSession.CommitTransaction();
        }

        public void Save()
        {
            this._internalAdoSession.CommitTransaction();
            //初始化数据库结构
            this._internalAdoSession.BeginTransaction(_il);
        }
        void CheckDisposed()
        {
            if (this._disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        public DataTable ExecuteDataTable(string cmdText, params DbParam[] parameters)
        {
            return ExecuteDataTable(cmdText, CommandType.Text, parameters);
        }

        public DataTable ExecuteDataTable(string cmdText, CommandType cmdType, params DbParam[] parameters)
        {
            var reader = ExecuteReader(cmdText, parameters);
            DataTable dt = new DataTable();
            try
            {
                int fieldCount = reader.FieldCount;
                for (int i = 0; i < fieldCount; i++)
                {
                    DataColumn dc = new DataColumn(reader.GetName(i), reader.GetFieldType(i));
                    dt.Columns.Add(dc);
                }
                while (reader.Read())
                {
                    DataRow dr = dt.NewRow();
                    for (int i = 0; i < fieldCount; i++)
                    {
                        dr[i] = reader[i];
                    }
                    dt.Rows.Add(dr);
                }
            }
            finally
            {
                reader.Close();
            }
            return dt;
        }

        public IEnumerable<T> ExecuteSqlToList<T>(string cmdText, params DbParam[] parameters)
        {
            return ExecuteSqlToList<T>(cmdText, CommandType.Text, parameters);
        }

        public IEnumerable<T> ExecuteSqlToList<T>(string cmdText, CommandType cmdType, params DbParam[] parameters)
        {
            Checks.NotNull(cmdText, "cmdText");
            return new InternalSqlQuery<T>(this, cmdText, cmdType, parameters);
        }

        public DbSet<SZORM_Upgrade> SZORM_Upgrades { get; set; }
    }
    public class SZORM_Upgrade
    {
        public int? Version { get; set; }
        [SZColumn(MaxLength =4000)]
        public string UPContent { get; set; }
        public DateTime? UPTime { get; set; }
        public DateTime? ReleaceTime { get; set; }
        [SZColumn(MaxLength = 4000)]
        public string ErrorMsg { get; set; }
    }
}
