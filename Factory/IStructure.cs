using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SZORM.Factory
{
    public interface IStructure
    {
        #region 常用字符串
        /// <summary>
        /// 对待特殊数据库表名右侧字符串
        /// </summary>
        /// <returns></returns>
        string GetTableChar(string tableName);
        /// <summary>
        /// 对待特殊数据库字段右侧字符串
        /// </summary>
        /// <returns></returns>
        string GetColumnChar(string columnName);
        /// <summary>
        /// 参数标示
        /// </summary>
        /// <returns></returns>
        string ParametersChar();
        /// <summary>
        /// 连接字符串
        /// </summary>
        /// <returns></returns>
        string JoinChar(params string[] str); 
        #endregion

        #region 表结构信息管理
        
        
        /// <summary>
        /// 获取当前数据库中存在的表
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        List<EntityModel> TableList(SZTransaction transaction);
        /// <summary>
        /// 获取表中存在的字段
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        List<EntityPropertyModel> ColumnList(SZTransaction transaction, string tableName);

        void CreateTable(SZTransaction transaction, EntityModel model);
        void ColumnAdd(SZTransaction transaction, string tableName, EntityPropertyModel model);
        void ColumnEdit(SZTransaction transaction, string tableName, EntityPropertyModel model);
        void ColumnAddKey(SZTransaction transaction, string tablename, string fieldname);
        void ColumnDropKey(SZTransaction transaction, string tablename, string fieldname);

        string FieldType(EntityPropertyModel _field);
        #endregion

        #region 增删改查
        string Insert(string tableName, Dictionary<string, string> fieldAndParms);
        string Update(string tableName, Dictionary<string, string> fieldAndParms, string whereSql);
        /// <summary>
        /// 删除单个实体
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="keyName"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        string Delete(string tableName, string whereSql);
        /// <summary>
        /// 查询单个实体
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="fields"></param>
        /// <param name="keyName"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        string Query(string tableName, List<string> fields, string whereSql);
        /// <summary>
        /// 查询拼接
        /// </summary>
        /// <param name="selectColumns"></param>
        /// <param name="tablename"></param>
        /// <param name="wherestr"></param>
        /// <param name="orderby"></param>
        /// <returns></returns>
        string Query(List<string> selectColumns, string tablename, string whereSql, Dictionary<string, string> orderby);
        /// <summary>
        /// 分页查询拼接
        /// </summary>
        /// <param name="selectColumns"></param>
        /// <param name="tablename"></param>
        /// <param name="wherestr"></param>
        /// <param name="orderby"></param>
        /// <param name="beginnum"></param>
        /// <param name="endnum"></param>
        /// <returns></returns>
        string Query(List<string> selectColumns, string tablename, string whereSql, Dictionary<string, string> orderby, int beginnum, int endnum);
        #endregion
    }
    public class SqlAndParametersModel
    {
        public SqlAndParametersModel()
        {
            Parameters = new Dictionary<string, object>();
            Sql = string.Empty;
        }
        public string Sql { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public object Data { get; set; }
    }
}
