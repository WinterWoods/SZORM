using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using SZORM.Factory;

namespace SZORM
{
    /// <summary>
    /// 继承该类,重写方法即可,默认连接字符串等于类名字
    /// </summary>
    public abstract class DbContext : IDisposable
    {
        /// <summary>
        /// 每次只进行一次检查
        /// </summary>
        static Dictionary<string, bool> isok = new Dictionary<string, bool>();
        private bool isUPDataBase = true;
        /// <summary>
        /// 是否更新数据库结构
        /// </summary>
        protected bool IsUPDataBase
        {
            get { return isUPDataBase; }
        }
        /// <summary>
        /// 每个初始化初始化一个事务
        /// </summary>
        public SZTransaction transaction;
        private IStructure genSqlInterface;
        /// <summary>
        /// 用于生成sql
        /// </summary>
        internal IStructure GenSqlInterface
        {
            get { return genSqlInterface; }
        }
        internal List<EntityModel> refListTable;
        public DbContext(bool _isUPDataBase = true)
            : this(null, _isUPDataBase)
        {

        }
        //默认使用继承的名字类连接
        public DbContext(IStructure excuteSql, bool _isUPDataBase = true)
        {
            genSqlInterface = excuteSql;
            isUPDataBase = _isUPDataBase;
            Type type = this.GetType();
            ConnectionStringSettingsCollection settings = ConfigurationManager.ConnectionStrings;
            if (settings[type.Name] == null) throw new Exception("没有配置连接字符串!");
            Init(settings[type.Name].ConnectionString, settings[type.Name].ProviderName);
        }
        //使用其他连接字符串名字
        public DbContext(IStructure excuteSql, string settingName, bool _isUPDataBase = true)
        {
            genSqlInterface = excuteSql;
            isUPDataBase = _isUPDataBase;
            ConnectionStringSettingsCollection settings = ConfigurationManager.ConnectionStrings;
            if (settings[settingName] == null) throw new Exception("没有配置连接字符串!");
            Init(settings[settingName].ConnectionString, settings[settingName].ProviderName);
        }
        //使用自定义连接字符串及访问方法
        public DbContext(IStructure excuteSql, string ConnectionStr, string ProviderName, bool _isUPDataBase = true)
        {
            genSqlInterface = excuteSql;
            isUPDataBase = _isUPDataBase;
            Init(ConnectionStr, ProviderName);
        }

        private void Init(string ConnectionStr, string ProviderName)
        {
            if (string.IsNullOrEmpty(ConnectionStr) || string.IsNullOrEmpty(ConnectionStr)) throw new Exception("连接字符串和ProviderName都不能为空");
            transaction = new SZTransaction(ConnectionStr, ProviderName);

            if (genSqlInterface == null)
                //初始化生成sql工厂
                genSqlInterface = ExcuteSqlFactory.Init(transaction.ConnectionConfig.ProviderName);
            //开启事务
            transaction.BeginTrans();
            //返回所有的属性.
            refListTable = ReflectionCache.DbContextGet(this);
            for (int i = 0; i < refListTable.Count; i++)
            {
                //必须创建新的实例.否则容易被释放掉.
                object obj = Assembly.GetAssembly(refListTable[i].PropertyType).CreateInstance(refListTable[i].PropertyType.FullName);
                refListTable[i].PropertyInfo.SetValue(this, obj, null);
                refListTable[i].SetDbContextMethod.Invoke(obj, new object[] { this });
                refListTable[i].SetTableMethod.Invoke(obj, new object[] { refListTable.Find(find => find.EntityName == refListTable[i].EntityName) });
            }
            lock (isok)
            {
                if (!isok.ContainsKey(ConnectionStr))
                {
                    //开始验证数据库结构
                    if (isUPDataBase)
                    {
                        DbStructCheck();
                        transaction.Commit();
                        transaction.BeginTrans();
                    }
                    isok.Add(ConnectionStr, true);
                }
            }

        }
        public SZTransaction CreateNewTrans()
        {
            SZTransaction szTran = new SZTransaction(transaction.ConnectionConfig.ConnectionStr, transaction.ConnectionConfig.ProviderName);
            szTran.BeginTrans();
            return szTran;
        }
        public void Dispose()
        {
            transaction.RollBack();
            transaction.Dispose();
            transaction = null;
        }
        public void Save()
        {
            transaction.Commit();
            transaction.BeginTrans();
        }
        public void RollBack()
        {
            transaction.RollBack();
            transaction.Dispose();
        }
        public DbDataReader ExceuteDataReader(string cmdText, params DbParameter[] commandParameters)
        {
            return transaction.ExceuteDataReader(cmdText, commandParameters);
        }
        public DataTable ExceuteDataTable(string cmdText, params DbParameter[] commandParameters)
        {
            return transaction.ExceuteDataTable(cmdText, commandParameters);
        }
        public object ExecuteScalar(string cmdText, params DbParameter[] commandParameters)
        {
            return transaction.ExecuteScalar(cmdText, commandParameters);
        }
        public int ExcuteNoQuery(string cmdText, params DbParameter[] commandParameters)
        {
            return transaction.ExcuteNoQuery(cmdText, commandParameters);
        }

