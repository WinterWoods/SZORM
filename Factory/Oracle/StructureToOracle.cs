using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using SZORM.Factory.Models;

namespace SZORM.Factory.Oracle
{
    class StructureToOracle : IStructure
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
            
            if (model.IsKey)
            {
                try
                {

                    dbContext.ExecuteNoQuery("ALTER TABLE " + SqlGenerator.GetQuoteName(tableName) + " DROP PRIMARY KEY");
                    // ColumnDropKey(transaction, tableName, model.Att.ColumnName);
                }
                catch (Exception)
                {

                }
                try
                {
                    string sql1 = "ALTER TABLE " + SqlGenerator.GetQuoteName(tableName) + " MODIFY (" + SqlGenerator.GetQuoteName(model.Name) + " null)";
                    dbContext.ExecuteNoQuery(sql1);
                }
                catch (Exception)
                {

                }
            }
            string sql = "ALTER TABLE " + SqlGenerator.GetQuoteName(tableName) + " MODIFY (" + FieldString(dbContext,model) + ")";
            dbContext.ExecuteNoQuery(sql);
        }

        public List<ColumnModel> ColumnList(DbContext dbContext, string tableName)
        {
            string sql = "select * from user_tab_columns where TABLE_NAME='" + tableName.ToUpper() + "'";
            List<ColumnModel> result = new List<ColumnModel>();

            DataTable table = dbContext.ExecuteDataTable(sql);
            for (var i = 0; i < table.Rows.Count; i++)
            {
                var row = table.Rows[i];
                ColumnModel model = new ColumnModel();
                model.Name = row["COLUMN_NAME"].ToString();
                string type = row["DATA_TYPE"].ToString();
                if (type == "BLOB" || type == "DATE")
                {
                    model.ColumnFullType = type;
                }
                else if (type == "VARCHAR2")
                {
                    model.ColumnFullType = type + "(" + row["DATA_LENGTH"].ToString() + ")";
                }
                else if (type == "NUMBER")
                {
                    if (row["DATA_PRECISION"].ToString() == "")
                    {
                        model.ColumnFullType = type;
                    }
                    else
                    {
                        if (row["DATA_PRECISION"].ToString() != "" && row["DATA_SCALE"].ToString() == "0")
                        {
                            model.ColumnFullType = type + "(" + row["DATA_PRECISION"].ToString() + ")";
                        }
                        else if (row["DATA_PRECISION"].ToString() != "0" && row["DATA_SCALE"].ToString() != "0")
                        {
                            model.ColumnFullType = type + "(" + row["DATA_PRECISION"].ToString() + "," + row["DATA_SCALE"].ToString() + ")";
                        }
                    }
                }
                else
                {
                    model.ColumnFullType = type;
                }


                string PKSql = "select count(1)  from user_cons_columns c, user_constraints p where c.constraint_name=p.constraint_name and p.constraint_type='P' and p.table_name='" + tableName.ToUpper() + "' and c.column_name='" + model.Name + "'";
                object obj = dbContext.ExecuteScalar(PKSql);
                model.IsKey = obj.ToString() == "1";

                model.Required = row["NULLABLE"].ToString() != "Y";
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

            str.Append(string.Join(",", fields));
            str.Append(")");
            dbContext.ExecuteNoQuery(str.ToString());
        }

        public string FieldType(ColumnModel column)
        {
            string result = string.Empty;
            if (column.type == typeof(bool))
            {
                result = "NUMBER(1)";
            }
            else if (column.type == typeof(string))
            {
                if (column.IsText)
                {
                    result = "CLOB";
                }
                else
                result = "VARCHAR2(" + column.MaxLength + ")";
            }
            else if (column.type == typeof(DateTime))
            {
                result = "DATE";
            }
            else if (column.type == typeof(Int16))
            {
                if (column.NumberSize != 0)
                {
                    if (column.NumberSize > 4) throw new Exception("Int32类型的长度必须是\"<=4\"");
                    result = "NUMBER(" + column.NumberSize + ")";
                }
                else
                {
                    result = "NUMBER(4)";
                }
            }
            else if (column.type == typeof(Int32))
            {
                if (column.NumberSize != 0)
                {
                    if (column.NumberSize > 9 || column.NumberSize < 5) throw new Exception("Int32类型的长度必须是\">=5\",\"<=9\"");
                    result = "NUMBER(" + column.NumberSize + ")";
                }
                else
                {
                    result = "NUMBER(9)";
                }
            }
            else if (column.type == typeof(Int64))
            {
                if (column.NumberSize != 0)
                {
                    if (column.NumberSize > 19 || column.NumberSize < 10) throw new Exception("Int32类型的长度必须是\">=10\",\"<=19\"");
                    result = "NUMBER(" + column.NumberSize + ")";
                }
                else
                {
                    result = "NUMBER(19)";
                }
            }
            else if (column.type == typeof(Single))
            {
                if (column.NumberSize == 0 && column.NumberPrecision == 0)
                {
                    result = "NUMBER(7,1)";
                }
                else if (column.NumberSize != 0 && column.NumberPrecision == 0)
                {
                    if (column.NumberSize < 1 || column.NumberSize > 7)
                        throw new Exception("FLOAT类型的长度必须是\">=1\",\"<=7\"");
                    result = "NUMBER(" + column.NumberSize + ")";
                }
                else
                {
                    if (column.NumberSize < 1 || column.NumberSize > 7)
                        throw new Exception("FLOAT类型的长度必须是\">=1\",\"<=7\"");
                    if (column.NumberPrecision < 1 || column.NumberPrecision > 7)
                        throw new Exception("FLOAT类型的长度必须是\">=1\",\"<=7\"");
                    if (column.NumberPrecision > column.NumberSize)
                        throw new Exception("FLOAT类型的精度不能大于长度");
                    result = "NUMBER(" + column.NumberSize + "," + column.NumberPrecision + ")";

                }
            }
            else if (column.type == typeof(Double))
            {
                if (column.NumberSize == 0 && column.NumberPrecision == 0)
                {
                    result = "NUMBER(15,2)";
                }
                else if (column.NumberSize != 0 && column.NumberPrecision == 0)
                {
                    if (column.NumberSize < 1 || column.NumberSize > 15)
                        throw new Exception("Double类型的长度必须是\">=1\",\"<=15\"");
                    result = "NUMBER(" + column.NumberSize + ")";
                }
                else
                {
                    if (column.NumberSize < 1 || column.NumberSize > 15)
                        throw new Exception("Double类型的长度必须是\">=1\",\"<=15\"");
                    if (column.NumberPrecision < 1 || column.NumberPrecision > 15)
                        throw new Exception("Double类型的精度必须是\">=1\",\"<=15\"");
                    if (column.NumberPrecision > column.NumberSize)
                        throw new Exception("Double类型的精度不能大于长度");
                    result = "NUMBER(" + column.NumberSize + "," + column.NumberPrecision + ")";
                }
            }
            else if (column.type == typeof(Decimal))
            {
                if (column.NumberSize == 0 && column.NumberPrecision == 0)
                {
                    result = "NUMBER(38)";
                }
                else if (column.NumberSize != 0 && column.NumberPrecision == 0)
                {
                    if (column.NumberSize < 1 || column.NumberSize > 38)
                        throw new Exception("Decimal类型的长度必须是\">=1\",\"<=38\"");
                    result = "NUMBER(" + column.NumberSize + ")";
                }
                else
                {
                    if (column.NumberSize < 1 || column.NumberSize > 38)
                        throw new Exception("Decimal类型的长度必须是\">=1\",\"<=38\"");
                    if (column.NumberPrecision < 1 || column.NumberPrecision > 30)
                        throw new Exception("Decimal类型的精度必须是\">=1\",\"<=30\"");
                    if (column.NumberPrecision > column.NumberSize)
                        throw new Exception("Decimal类型的精度不能大于长度");
                    result = "NUMBER(" + column.NumberSize + "," + column.NumberPrecision + ")";
                }
            }
            else if (column.type == typeof(Byte[]))
            {
                result = "BLOB";
            }
            else
            {
                if (column.type.BaseType == typeof(Enum))
                {
                    result = "NUMBER(3)";
                }
                else
                    throw new Exception("暂时不支持的数据类型:" + column.Name + "-" + column.type.ToString());
            }
            return result;
        }

        public List<TableModel> TableList(DbContext dbContext)
        {
            string sql = "select * from user_tab_comments";

            List<TableModel> tableList = new List<TableModel>();
            DataTable table = dbContext.ExecuteDataTable(sql);
            for (var i = 0; i < table.Rows.Count; i++)
            {
                var row = table.Rows[i];
                var name = row["TABLE_NAME"].ToString();
                tableList.Add(new TableModel { Name = name });
            }
            return tableList;
        }
        string FieldString(DbContext dbContext, ColumnModel column)
        {
            var SqlGenerator = dbContext._dbContextServiceProvider.CreateDbExpressionTranslator().GetSqlGenerator();
            StringBuilder str = new StringBuilder();
            str.AppendFormat("{0} {1}", SqlGenerator.GetQuoteName(column.Name), FieldType(column));
            if (column.IsKey)
                str.Append("  PRIMARY KEY  ");
            if (column.Required)
                str.Append("  NOT NULL ");

            return str.ToString();
        }
    }
}
