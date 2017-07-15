using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using SZORM.Factory.Models;

namespace SZORM.Factory.SqlServer
{
    class StructureToSqlServer : IStructure
    {
        public void ColumnAdd(DbContext dbContext, string tableName, ColumnModel model)
        {
            var SqlGenerator = dbContext._dbContextServiceProvider.CreateDbExpressionTranslator().GetSqlGenerator();
            string sql = "ALTER TABLE " + SqlGenerator.GetQuoteName(tableName) + " ADD (" + FieldString(dbContext, model) + ")";
            dbContext.ExecuteNoQuery(sql);
        }

        public void ColumnEdit(DbContext dbContext, string tableName, ColumnModel model)
        {
            var SqlGenerator = dbContext._dbContextServiceProvider.CreateDbExpressionTranslator().GetSqlGenerator();
            string sql = "ALTER TABLE " + SqlGenerator.GetQuoteName(tableName) + " MODIFY (" + FieldString(dbContext, model)+")";
            dbContext.ExecuteNoQuery(sql);
        }

        public List<ColumnModel> ColumnList(DbContext dbContext, string tableName)
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
            List<ColumnModel> result = new List<ColumnModel>();

            DataTable table = dbContext.ExecuteDataTable(sql);
            for (var i = 0; i < table.Rows.Count; i++)
            {
                var row = table.Rows[i];
                ColumnModel model = new ColumnModel();
                model.Name = row["ColumnName"].ToString();

                string type = row["ColumnType"].ToString().ToUpper();
                if (type == "IMAGE" || type == "DATETIME")
                {
                    model.ColumnFullType = type;
                }
                else if (type == "VARCHAR")
                {
                    model.ColumnFullType = type + "(" + row["Lenght"].ToString() + ")";
                }
                else if (type == "DECIMAL")
                {
                    if (row["Lenght"].ToString() == "")
                    {
                        model.ColumnFullType = type;
                    }
                    else
                    {
                        if (row["Lenght"].ToString() != "" && row["RightLenght"].ToString() == "0")
                        {
                            model.ColumnFullType = type + "(" + row["Lenght"].ToString() + ")";
                        }
                        else if (row["Lenght"].ToString() != "0" && row["RightLenght"].ToString() != "0")
                        {
                            model.ColumnFullType = type + "(" + row["Lenght"].ToString() + "," + row["RightLenght"].ToString() + ")";
                        }
                    }
                }
                else
                {
                    model.ColumnFullType = type;
                }

                model.Required = row["IsNull"].ToString() != "Y";

                model.IsKey = row["IsKey"].ToString() == "Y";

                result.Add(model);
            }
            return result;
        }

        public void CreateTable(DbContext dbContext, TableModel model)
        {
            var SqlGenerator = dbContext._dbContextServiceProvider.CreateDbExpressionTranslator().GetSqlGenerator();
            StringBuilder str = new StringBuilder();
            str.AppendFormat("CREATE TABLE {0}", SqlGenerator.GetQuoteName(model.Name));
            str.Append("(");
            List<string> fields = new List<string>();
            string key = "";
            model.Columns.ForEach(f =>
            {
                if (f.IsKey)
                {
                    key = f.Name;
                }
                fields.Add(FieldString(dbContext, f));
            });
            if (!string.IsNullOrEmpty(key))
                fields.Add("PRIMARY KEY (" + SqlGenerator.GetQuoteName(key) + ")");

            str.Append(string.Join(",", fields));
            str.Append(")");
            dbContext.ExecuteNoQuery(str.ToString());
        }