        private void DbStructCheck()
        {
            //初始化所有的属性
            //初始化库
            int i = 0;
            //获取当前实例的属性
            var RefListTable = ReflectionCache.DbContextGet(this);
            //获取数据库中的表列表
            var databaseTablelist = genSqlInterface.TableList(transaction);
            //循环所有的属性
            foreach (var _refTable in RefListTable)
            {
                if (_refTable.Att.IsView)
                    continue;
                bool __tablehas = false;
                foreach (var datatable in databaseTablelist)
                {
                    
                    //遍历查询是否存在这个表,
                    if (datatable.Att.TableName.ToUpper() == _refTable.Att.TableName.ToUpper())
                    {
                        //如果存在
                        __tablehas = true;
                        //就去检查所有的字段是否一致,

                        var dataTableColumn = genSqlInterface.ColumnList(transaction, datatable.Att.TableName);
                        foreach (var _field in _refTable.Fields)
                        {

                            bool __fieldhas = false;
                            foreach (var column in dataTableColumn)
                            {
                                if (column.Att.ColumnName.ToUpper() == _field.Att.ColumnName.ToUpper())
                                {
                                    __fieldhas = true;
                                    //开始比对,是否一样
                                    bool __filedIs = true;
                                    if (_field.Att.Required != column.Att.Required)
                                    {
                                        //如果不一样
                                        __filedIs = false;
                                    }
                                    if (_field.Att.IsKey != column.Att.IsKey)
                                    {
                                        //修改
                                        __filedIs = false;
                                    }
                                    string tmp = genSqlInterface.FieldType(_field);
                                    if (tmp != column.Att.ColumnType)
                                    {
                                        __filedIs = false;
                                    }
                                    if (column.Att.DefaultValue != _field.Att.DefaultValue)
                                    {
                                        __filedIs = false;
                                    }
                                    if (!__filedIs)
                                    {
                                        //更新字段
                                        genSqlInterface.ColumnEdit(transaction, datatable.Att.TableName, _field);
                                    }
                                    break;
                                }
                            }
                            //如果不存在这个字段.
                            if (!__fieldhas)
                            {
                                //添加字段
                                genSqlInterface.ColumnAdd(transaction, _refTable.Att.TableName, _field);
                            }
                        }


                    }
                }
                if (!__tablehas)
                {
                    genSqlInterface.CreateTable(transaction, _refTable);
                    i++;
                }
            }

            //开始更新数据库
            var uplist = SZORM_Upgrades.AsQuery().OrderDesc(o => o.Version).Take(1).ToList();
            if (uplist.Any())
            {
                dbVersion = uplist[0].Version;
            }
            else
            {
                SZORM_Upgrade up = new SZORM_Upgrade();
                up.UPTime = DateTime.Now;
                up.Version = 0;
                up.UPContent = "首次创建数据库.";
                UPDBVersion(up);
                Initialization();
            }
            UpdataDBExce();
        }
        /// <summary>
        /// 数据库初始化.只有第一次创建数据库时运行此函数
        /// </summary>
        protected abstract void Initialization();
        private int? dbVersion = -1;
        protected decimal? DBVersion { get { return dbVersion; } }
        /// <summary>
        /// 重写次方法.使用DBVersion查看数据库版本,第一次为1,初始化的时候运行一次
        /// </summary>
        protected abstract void UpdataDBExce();
        /// <summary>
        /// 更新数据库版本
        /// </summary>
        /// <param name="up"></param>
        protected void UPDBVersion(SZORM_Upgrade up)
        {
            if (up.Version <= dbVersion) throw new Exception("更新后的版本必须大于现有版本");

            up.UPTime = DateTime.Now;
            SZORM_Upgrades.Add(up);
            Save();

            dbVersion = up.Version;
        }
        public DbSet<SZORM_Upgrade> SZORM_Upgrades { get; set; }

    }
    public class SZORM_Upgrade
    {
        public int? Version { get; set; }
        public string UPContent { get; set; }
        public DateTime? UPTime { get; set; }
        public DateTime? ReleaceTime { get; set; }
        public string ErrorMsg { get; set; }
    }
}
