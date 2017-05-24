using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SZORM.Factory.Models;

namespace SZORM.Factory
{
    public interface IStructure
    {
        #region 表结构信息管理
        /// <summary>
        /// 查询库中所有表
        /// </summary>
        /// <returns></returns>
        List<TableModel> TableList(DbContext dbContext);
        /// <summary>
        /// 查询某表中所有字段
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        List<ColumnModel> ColumnList(DbContext dbContext,string tableName);
        /// <summary>
        /// 创建表
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        void CreateTable(DbContext dbContext,TableModel model);
        /// <summary>
        /// 像某表添加字段
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        void ColumnAdd(DbContext dbContext,string tableName, ColumnModel model);
        void ColumnEdit(DbContext dbContext,string tableName, ColumnModel model);

        string FieldType(ColumnModel column);
        #endregion
        
    }
}