        public string FieldType(ColumnModel column)
        {
            string result = string.Empty;
            if (column.type == typeof(bool))
            {
                result = "BIT";
            }
            else if (column.type == typeof(string))
            {
                if (column.IsText)
                {
                    result = "TEXT";
                }
                else
                    result = "VARCHAR(" + column.MaxLength + ")";
            }
            else if (column.type == typeof(DateTime))
            {
                result = "DATETIME";
            }
            else if (column.type == typeof(Int16))
            {
                result = "SMALLINT";
            }
            else if (column.type == typeof(Int32))
            {
                result = "INT";
            }
            else if (column.type == typeof(Int64))
            {
                result = "BIGINT";
            }
            else if (column.type == typeof(Single))
            {
                if (column.NumberSize == 0 && column.NumberPrecision == 0)
                {
                    result = "REAL(7,1)";
                }
                else if (column.NumberSize != 0 && column.NumberPrecision == 0)
                {
                    if (column.NumberSize < 1 || column.NumberSize > 7)
                        throw new Exception("FLOAT类型的长度必须是\">=1\",\"<=7\"");
                    result = "REAL(" + column.NumberSize + ")";
                }
                else
                {
                    if (column.NumberSize < 1 || column.NumberSize > 7)
                        throw new Exception("FLOAT类型的长度必须是\">=1\",\"<=7\"");
                    if (column.NumberPrecision < 1 || column.NumberPrecision > 7)
                        throw new Exception("FLOAT类型的长度必须是\">=1\",\"<=7\"");
                    if (column.NumberPrecision > column.NumberSize)
                        throw new Exception("FLOAT类型的精度不能大于长度");
                    result = "REAL(" + column.NumberSize + "," + column.NumberPrecision + ")";

                }
            }
            else if (column.type == typeof(Double))
            {
                if (column.NumberSize == 0 && column.NumberPrecision == 0)
                {
                    result = "FLOAT(15,2)";
                }
                else if (column.NumberSize != 0 && column.NumberPrecision == 0)
                {
                    if (column.NumberSize < 1 || column.NumberSize > 15)
                        throw new Exception("Double类型的长度必须是\">=1\",\"<=15\"");
                    result = "FLOAT(" + column.NumberSize + ")";
                }
                else
                {
                    if (column.NumberSize < 1 || column.NumberSize > 15)
                        throw new Exception("Double类型的长度必须是\">=1\",\"<=15\"");
                    if (column.NumberPrecision < 1 || column.NumberPrecision > 15)
                        throw new Exception("Double类型的精度必须是\">=1\",\"<=15\"");
                    if (column.NumberPrecision > column.NumberSize)
                        throw new Exception("Double类型的精度不能大于长度");
                    result = "FLOAT(" + column.NumberSize + "," + column.NumberPrecision + ")";
                }
            }
            else if (column.type == typeof(Decimal))
            {
                if (column.NumberSize == 0 && column.NumberPrecision == 0)
                {
                    result = "DECIMAL(65)";
                }
                else if (column.NumberSize != 0 && column.NumberPrecision == 0)
                {
                    if (column.NumberSize < 1 || column.NumberSize > 65)
                        throw new Exception("Decimal类型的长度必须是\">=1\",\"<=65\"");
                    result = "DECIMAL(" + column.NumberSize + ")";
                }
                else
                {
                    if (column.NumberSize < 1 || column.NumberSize > 65)
                        throw new Exception("Decimal类型的长度必须是\">=1\",\"<=65\"");
                    if (column.NumberPrecision < 1 || column.NumberPrecision > 30)
                        throw new Exception("Decimal类型的精度必须是\">=1\",\"<=30\"");
                    if (column.NumberPrecision > column.NumberSize)
                        throw new Exception("Decimal类型的精度不能大于长度");
                    result = "DECIMAL(" + column.NumberSize + "," + column.NumberPrecision + ")";
                }
            }
            else if (column.type == typeof(Byte[]))
            {
                result = "IMAGE";
            }
            else
            {
                if (column.type.BaseType == typeof(Enum))
                {
                    result = "INT";
                }
                else
                    throw new Exception("暂时不支持的数据类型:" + column.Name + "-" + column.type.ToString());
            }
            return result;
        }

        public List<TableModel> TableList(DbContext dbContext)
        {
            string sql = "select TableName=c.Name,TableInfo=isnull(f.[value],'') from sys.objects c left join sys.extended_properties f on f.major_id=c.object_id where  Type='U'";

            List<TableModel> tableList = new List<TableModel>();
            DataTable table = dbContext.ExecuteDataTable(sql);
            for (var i = 0; i < table.Rows.Count; i++)
            {
                var row = table.Rows[i];
                var name = row["TableName"].ToString();
                tableList.Add(new TableModel { Name = name });
            }
            return tableList;
        }
        public string FieldString(DbContext dbContext, ColumnModel column)
        {
            var SqlGenerator = dbContext._dbContextServiceProvider.CreateDbExpressionTranslator().GetSqlGenerator();
            StringBuilder str = new StringBuilder();
            str.AppendFormat("{0} {1}", SqlGenerator.GetQuoteName(column.Name), FieldType(column));
            if (column.Required)
                str.Append("  NOT NULL ");

            return str.ToString();
        }
    }
}
