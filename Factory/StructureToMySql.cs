using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace SZORM.Factory
{
    public class StructureToMySql : IStructure
    {
        /// <summary>
        /// 特殊数据库表名左侧字符串
        /// </summary>
        /// <returns></returns>
        public string GetTableChar(string tableName)
        {
            return "`" + tableName + "`";
        }
        /// <summary>
        /// 特殊数据库字段左侧字符串
        /// </summary>
        /// <returns></returns>
        public string GetColumnChar(string columnName)
        {
            return "`" + columnName + "`";
        }
        /// <summary>
        /// 参数
        /// </summary>
        /// <returns></returns>
        public string ParametersChar()
        {
            return "?";
        }
        /// <summary>
        /// 连接字符串
        /// </summary>
        /// <returns></returns>
        public string JoinChar(params string[] str)
        {
            return "concat(" + string.Join(",", str) + ")";
        }
        /// <summary>
        /// 获取表列表
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public List<EntityModel> TableList(SZTransaction transaction)
        {
            string sql = "show tables;";
            DataTable table = transaction.ExceuteDataTable(sql);
            List<EntityModel> tableList = new List<EntityModel>();
            foreach (DataRow row in table.Rows)
            {
                EntityModel model = new EntityModel();
                model.Att.TableName = row[0].ToString();
                model.Att.Information = "";
                tableList.Add(model);
            }
            return tableList;
        }
        /// <summary>
        /// 字段列表
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public List<EntityPropertyModel> ColumnList(SZTransaction transaction, string tableName)
        {
            string sql = "show columns from " + tableName;

            DataTable table = transaction.ExceuteDataTable(sql);
            List<EntityPropertyModel> tableList = new List<EntityPropertyModel>();
            foreach (DataRow row in table.Rows)
            {
                EntityPropertyModel model = new EntityPropertyModel();
                model.Att.ColumnName = row["field"].ToString();
                model.Att.ColumnType = row["type"].ToString().ToUpper();
                if (row["null"].ToString() == "YES")
                {
                    model.Att.Required = false;
                }
                else
                {
                    model.Att.Required = true;
                }

                if (row["key"].ToString() == "PRI")
                {
                    model.Att.IsKey = true;
                }
                else
                {
                    model.Att.IsKey = false;
                }
                model.Att.DefaultValue = row["DEFAULT"].ToString();

                tableList.Add(model);
            }
            return tableList;
        }
        /// <summary>
        /// 创建表
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="model"></param>
        public void CreateTable(SZTransaction transaction, EntityModel model)
        {
            StringBuilder str = new StringBuilder();
            str.AppendFormat("CREATE TABLE {0}", GetTableChar(model.Att.TableName));
            str.Append("(");
            List<string> fields = new List<string>();
            string key = "";
            model.Fields.ForEach(f =>
            {
                if (f.Att.IsKey)
                {
                    key = f.Att.ColumnName;
                }
                fields.Add(FieldString(f));
            });
            if (!string.IsNullOrEmpty(key))
                fields.Add("PRIMARY KEY (" + GetColumnChar(key) + ")");

            str.Append(string.Join(",", fields));
            str.Append(")");
            transaction.ExcuteNoQuery(str.ToString());
        }
        public string FieldString(EntityPropertyModel field)
        {
            StringBuilder str = new StringBuilder();
            str.AppendFormat("{0} {1}", GetColumnChar(field.Att.ColumnName), FieldType(field));
            if (field.Att.Required)
                str.Append("  NOT NULL ");
            if (!string.IsNullOrEmpty(field.Att.DefaultValue))
                str.AppendFormat(" DEFAULT {0}", field.Att.DefaultValue);

            return str.ToString();
        }
        /// <summary>
        /// 添加字段
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="model"></param>
        public void ColumnAdd(SZTransaction transaction, string tableName, EntityPropertyModel model)
        {
            string sql = "ALTER TABLE " + GetTableChar(tableName) + " ADD (" + FieldString(model) + ")";
            transaction.ExcuteNoQuery(sql);
        }
        /// <summary>
        /// 修改字段
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="model"></param>
        public void ColumnEdit(SZTransaction transaction, string tableName, EntityPropertyModel model)
        {
            string sql = "ALTER TABLE " + GetTableChar(tableName) + " MODIFY COLUMN" + FieldString(model) + ";";
            transaction.ExcuteNoQuery(sql);
        }
        /// <summary>
        /// 添加key
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="tablename"></param>
        /// <param name="fieldname"></param>
        public void ColumnAddKey(SZTransaction transaction, string tableName, string fieldname)
        {
            string sql = " ALTER TABLE " + GetTableChar(tableName) + "PRIMARY KEY ,ADD PRIMARY KEY (" + GetColumnChar(fieldname) + ") ";
            transaction.ExcuteNoQuery(sql);
        }
        /// <summary>
        /// 删除key
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="tablename"></param>
        /// <param name="fieldname"></param>
        public void ColumnDropKey(SZTransaction transaction, string tableName, string fieldname)
        {
            string sql = " ALTER TABLE " + GetTableChar(tableName) + "PRIMARY KEY";
            transaction.ExcuteNoQuery(sql);
        }
        /// <summary>
        /// 插入数据
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="fieldAndValue"></param>
        /// <returns></returns>
        public string Insert(string tableName, Dictionary<string, string> fieldAndParms)
        {
            StringBuilder str = new StringBuilder();

            List<string> field = new List<string>();
            List<string> value = new List<string>();
            foreach (var _o in fieldAndParms)
            {
                field.Add(GetColumnChar(_o.Key));
                value.Add(ParametersChar() + _o.Value);
            }
            str.AppendFormat("INSERT INTO {0} ({1}) VALUES ({2})", GetTableChar(tableName), string.Join(",", field), string.Join(",", value));
            return str.ToString();
        }
        /// <summary>
        /// 修改数据
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="fieldAndValue"></param>
        /// <param name="whereSql"></param>
        /// <returns></returns>
        public string Update(string tableName, Dictionary<string, string> fieldAndParms, string whereSql)
        {
            List<string> value = new List<string>();
            foreach (var _o in fieldAndParms)
            {
                value.Add(GetColumnChar(_o.Key) + "=" + ParametersChar() + _o.Value);
            }

            StringBuilder str = new StringBuilder();
            str.AppendFormat("UPDATE {0} SET {1} {2}", GetTableChar(tableName), string.Join(",", value), whereSql);
            return str.ToString();
        }
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="whereSql"></param>
        /// <returns></returns>
        public string Delete(string tableName, string whereSql)
        {
            StringBuilder str = new StringBuilder();
            str.AppendFormat("DELETE  FROM {0} {1}", GetTableChar(tableName), whereSql);
            return str.ToString();
        }

        public string Query(string tableName, List<string> fields, string whereSql)
        {
            StringBuilder str = new StringBuilder();
            str.AppendFormat("SELECT {0}  FROM {1} {2}", string.Join(",", fields), GetTableChar(tableName), whereSql);
            return str.ToString();
        }

        public string Query(List<string> selectColumns, string tablename, string whereSql, Dictionary<string, string> orderby)
        {
            return Query(selectColumns, tablename, whereSql, orderby, 0, 0);
        }

        public string Query(List<string> selectColumns, string tablename, string whereSql, Dictionary<string, string> orderby, int beginnum, int endnum)
        {
            string selectsql = " t.* ";
            if (selectColumns.Any())
            {
                selectsql = string.Join(",", selectColumns);

            }
            else
            {
                tablename = GetTableChar(tablename) + " t ";
            }
            string ordersql = "";
            if (orderby.Any())
            {
                List<string> tmp = new List<string>();
                foreach (var order in orderby)
                {
                    tmp.Add(GetColumnChar(order.Key) + " " + order.Value);
                }
                ordersql = " order by " + string.Join(",", tmp);
            }
            string wherestr = " WHERE 1=1";
            if (string.IsNullOrEmpty(whereSql))
                wherestr = wherestr + whereSql;
            else
                wherestr = wherestr + " and " + whereSql;
            if (endnum == -1)
            {
                return string.Format("SELECT {0} FROM {1} {2} {3}", selectsql, tablename, wherestr, ordersql);
            }
            else
            {
                return string.Format("SELECT {0}  FROM {1}  {2} {3}  Limit {4},{5} ", selectsql, tablename, wherestr, ordersql, beginnum, endnum);
            }
        }

        public string FieldType(EntityPropertyModel _field)
        {
            string result = string.Empty;
            switch (_field.FieldType)
            {
                case "System.Boolean":
                    result = "Boolean";
                    break;
                case "System.String":
                    result = "VARCHAR(" + _field.Att.MaxLength + ")";
                    break;
                case "System.DateTime":
                    result = "DATETIME";
                    break;
                case "System.Int16":
                    result = "SMALLINT";
                    break;
                case "System.Int32":
                    result = "INT(11)";
                    break;
                case "System.Int64":
                    result = "BIGINT(20)";
                    break;
                case "System.Single":
                    if (_field.Att.NumberSize == 0 && _field.Att.NumberPrecision == 0)
                    {
                        result = "FLOAT(7,1)";
                    }
                    else if (_field.Att.NumberSize != 0 && _field.Att.NumberPrecision == 0)
                    {
                        if (_field.Att.NumberSize < 1 || _field.Att.NumberSize > 7)
                            throw new Exception("FLOAT类型的长度必须是\">=1\",\"<=7\"");
                        result = "FLOAT(" + _field.Att.NumberSize + ")";
                    }
                    else
                    {
                        if (_field.Att.NumberSize< 1 || _field.Att.NumberSize> 7)
                            throw new Exception("FLOAT类型的长度必须是\">=1\",\"<=7\"");
                        if (_field.Att.NumberPrecision< 1 || _field.Att.NumberPrecision > 7)
                            throw new Exception("FLOAT类型的长度必须是\">=1\",\"<=7\"");
                        if (_field.Att.NumberPrecision > _field.Att.NumberSize)
                            throw new Exception("FLOAT类型的精度不能大于长度");
                        result = "FLOAT(" + _field.Att.NumberSize + "," + _field.Att.NumberPrecision + ")";

                    }
                    break;
                case "System.Double":
                    if (_field.Att.NumberSize == 0 && _field.Att.NumberPrecision == 0)
                    {
                        result = "DOUBLE(15,2)";
                    }
                    else if (_field.Att.NumberSize != 0 && _field.Att.NumberPrecision == 0)
                    {
                        if (_field.Att.NumberSize < 1 || _field.Att.NumberSize > 15)
                            throw new Exception("Double类型的长度必须是\">=1\",\"<=15\"");
                        result = "DOUBLE(" + _field.Att.NumberSize + ")";
                    }
                    else
                    {
                        if (_field.Att.NumberSize< 1 || _field.Att.NumberSize > 15)
                            throw new Exception("Double类型的长度必须是\">=1\",\"<=15\"");
                        if (_field.Att.NumberPrecision < 1 || _field.Att.NumberPrecision >15)
                            throw new Exception("Double类型的精度必须是\">=1\",\"<=15\"");
                        if (_field.Att.NumberPrecision > _field.Att.NumberSize)
                            throw new Exception("Double类型的精度不能大于长度");
                        result = "DOUBLE(" + _field.Att.NumberSize + "," + _field.Att.NumberPrecision + ")";
                    }
                    break;
                case "System.Decimal":
                    if (_field.Att.NumberSize == 0 && _field.Att.NumberPrecision == 0)
                    {
                        result = "DECIMAL(65)";
                    }
                    else if (_field.Att.NumberSize != 0 && _field.Att.NumberPrecision == 0)
                    {
                        if (_field.Att.NumberSize < 1 || _field.Att.NumberSize > 65)
                            throw new Exception("Decimal类型的长度必须是\">=1\",\"<=65\"");
                        result = "DECIMAL(" + _field.Att.NumberSize + ")";
                    }
                    else
                    {
                        if (_field.Att.NumberSize < 1 || _field.Att.NumberSize > 65)
                            throw new Exception("Decimal类型的长度必须是\">=1\",\"<=65\"");
                        if (_field.Att.NumberPrecision < 1 || _field.Att.NumberPrecision > 30)
                            throw new Exception("Decimal类型的精度必须是\">=1\",\"<=30\"");
                        if (_field.Att.NumberPrecision > _field.Att.NumberSize)
                            throw new Exception("Decimal类型的精度不能大于长度");
                        result = "DECIMAL(" + _field.Att.NumberSize + "," + _field.Att.NumberPrecision + ")";
                    }
                    break;
                case "System.Byte[]":
                    result = "LONGBLOB";
                    break;
                default:
                    {
                        if (_field.IsEnmu)
                        {
                            result = "VARCHAR(" + _field.Att.MaxLength + ")";
                            //_field.Property.
                        }
                        else
                        {
                            throw new Exception("暂时不支持的数据类型:" + _field.Name + "-" + _field.FieldType);
                        }
                    }
                    break;
            }
            return result;
        }

    }
}
