using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace SZORM.Factory
{
    public class StructureToOracle : IStructure
    {
        /// <summary>
        /// 特殊数据库表名左侧字符串
        /// </summary>
        /// <returns></returns>
        public string GetTableChar(string tableName)
        {
            return tableName;
        }
        /// <summary>
        /// 特殊数据库字段左侧字符串
        /// </summary>
        /// <returns></returns>
        public string GetColumnChar(string columnName)
        {
            return columnName;
        }
        /// <summary>
        /// 参数
        /// </summary>
        /// <returns></returns>
        public string ParametersChar()
        {
            return ":";
        }
        /// <summary>
        /// 连接字符串
        /// </summary>
        /// <returns></returns>
        public string JoinChar(params string[] str)
        {
            return string.Join("||", str);
        }
        /// <summary>
        /// 获取表列表
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public List<EntityModel> TableList(SZTransaction transaction)
        {
            string sql = "select * from user_tab_comments";
            DataTable table = transaction.ExceuteDataTable(sql);
            List<EntityModel> tableList = new List<EntityModel>();
            foreach (DataRow row in table.Rows)
            {
                EntityModel model = new EntityModel();
                model.Att.TableName = row["TABLE_NAME"].ToString();
                model.Att.Information = row["COMMENTS"].ToString();
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
            string sql = "select * from user_tab_columns where TABLE_NAME='" + tableName.ToUpper() + "'";

            DataTable table = transaction.ExceuteDataTable(sql);
            List<EntityPropertyModel> tableList = new List<EntityPropertyModel>();
            foreach (DataRow row in table.Rows)
            {
                EntityPropertyModel model = new EntityPropertyModel();
                model.Att.ColumnName = row["COLUMN_NAME"].ToString();
                string type = row["DATA_TYPE"].ToString();
                if (type == "BLOB" || type == "DATE")
                {
                    model.Att.ColumnType = type;
                }
                else if (type == "VARCHAR2")
                {
                    model.Att.ColumnType = type + "(" + row["DATA_LENGTH"].ToString() + ")";
                }
                else if (type == "NUMBER")
                {
                    if (row["DATA_PRECISION"].ToString() == "")
                    {
                        model.Att.ColumnType = type;
                    }
                    else
                    {
                        if (row["DATA_PRECISION"].ToString() != "" && row["DATA_SCALE"].ToString() == "0")
                        {
                            model.Att.ColumnType = type + "(" + row["DATA_PRECISION"].ToString() + ")";
                        }
                        else if (row["DATA_PRECISION"].ToString() != "0" && row["DATA_SCALE"].ToString() != "0")
                        {
                            model.Att.ColumnType = type + "(" + row["DATA_PRECISION"].ToString() + "," + row["DATA_SCALE"].ToString() + ")";
                        }
                    }
                }
                else
                {
                    model.Att.ColumnType = type;
                }


                string PKSql = "select count(1)  from user_cons_columns c, user_constraints p where c.constraint_name=p.constraint_name and p.constraint_type='P' and p.table_name='" + tableName.ToUpper() + "' and c.column_name='" + model.Att.ColumnName + "'";
                object obj = transaction.ExecuteScalar(PKSql);
                if (obj.ToString() == "1")
                {
                    model.Att.IsKey = true;
                }
                else
                {
                    model.Att.IsKey = false;
                }
                string CommentsSql = "select COMMENTS from user_col_comments where TABLE_NAME='" + tableName.ToUpper() + "' and COLUMN_NAME='" + model.Att.ColumnName + "'";
                obj = transaction.ExecuteScalar(CommentsSql);
                if (obj != null)
                {
                    model.Att.Information = obj.ToString();
                }

                model.Att.DefaultValue = row["DATA_DEFAULT"].ToString();

                if (row["NULLABLE"].ToString() == "Y")
                    model.Att.Required = false;
                else
                {
                    model.Att.Required = true;
                }
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
            str.AppendFormat("CREATE TABLE {0}", model.Att.TableName);
            str.Append("(");
            List<string> fields = new List<string>();
            model.Fields.ForEach(f =>
            {

                fields.Add(FieldString(f));
            });
            str.Append(string.Join(",", fields));
            str.Append(")");
            transaction.ExcuteNoQuery(str.ToString());
        }
        public string FieldString(EntityPropertyModel field)
        {
            StringBuilder str = new StringBuilder();
            str.AppendFormat("{0} {1}", field.Att.ColumnName, FieldType(field));
            if (field.Att.IsKey)
                str.Append("  PRIMARY KEY  ");
            if (!string.IsNullOrEmpty(field.Att.DefaultValue))
                str.AppendFormat(" DEFAULT {0}", field.Att.DefaultValue);
            if (field.Att.Required)
                str.Append("  NOT NULL ");
            return str.ToString();
        }
        /// <summary>
        /// 添加字段
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="model"></param>
        public void ColumnAdd(SZTransaction transaction, string tableName, EntityPropertyModel model)
        {
            string sql = "ALTER TABLE " + tableName + " ADD (" + FieldString(model) + ")";
            transaction.ExcuteNoQuery(sql);
        }
        /// <summary>
        /// 修改字段
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="model"></param>
        public void ColumnEdit(SZTransaction transaction, string tableName, EntityPropertyModel model)
        {
            if (model.Att.IsKey)
            {
                try
                {

                    transaction.ExcuteNoQuery("ALTER TABLE " + tableName + " DROP PRIMARY KEY");
                    // ColumnDropKey(transaction, tableName, model.Att.ColumnName);
                }
                catch (Exception)
                {

                }
                try
                {
                    string sql1 = "ALTER TABLE " + tableName + " MODIFY (" + model.Att.ColumnName + " null)";
                    transaction.ExcuteNoQuery(sql1);
                }
                catch (Exception)
                {

                }
            }
            string sql = "ALTER TABLE " + tableName + " MODIFY (" + FieldString(model) + ")";
            transaction.ExcuteNoQuery(sql);
        }
        /// <summary>
        /// 添加key
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="tablename"></param>
        /// <param name="fieldname"></param>
        public void ColumnAddKey(SZTransaction transaction, string tablename, string fieldname)
        {
            string sql = " alter table " + tablename + " add constraint pk_" + tablename + "_" + fieldname + " primary key(" + fieldname + ") ";
            transaction.ExcuteNoQuery(sql);
        }
        /// <summary>
        /// 删除key
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="tablename"></param>
        /// <param name="fieldname"></param>
        public void ColumnDropKey(SZTransaction transaction, string tablename, string fieldname)
        {
            string sql = "alter table " + tablename.ToUpper() + " drop constraint " + fieldname;
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
                field.Add(_o.Key);
                value.Add(ParametersChar() + _o.Value);
            }
            str.AppendFormat("INSERT INTO {0} ({1}) VALUES ({2})", tableName, string.Join(",", field), string.Join(",", value));
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
                value.Add(_o.Key + "=" + ParametersChar() + _o.Value);
            }

            StringBuilder str = new StringBuilder();
            str.AppendFormat("UPDATE {0} SET {1} {2}", tableName, string.Join(",", value), whereSql);
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
            str.AppendFormat("DELETE  FROM {0} {1}", tableName, whereSql);
            return str.ToString();
        }

        public string Query(string tableName, List<string> fields, string whereSql)
        {
            StringBuilder str = new StringBuilder();
            str.AppendFormat("SELECT {0}  FROM {1} {2}", string.Join(",", fields), tableName, whereSql);
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
                tablename = tablename + " t ";
            }
            string ordersql = " null ";
            if (orderby.Any())
            {
                List<string> tmp = new List<string>();
                foreach (var order in orderby)
                {
                    tmp.Add(order.Key + " " + order.Value);
                }
                ordersql = string.Join(",", tmp);
            }
            string wherestr = " WHERE 1=1";
            if (string.IsNullOrEmpty(whereSql))
                wherestr = wherestr + whereSql;
            else
                wherestr = wherestr + " and " + whereSql;
            if (endnum == -1)
            {
                return string.Format("SELECT {0}, ROW_NUMBER() OVER(ORDER BY {1}) AS ROW_NUMBER FROM {2} {3}", selectsql, ordersql, tablename, wherestr);
            }
            else
            {
                return string.Format("SELECT *  FROM (SELECT * FROM (SELECT {0}, ROW_NUMBER() OVER(ORDER BY {1}) AS ROW_NUMBER FROM {2}  {3}) P WHERE P.ROW_NUMBER > {4}) Q WHERE ROWNUM <= {5}", selectsql, ordersql, tablename, wherestr, beginnum, endnum);
            }
        }

        public string FieldType(EntityPropertyModel _field)
        {
            string result = string.Empty;
            switch (_field.FieldType)
            {
                case "System.Boolean":
                    result = "VARCHAR2(1)";
                    break;
                case "System.String":
                    result = "VARCHAR2(" + _field.Att.MaxLength + ")";
                    break;
                case "System.DateTime":
                    result = "DATE";
                    break;
                case "System.Int16":
                    if (_field.Att.NumberSize != 0)
                    {
                        if (_field.Att.NumberSize > 4) throw new Exception("Int32类型的长度必须是\"<=4\"");
                        result = "NUMBER(" + _field.Att.NumberSize + ")";
                    }
                    else
                    {
                        result = "NUMBER(4)";
                    }
                    break;
                case "System.Int32":
                    if (_field.Att.NumberSize != 0)
                    {
                        if (_field.Att.NumberSize > 9 || _field.Att.NumberSize < 5) throw new Exception("Int32类型的长度必须是\">=5\",\"<=9\"");
                        result = "NUMBER(" + _field.Att.NumberSize + ")";
                    }
                    else
                    {
                        result = "NUMBER(9)";
                    }
                    break;
                case "System.Int64":
                    if (_field.Att.NumberSize != 0)
                    {
                        if (_field.Att.NumberSize > 19 || _field.Att.NumberSize < 10) throw new Exception("Int32类型的长度必须是\">=10\",\"<=19\"");
                        result = "NUMBER(" + _field.Att.NumberSize + ")";
                    }
                    else
                    {
                        result = "NUMBER(19)";
                    }
                    break;
                case "System.Single":
                    if (_field.Att.NumberSize == 0 && _field.Att.NumberPrecision == 0)
                    {
                        result = "NUMBER(7,1)";
                    }
                    else if (_field.Att.NumberSize != 0 && _field.Att.NumberPrecision == 0)
                    {
                        if (_field.Att.NumberSize < 1 || _field.Att.NumberSize > 7)
                            throw new Exception("FLOAT类型的长度必须是\">=1\",\"<=7\"");
                        result = "NUMBER(" + _field.Att.NumberSize + ")";
                    }
                    else
                    {
                        if (_field.Att.NumberSize < 1 || _field.Att.NumberSize > 7)
                            throw new Exception("FLOAT类型的长度必须是\">=1\",\"<=7\"");
                        if (_field.Att.NumberPrecision < 1 || _field.Att.NumberPrecision > 7)
                            throw new Exception("FLOAT类型的长度必须是\">=1\",\"<=7\"");
                        if (_field.Att.NumberPrecision > _field.Att.NumberSize)
                            throw new Exception("FLOAT类型的精度不能大于长度");
                        result = "NUMBER(" + _field.Att.NumberSize + "," + _field.Att.NumberPrecision + ")";

                    }
                    break;
                case "System.Double":
                    if (_field.Att.NumberSize == 0 && _field.Att.NumberPrecision == 0)
                    {
                        result = "NUMBER(15,2)";
                    }
                    else if (_field.Att.NumberSize != 0 && _field.Att.NumberPrecision == 0)
                    {
                        if (_field.Att.NumberSize < 1 || _field.Att.NumberSize > 15)
                            throw new Exception("Double类型的长度必须是\">=1\",\"<=15\"");
                        result = "NUMBER(" + _field.Att.NumberSize + ")";
                    }
                    else
                    {
                        if (_field.Att.NumberSize < 1 || _field.Att.NumberSize > 15)
                            throw new Exception("Double类型的长度必须是\">=1\",\"<=15\"");
                        if (_field.Att.NumberPrecision < 1 || _field.Att.NumberPrecision > 15)
                            throw new Exception("Double类型的精度必须是\">=1\",\"<=15\"");
                        if (_field.Att.NumberPrecision > _field.Att.NumberSize)
                            throw new Exception("Double类型的精度不能大于长度");
                        result = "NUMBER(" + _field.Att.NumberSize + "," + _field.Att.NumberPrecision + ")";
                    }
                    break;
                case "System.Decimal":
                    if (_field.Att.NumberSize == 0 && _field.Att.NumberPrecision == 0)
                    {
                        result = "NUMBER(38)";
                    }
                    else if (_field.Att.NumberSize != 0 && _field.Att.NumberPrecision == 0)
                    {
                        if (_field.Att.NumberSize < 1 || _field.Att.NumberSize > 38)
                            throw new Exception("Decimal类型的长度必须是\">=1\",\"<=38\"");
                        result = "NUMBER(" + _field.Att.NumberSize + ")";
                    }
                    else
                    {
                        if (_field.Att.NumberSize < 1 || _field.Att.NumberSize > 38)
                            throw new Exception("Decimal类型的长度必须是\">=1\",\"<=38\"");
                        if (_field.Att.NumberPrecision < 1 || _field.Att.NumberPrecision > 30)
                            throw new Exception("Decimal类型的精度必须是\">=1\",\"<=30\"");
                        if (_field.Att.NumberPrecision > _field.Att.NumberSize)
                            throw new Exception("Decimal类型的精度不能大于长度");
                        result = "NUMBER(" + _field.Att.NumberSize + "," + _field.Att.NumberPrecision + ")";
                    }
                    break;
                case "System.Byte[]":
                    result = "BLOB";
                    break;
                default:
                    {
                        if (_field.IsEnmu)
                        {
                            result = "VARCHAR2(" + _field.Att.MaxLength + ")";
                            //_field.Property.
                        }
                        else
                        {
                            throw new Exception("暂时不支持的数据类型");
                        }
                    }
                    break;
            }
            return result;
        }

    }
}
