using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace SZORM.Factory
{
    public class StructureToMSSql : IStructure
    {
        /// <summary>
        /// 特殊数据库表名左侧字符串
        /// </summary>
        /// <returns></returns>
        public string GetTableChar(string tableName)
        {
            return "[" + tableName + "]";
        }
        /// <summary>
        /// 特殊数据库字段左侧字符串
        /// </summary>
        /// <returns></returns>
        public string GetColumnChar(string columnName)
        {
            return "[" + columnName + "]";
        }
        /// <summary>
        /// 参数
        /// </summary>
        /// <returns></returns>
        public string ParametersChar()
        {
            return "@";
        }
        /// <summary>
        /// 连接字符串
        /// </summary>
        /// <returns></returns>
        public string JoinChar(params string[] str)
        {
            return string.Join("+", str);
            //return "concat(" + string.Join(",", str) + ")";
        }
        /// <summary>
        /// 获取表列表
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public List<EntityModel> TableList(SZTransaction transaction)
        {
            string sql = "select TableName=c.Name,TableInfo=isnull(f.[value],'') from sys.objects c left join sys.extended_properties f on f.major_id=c.object_id where  Type='U'";
            DataTable table = transaction.ExceuteDataTable(sql);
            List<EntityModel> tableList = new List<EntityModel>();
            foreach (DataRow row in table.Rows)
            {
                EntityModel model = new EntityModel();
                model.Att.TableName = row["TableName"].ToString();
                model.Att.Information = row["TableInfo"].ToString();
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
            string sql = @"select 
                            TableName=c.Name,
                            TableInfo=isnull(f.[value],''),
                            ColumnName=a.Name,
                            IsKey=case when exists(select 1 from sys.objects where parent_object_id=a.object_id and type=N'PK' and name in
                                            (select Name from sys.indexes where index_id in
                                            (select indid from sysindexkeys where id = a.object_id AND colid=a.column_id)))
                                            then 'Y' else '' end,
                            ColumnType=b.Name,
                            Lenght=ColumnProperty(a.object_id,a.Name,'Precision'),
                            RightLenght=isnull(ColumnProperty(a.object_id,a.Name,'Scale'),0),
                            IsNull=case when a.is_nullable=1 then 'Y' else '' end,
                            ColumnInfo=isnull(e.[value],''),
                            ColumnDefaultValue=isnull(d.text,'')    
                        from 
                            sys.columns a
                        left join
                            sys.types b on a.user_type_id=b.user_type_id
                        inner join
                            sys.objects c on a.object_id=c.object_id and c.Type='U'
                        left join
                            syscomments d on a.default_object_id=d.ID
                        left join
                            sys.extended_properties e on e.major_id=c.object_id and e.minor_id=a.Column_id and e.class=1 
                        left join
                            sys.extended_properties f on f.major_id=c.object_id and f.minor_id=0 and f.class=1
	                    where c.name='" + tableName + "'";

            DataTable table = transaction.ExceuteDataTable(sql);
            List<EntityPropertyModel> tableList = new List<EntityPropertyModel>();
            foreach (DataRow row in table.Rows)
            {
                EntityPropertyModel model = new EntityPropertyModel();
                model.Att.ColumnName = row["ColumnName"].ToString();

                //model.Att.ColumnType = row["ColumnType"].ToString().ToUpper();


                string type = row["ColumnType"].ToString().ToUpper();
                if (type == "IMAGE" || type == "DATETIME")
                {
                    model.Att.ColumnType = type;
                }
                else if (type == "VARCHAR")
                {
                    model.Att.ColumnType = type + "(" + row["Lenght"].ToString() + ")";
                }
                else if (type == "DECIMAL")
                {
                    if (row["Lenght"].ToString() == "")
                    {
                        model.Att.ColumnType = type;
                    }
                    else
                    {
                        if (row["Lenght"].ToString() != "" && row["RightLenght"].ToString() == "0")
                        {
                            model.Att.ColumnType = type + "(" + row["Lenght"].ToString() + ")";
                        }
                        else if (row["Lenght"].ToString() != "0" && row["RightLenght"].ToString() != "0")
                        {
                            model.Att.ColumnType = type + "(" + row["Lenght"].ToString() + "," + row["RightLenght"].ToString() + ")";
                        }
                    }
                }
                else
                {
                    model.Att.ColumnType = type;
                }




                model.Att.Information = row["ColumnInfo"].ToString();
                if (row["IsNull"].ToString() == "Y")
                {
                    model.Att.Required = false;
                }
                else
                {
                    model.Att.Required = true;
                }

                if (row["IsKey"].ToString() == "Y")
                {
                    model.Att.IsKey = true;
                }
                else
                {
                    model.Att.IsKey = false;
                }
                model.Att.DefaultValue = row["ColumnDefaultValue"].ToString();

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
            string sql = "ALTER TABLE " + GetTableChar(tableName) + " MODIFY (" + FieldString(model) + ")";
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
            string ordersql = " order by (select 0) ";
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
                return string.Format("select {0} from {1} {2} {3}", selectsql, tablename, wherestr, ordersql);
            }
            else
            {
                return string.Format("select * from ( select row_number() over ({0}) row,{1} from {2} ) p {3} and  row between {4} and {5}", ordersql, selectsql, tablename, wherestr, beginnum, endnum);
            }
        }

        public string FieldType(EntityPropertyModel _field)
        {
            string result = string.Empty;
            switch (_field.FieldType)
            {
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
                    result = "INT";
                    break;
                case "System.Int64":
                    result = "BIGINT";
                    break;
                case "System.Single":
                    if (_field.Att.NumberSize == 0 && _field.Att.NumberPrecision == 0)
                    {
                        result = "REAL(7,1)";
                    }
                    else if (_field.Att.NumberSize != 0 && _field.Att.NumberPrecision == 0)
                    {
                        if (_field.Att.NumberSize < 1 || _field.Att.NumberSize > 7)
                            throw new Exception("FLOAT类型的长度必须是\">=1\",\"<=7\"");
                        result = "REAL(" + _field.Att.NumberSize + ")";
                    }
                    else
                    {
                        if (_field.Att.NumberSize < 1 || _field.Att.NumberSize > 7)
                            throw new Exception("FLOAT类型的长度必须是\">=1\",\"<=7\"");
                        if (_field.Att.NumberPrecision < 1 || _field.Att.NumberPrecision > 7)
                            throw new Exception("FLOAT类型的长度必须是\">=1\",\"<=7\"");
                        if (_field.Att.NumberPrecision > _field.Att.NumberSize)
                            throw new Exception("FLOAT类型的精度不能大于长度");
                        result = "REAL(" + _field.Att.NumberSize + "," + _field.Att.NumberPrecision + ")";

                    }
                    break;
                case "System.Double":
                    if (_field.Att.NumberSize == 0 && _field.Att.NumberPrecision == 0)
                    {
                        result = "FLOAT(15,2)";
                    }
                    else if (_field.Att.NumberSize != 0 && _field.Att.NumberPrecision == 0)
                    {
                        if (_field.Att.NumberSize < 1 || _field.Att.NumberSize > 15)
                            throw new Exception("Double类型的长度必须是\">=1\",\"<=15\"");
                        result = "FLOAT(" + _field.Att.NumberSize + ")";
                    }
                    else
                    {
                        if (_field.Att.NumberSize < 1 || _field.Att.NumberSize > 15)
                            throw new Exception("Double类型的长度必须是\">=1\",\"<=15\"");
                        if (_field.Att.NumberPrecision < 1 || _field.Att.NumberPrecision > 15)
                            throw new Exception("Double类型的精度必须是\">=1\",\"<=15\"");
                        if (_field.Att.NumberPrecision > _field.Att.NumberSize)
                            throw new Exception("Double类型的精度不能大于长度");
                        result = "FLOAT(" + _field.Att.NumberSize + "," + _field.Att.NumberPrecision + ")";
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
                    result = "IMAGE";
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
                            throw new Exception("暂时不支持的数据类型");
                        }
                    }
                    break;
            }
            return result;
        }

    }
}
