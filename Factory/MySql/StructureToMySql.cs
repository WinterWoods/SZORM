using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using SZORM.Factory.Models;

namespace SZORM.Factory.MySql
{
    public class StructureToMySql : IStructure
    {
        public void ColumnAdd(DbContext dbContext, string tableName, ColumnModel model)
        {
            var SqlGenerator = dbContext._dbContextServiceProvider.CreateDbExpressionTranslator().GetSqlGenerator();
            string sql = "ALTER TABLE " + SqlGenerator.GetQuoteName(tableName) + " ADD (" + FieldString(dbContext,model) + ")";
            dbContext.ExecuteNoQuery(sql);
        }

        public void ColumnEdit(DbContext dbContext, string tableName, ColumnModel model)
        {
            var SqlGenerator = dbContext._dbContextServiceProvider.CreateDbExpressionTranslator().GetSqlGenerator();
            string sql = "ALTER TABLE " + SqlGenerator.GetQuoteName(tableName) + " MODIFY COLUMN" + FieldString(dbContext,model);
            dbContext.ExecuteNoQuery(sql);
        }

        public List<ColumnModel> ColumnList(DbContext dbContext, string tableName)
        {
            string sql = "show columns from " + tableName;
            List<ColumnModel> result = new List<ColumnModel>();
            
            DataTable table = dbContext.ExecuteDataTable(sql);
            for(var i=0;i< table.Rows.Count;i++)
            {
                var row = table.Rows[i];
                ColumnModel model = new ColumnModel();
                model.Name = row["field"].ToString();
                model.ColumnFullType = row["type"].ToString().ToUpper();
                model.IsKey = row["key"].ToString() == "PRI";
                if (model.IsKey)
                {
                    model.Required = true;

                }
                else
                {
                    model.Required = row["null"].ToString() != "YES";
                }

                model.IsKey = row["key"].ToString() == "PRI";
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
                fields.Add(FieldString(dbContext,f));
            });
            if (!string.IsNullOrEmpty(key))
                fields.Add("PRIMARY KEY (" + SqlGenerator.GetQuoteName(key) + ")");

            str.Append(string.Join(",", fields));
            str.Append(")");
            dbContext.ExecuteNoQuery(str.ToString());
        }
        string FieldString(DbContext dbContext, ColumnModel column)
        {
            var SqlGenerator = dbContext._dbContextServiceProvider.CreateDbExpressionTranslator().GetSqlGenerator();
            StringBuilder str = new StringBuilder();
            str.AppendFormat("{0} {1}", SqlGenerator.GetQuoteName(column.Name), FieldType(column));
            if (column.Required)
                str.Append("  NOT NULL ");

            return str.ToString();
        }
        public string FieldType(ColumnModel column)
        {
            string result = string.Empty;
            if(column.type== typeof(bool))
            {
                result = "TINYINT(1)";
            }
            else if (column.type == typeof(string))
            {
                if (column.IsText) {
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
                result = "INT(11)";
            }
            else if (column.type == typeof(Int64))
            {
                result = "BIGINT(20)";
            }
            else if (column.type == typeof(Single))
            {
                if (column.NumberSize == 0 && column.NumberPrecision == 0)
                {
                    result = "FLOAT(7,1)";
                }
                else if (column.NumberSize != 0 && column.NumberPrecision == 0)
                {
                    if (column.NumberSize < 1 || column.NumberSize > 7)
                        throw new Exception("FLOAT类型的长度必须是\">=1\",\"<=7\"");
                    result = "FLOAT(" + column.NumberSize + ")";
                }
                else
                {
                    if (column.NumberSize < 1 || column.NumberSize > 7)
                        throw new Exception("FLOAT类型的长度必须是\">=1\",\"<=7\"");
                    if (column.NumberPrecision < 1 || column.NumberPrecision > 7)
                        throw new Exception("FLOAT类型的长度必须是\">=1\",\"<=7\"");
                    if (column.NumberPrecision > column.NumberSize)
                        throw new Exception("FLOAT类型的精度不能大于长度");
                    result = "FLOAT(" + column.NumberSize + "," + column.NumberPrecision + ")";

                }
            }
            else if (column.type == typeof(Double))
            {
                if (column.NumberSize == 0 && column.NumberPrecision == 0)
                {
                    result = "DOUBLE(15,2)";
                }
                else if (column.NumberSize != 0 && column.NumberPrecision == 0)
                {
                    if (column.NumberSize < 1 || column.NumberSize > 15)
                        throw new Exception("Double类型的长度必须是\">=1\",\"<=15\"");
                    result = "DOUBLE(" + column.NumberSize + ")";
                }
                else
                {
                    if (column.NumberSize < 1 || column.NumberSize > 15)
                        throw new Exception("Double类型的长度必须是\">=1\",\"<=15\"");
                    if (column.NumberPrecision < 1 || column.NumberPrecision > 15)
                        throw new Exception("Double类型的精度必须是\">=1\",\"<=15\"");
                    if (column.NumberPrecision > column.NumberSize)
                        throw new Exception("Double类型的精度不能大于长度");
                    result = "DOUBLE(" + column.NumberSize + "," + column.NumberPrecision + ")";
                }
            }
            else if (column.type == typeof(Decimal))
            {
                if (column.NumberSize == 0 && column.NumberPrecision == 0)
                {
                    result = "DECIMAL(65,0)";
                }
                else if (column.NumberSize != 0 && column.NumberPrecision == 0)
                {
                    if (column.NumberSize < 1 || column.NumberSize > 65)
                        throw new Exception("Decimal类型的长度必须是\">=1\",\"<=65\"");
                    result = "DECIMAL(" + column.NumberSize + ",0)";
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
                result = "LONGBLOB";
            }
            else 
            {
                if (column.type.BaseType == typeof(Enum))
                {
                    result = "INT(11)";
                }
                else
                throw new Exception("暂时不支持的数据类型:" + column.Name + "-" + column.type.ToString());
            }
            return result;
        }

        public List<TableModel> TableList(DbContext dbContext)
        {
            string sql = "show tables;";

            List<TableModel> tableList = new List<TableModel>();
            DataTable table = dbContext.ExecuteDataTable(sql);
            for (var i = 0; i < table.Rows.Count; i++)
            {
                var row = table.Rows[i];
                var name = row[0].ToString();
                tableList.Add(new TableModel { Name = name });
            }
            return tableList;
        }
    }
}
