using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace SZORM
{
    /// <summary>
    /// 表特征
    /// </summary>
    public sealed class SZTableAttribute : Attribute
    {
        /// <summary>
        /// 表格名字
        /// </summary>
        public string TableName { get; set; }
        /// <summary>
        /// 显示名字,出现异常信息显示表名,为空时显示TableName
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// 表描述信息,可以为空
        /// </summary>
        public string Information { get; set; }
        /// <summary>
        /// 是否视图,视图是不自动生成表的
        /// </summary>
        public bool IsView { get; set; }
    }
    /// <summary>
    /// 字段特征
    /// </summary>
    public sealed class SZColumnAttribute : Attribute
    {
        private bool isKey = false;
        /// <summary>
        /// 是否主键,默认值为false
        /// </summary>
        public bool IsKey
        {
            get { return isKey; }
            set { isKey = value; }
        }
        /// <summary>
        /// 如果不设置自动使用字段名字
        /// </summary>
        public string FieldName { get; set; }
        private string displayName="";
        /// <summary>
        /// 显示名字,如果出现异常显示名字,为空时显示ColumnName
        /// </summary>
        public string DisplayName
        {
            get { return displayName; }
            set { displayName = value; }
        }
        private bool isAddTime = false;
        /// <summary>
        /// 是否添加时间
        /// </summary>
        public bool IsAddTime
        {
            get { return isAddTime; }
            set { isAddTime = value; }
        }
        private bool isEditTime = false;
        /// <summary>
        /// 是否修改时间
        /// </summary>
        public bool IsEditTime
        {
            get { return isEditTime; }
            set { isEditTime = value; }
        }
        private bool required = false;
        /// <summary>
        /// 是否必须有值,默认为允许为空
        /// </summary>
        public bool Required
        {
            get { return required; }
            set { required = value; }
        }
        private int minLength = 0;
        /// <summary>
        /// 最小长度
        /// </summary>
        public int MinLength
        {
            get { return minLength; }
            set { minLength = value; }
        }
        private int maxLength = 0;
        /// <summary>
        /// 最大值
        /// </summary>
        public int MaxLength
        {
            get { return maxLength; }
            set { maxLength = value; }
        }
        /// <summary>
        /// 数字的最大位数
        /// </summary>
        public int NumberSize { get; set; }
        /// <summary>
        /// 数字的最大精度 可以从负数到整数 如-1 存储111 结果如110
        /// </summary>
        public int NumberPrecision { get; set; }
        /// <summary>
        /// 字段描述信息
        /// </summary>
        public string Information { get; set; }
        DbType? _dbType;/* -1=Unspecified */
        /// <summary>
        /// -1 表示未指定确切的值，用该属性的时候务必做 -1 判断。
        /// </summary>
        public DbType? DbType { get { return this._dbType; } set { this._dbType = value; } }

        public bool NotGen
        {
            get
            {
                return notGen;
            }

            set
            {
                notGen = value;
            }
        }

        private bool notGen = false;
    }
}
