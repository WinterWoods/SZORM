﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using SZORM.DbExpressions;
using SZORM.InternalExtensions;
using SZORM.Utility;

namespace SZORM.Factory.SqlServer
{
    partial class SqlGenerator : DbExpressionVisitor<DbExpression>
    {
        public const string ParameterPrefix = "@P_";

        internal ISqlBuilder _sqlBuilder = new SqlBuilder();
        List<DbParam> _parameters = new List<DbParam>();

        DbValueExpressionVisitor _valueExpressionVisitor;

        static readonly Dictionary<string, Action<DbMethodCallExpression, SqlGenerator>> MethodHandlers = InitMethodHandlers();
        static readonly Dictionary<string, Action<DbAggregateExpression, SqlGenerator>> AggregateHandlers = InitAggregateHandlers();
        static readonly Dictionary<MethodInfo, Action<DbBinaryExpression, SqlGenerator>> BinaryWithMethodHandlers = InitBinaryWithMethodHandlers();
        static readonly Dictionary<Type, string> CastTypeMap;
        static readonly Dictionary<Type, Type> NumericTypes;
        static readonly List<string> CacheParameterNames;

        public static readonly ReadOnlyCollection<DbExpressionType> SafeDbExpressionTypes;

        static SqlGenerator()
        {
            List<DbExpressionType> safeDbExpressionTypes = new List<DbExpressionType>();
            safeDbExpressionTypes.Add(DbExpressionType.MemberAccess);
            safeDbExpressionTypes.Add(DbExpressionType.ColumnAccess);
            safeDbExpressionTypes.Add(DbExpressionType.Constant);
            safeDbExpressionTypes.Add(DbExpressionType.Parameter);
            safeDbExpressionTypes.Add(DbExpressionType.Convert);
            SafeDbExpressionTypes = safeDbExpressionTypes.AsReadOnly();


            Dictionary<Type, string> castTypeMap = new Dictionary<Type, string>();
            castTypeMap.Add(typeof(string), "NVARCHAR(MAX)");
            castTypeMap.Add(typeof(byte), "TINYINT");
            castTypeMap.Add(typeof(Int16), "SMALLINT");
            castTypeMap.Add(typeof(int), "INT");
            castTypeMap.Add(typeof(long), "BIGINT");
            castTypeMap.Add(typeof(float), "REAL");
            castTypeMap.Add(typeof(double), "FLOAT");
            castTypeMap.Add(typeof(decimal), "DECIMAL(19,0)");//I think this will be a bug.
            castTypeMap.Add(typeof(bool), "BIT");
            castTypeMap.Add(typeof(DateTime), "DATETIME");
            castTypeMap.Add(typeof(Guid), "UNIQUEIDENTIFIER");
            CastTypeMap = Utils.Clone(castTypeMap);


            Dictionary<Type, Type> numericTypes = new Dictionary<Type, Type>();
            numericTypes.Add(typeof(byte), typeof(byte));
            numericTypes.Add(typeof(sbyte), typeof(sbyte));
            numericTypes.Add(typeof(short), typeof(short));
            numericTypes.Add(typeof(ushort), typeof(ushort));
            numericTypes.Add(typeof(int), typeof(int));
            numericTypes.Add(typeof(uint), typeof(uint));
            numericTypes.Add(typeof(long), typeof(long));
            numericTypes.Add(typeof(ulong), typeof(ulong));
            numericTypes.Add(typeof(float), typeof(float));
            numericTypes.Add(typeof(double), typeof(double));
            numericTypes.Add(typeof(decimal), typeof(decimal));
            NumericTypes = Utils.Clone(numericTypes);


            int cacheParameterNameCount = 2 * 12;
            List<string> cacheParameterNames = new List<string>(cacheParameterNameCount);
            for (int i = 0; i < cacheParameterNameCount; i++)
            {
                string paramName = ParameterPrefix + i.ToString();
                cacheParameterNames.Add(paramName);
            }
            CacheParameterNames = cacheParameterNames;
        }

        public ISqlBuilder SqlBuilder { get { return this._sqlBuilder; } }
        public List<DbParam> Parameters { get { return this._parameters; } }

        DbValueExpressionVisitor ValueExpressionVisitor
        {
            get
            {
                if (this._valueExpressionVisitor == null)
                    this._valueExpressionVisitor = new DbValueExpressionVisitor(this);

                return this._valueExpressionVisitor;
            }
        }

        public static SqlGenerator CreateInstance()
        {
            return new SqlGenerator();
        }

        public override DbExpression Visit(DbEqualExpression exp)
        {
            DbExpression left = exp.Left;
            DbExpression right = exp.Right;

            left = DbExpressionHelper.OptimizeDbExpression(left);
            right = DbExpressionHelper.OptimizeDbExpression(right);

            //明确 left right 其中一边一定为 null
            if (DbExpressionExtension.AffirmExpressionRetValueIsNull(right))
            {
                left.Accept(this);
                this._sqlBuilder.Append(" IS NULL");
                return exp;
            }

            if (DbExpressionExtension.AffirmExpressionRetValueIsNull(left))
            {
                right.Accept(this);
                this._sqlBuilder.Append(" IS NULL");
                return exp;
            }

            AmendDbInfo(left, right);

            left.Accept(this);
            this._sqlBuilder.Append(" = ");
            right.Accept(this);

            return exp;
        }
        public override DbExpression Visit(DbNotEqualExpression exp)
        {
            DbExpression left = exp.Left;
            DbExpression right = exp.Right;

            left = DbExpressionHelper.OptimizeDbExpression(left);
            right = DbExpressionHelper.OptimizeDbExpression(right);

            //明确 left right 其中一边一定为 null
            if (DbExpressionExtension.AffirmExpressionRetValueIsNull(right))
            {
                left.Accept(this);
                this._sqlBuilder.Append(" IS NOT NULL");
                return exp;
            }

            if (DbExpressionExtension.AffirmExpressionRetValueIsNull(left))
            {
                right.Accept(this);
                this._sqlBuilder.Append(" IS NOT NULL");
                return exp;
            }

            AmendDbInfo(left, right);

            left.Accept(this);
            this._sqlBuilder.Append(" <> ");
            right.Accept(this);

            return exp;
        }

        public override DbExpression Visit(DbNotExpression exp)
        {
            this._sqlBuilder.Append("NOT ");
            this._sqlBuilder.Append("(");
            exp.Operand.Accept(this);
            this._sqlBuilder.Append(")");

            return exp;
        }

        public override DbExpression Visit(DbBitAndExpression exp)
        {
            Stack<DbExpression> operands = GatherBinaryExpressionOperand(exp);
            this.ConcatOperands(operands, " & ");

            return exp;
        }
        public override DbExpression Visit(DbAndExpression exp)
        {
            Stack<DbExpression> operands = GatherBinaryExpressionOperand(exp);
            this.ConcatOperands(operands, " AND ");

            return exp;
        }
        public override DbExpression Visit(DbBitOrExpression exp)
        {
            Stack<DbExpression> operands = GatherBinaryExpressionOperand(exp);
            this.ConcatOperands(operands, " | ");

            return exp;
        }
        public override DbExpression Visit(DbOrExpression exp)
        {
            Stack<DbExpression> operands = GatherBinaryExpressionOperand(exp);
            this.ConcatOperands(operands, " OR ");

            return exp;
        }

        // +
        public override DbExpression Visit(DbAddExpression exp)
        {
            MethodInfo method = exp.Method;
            if (method != null)
            {
                Action<DbBinaryExpression, SqlGenerator> handler;
                if (BinaryWithMethodHandlers.TryGetValue(method, out handler))
                {
                    handler(exp, this);
                    return exp;
                }

                throw UtilExceptions.NotSupportedMethod(exp.Method);
            }

            Stack<DbExpression> operands = GatherBinaryExpressionOperand(exp);
            this.ConcatOperands(operands, " + ");

            return exp;
        }
        // -
        public override DbExpression Visit(DbSubtractExpression exp)
        {
            Stack<DbExpression> operands = GatherBinaryExpressionOperand(exp);
            this.ConcatOperands(operands, " - ");

            return exp;
        }
        // *
        public override DbExpression Visit(DbMultiplyExpression exp)
        {
            Stack<DbExpression> operands = GatherBinaryExpressionOperand(exp);
            this.ConcatOperands(operands, " * ");

            return exp;
        }
        // /
        public override DbExpression Visit(DbDivideExpression exp)
        {
            Stack<DbExpression> operands = GatherBinaryExpressionOperand(exp);
            this.ConcatOperands(operands, " / ");

            return exp;
        }
        // %
        public override DbExpression Visit(DbModuloExpression exp)
        {
            Stack<DbExpression> operands = GatherBinaryExpressionOperand(exp);
            this.ConcatOperands(operands, " % ");

            return exp;
        }
        // <
        public override DbExpression Visit(DbLessThanExpression exp)
        {
            exp.Left.Accept(this);
            this._sqlBuilder.Append(" < ");
            exp.Right.Accept(this);

            return exp;
        }
        // <=
        public override DbExpression Visit(DbLessThanOrEqualExpression exp)
        {
            exp.Left.Accept(this);
            this._sqlBuilder.Append(" <= ");
            exp.Right.Accept(this);

            return exp;
        }
        // >
        public override DbExpression Visit(DbGreaterThanExpression exp)
        {
            exp.Left.Accept(this);
            this._sqlBuilder.Append(" > ");
            exp.Right.Accept(this);

            return exp;
        }
        // >=
        public override DbExpression Visit(DbGreaterThanOrEqualExpression exp)
        {
            exp.Left.Accept(this);
            this._sqlBuilder.Append(" >= ");
            exp.Right.Accept(this);

            return exp;
        }


        public override DbExpression Visit(DbAggregateExpression exp)
        {
            Action<DbAggregateExpression, SqlGenerator> aggregateHandler;
            if (!AggregateHandlers.TryGetValue(exp.Method.Name, out aggregateHandler))
            {
                throw UtilExceptions.NotSupportedMethod(exp.Method);
            }

            aggregateHandler(exp, this);
            return exp;
        }


        public override DbExpression Visit(DbTableExpression exp)
        {
            if (exp.Table.Schema != null)
            {
                this.QuoteName(exp.Table.Schema);
                this._sqlBuilder.Append(".");
            }

            this.QuoteName(exp.Table.Name);

            return exp;
        }
        public override DbExpression Visit(DbColumnAccessExpression exp)
        {
            this.QuoteName(exp.Table.Name);
            this._sqlBuilder.Append(".");
            this.QuoteName(exp.Column.Name);

            return exp;
        }
        public override DbExpression Visit(DbFromTableExpression exp)
        {
            this.AppendTableSegment(exp.Table);
            this.VisitDbJoinTableExpressions(exp.JoinTables);

            return exp;
        }
        public override DbExpression Visit(DbJoinTableExpression exp)
        {
            DbJoinTableExpression joinTablePart = exp;
            string joinString = null;

            if (joinTablePart.JoinType == DbJoinType.InnerJoin)
            {
                joinString = " INNER JOIN ";
            }
            else if (joinTablePart.JoinType == DbJoinType.LeftJoin)
            {
                joinString = " LEFT JOIN ";
            }
            else if (joinTablePart.JoinType == DbJoinType.RightJoin)
            {
                joinString = " RIGHT JOIN ";
            }
            else if (joinTablePart.JoinType == DbJoinType.FullJoin)
            {
                joinString = " FULL JOIN ";
            }
            else
                throw new NotSupportedException("JoinType: " + joinTablePart.JoinType);

            this._sqlBuilder.Append(joinString);
            this.AppendTableSegment(joinTablePart.Table);
            this._sqlBuilder.Append(" ON ");
            joinTablePart.Condition.Accept(this);
            this.VisitDbJoinTableExpressions(joinTablePart.JoinTables);

            return exp;
        }


        public override DbExpression Visit(DbSubQueryExpression exp)
        {
            this._sqlBuilder.Append("(");
            exp.SqlQuery.Accept(this);
            this._sqlBuilder.Append(")");

            return exp;
        }
        public override DbExpression Visit(DbSqlQueryExpression exp)
        {
            if (exp.SkipCount != null)
            {
                this.BuildLimitSql(exp);
                return exp;
            }
            else
            {
                //构建常规的查询
                this.BuildGeneralSql(exp);
                return exp;
            }

            throw new NotImplementedException();
        }
        public override DbExpression Visit(DbInsertExpression exp)
        {
            this._sqlBuilder.Append("INSERT INTO ");
            this.QuoteName(exp.Table.Name);
            this._sqlBuilder.Append("(");

            bool first = true;
            foreach (var item in exp.InsertColumns)
            {
                if (first)
                    first = false;
                else
                {
                    this._sqlBuilder.Append(",");
                }

                this.QuoteName(item.Key.Name);
            }

            this._sqlBuilder.Append(")");

            this._sqlBuilder.Append(" VALUES(");
            first = true;
            foreach (var item in exp.InsertColumns)
            {
                if (first)
                    first = false;
                else
                {
                    this._sqlBuilder.Append(",");
                }

                DbExpression valExp = item.Value.OptimizeDbExpression();
                AmendDbInfo(item.Key, valExp);
                valExp.Accept(this.ValueExpressionVisitor);
            }

            this._sqlBuilder.Append(")");

            return exp;
        }
        public override DbExpression Visit(DbUpdateExpression exp)
        {
            this._sqlBuilder.Append("UPDATE ");
            this.QuoteName(exp.Table.Name);
            this._sqlBuilder.Append(" SET ");

            bool first = true;
            foreach (var item in exp.UpdateColumns)
            {
                if (first)
                    first = false;
                else
                    this._sqlBuilder.Append(",");

                this.QuoteName(item.Key.Name);
                this._sqlBuilder.Append("=");

                DbExpression valExp = item.Value.OptimizeDbExpression();
                AmendDbInfo(item.Key, valExp);
                valExp.Accept(this.ValueExpressionVisitor);
            }

            this.BuildWhereState(exp.Condition);

            return exp;
        }
        public override DbExpression Visit(DbDeleteExpression exp)
        {
            this._sqlBuilder.Append("DELETE ");
            this.QuoteName(exp.Table.Name);
            this.BuildWhereState(exp.Condition);

            return exp;
        }


        // then 部分必须返回 C# type，所以得判断是否是诸如 a>1,a=b,in,like 等等的情况，如果是则将其构建成一个 case when 
        public override DbExpression Visit(DbCaseWhenExpression exp)
        {
            this._sqlBuilder.Append("CASE");
            foreach (var whenThen in exp.WhenThenPairs)
            {
                // then 部分得判断是否是诸如 a>1,a=b,in,like 等等的情况，如果是则将其构建成一个 case when 
                this._sqlBuilder.Append(" WHEN ");
                whenThen.When.Accept(this);
                this._sqlBuilder.Append(" THEN ");
                EnsureDbExpressionReturnCSharpBoolean(whenThen.Then).Accept(this);
            }

            this._sqlBuilder.Append(" ELSE ");
            EnsureDbExpressionReturnCSharpBoolean(exp.Else).Accept(this);
            this._sqlBuilder.Append(" END");

            return exp;
        }
        public override DbExpression Visit(DbConvertExpression exp)
        {
            DbExpression stripedExp = DbExpressionExtension.StripInvalidConvert(exp);

            if (stripedExp.NodeType != DbExpressionType.Convert)
            {
                EnsureDbExpressionReturnCSharpBoolean(stripedExp).Accept(this);
                return exp;
            }

            exp = (DbConvertExpression)stripedExp;

            string dbTypeString;
            if (TryGetCastTargetDbTypeString(exp.Operand.Type, exp.Type, out dbTypeString))
            {
                this.BuildCastState(EnsureDbExpressionReturnCSharpBoolean(exp.Operand), dbTypeString);
            }
            else
                EnsureDbExpressionReturnCSharpBoolean(exp.Operand).Accept(this);

            return exp;
        }


        public override DbExpression Visit(DbMethodCallExpression exp)
        {
            Action<DbMethodCallExpression, SqlGenerator> methodHandler;
            if (!MethodHandlers.TryGetValue(exp.Method.Name, out methodHandler))
            {
                throw UtilExceptions.NotSupportedMethod(exp.Method);
            }

            methodHandler(exp, this);
            return exp;
        }
        public override DbExpression Visit(DbMemberExpression exp)
        {
            MemberInfo member = exp.Member;

            if (member.DeclaringType == UtilConstants.TypeOfDateTime)
            {
                if (member == UtilConstants.PropertyInfo_DateTime_Now)
                {
                    this._sqlBuilder.Append("GETDATE()");
                    return exp;
                }

                if (member == UtilConstants.PropertyInfo_DateTime_UtcNow)
                {
                    this._sqlBuilder.Append("GETUTCDATE()");
                    return exp;
                }

                if (member == UtilConstants.PropertyInfo_DateTime_Today)
                {
                    this.BuildCastState("GETDATE()", "DATE");
                    return exp;
                }

                if (member == UtilConstants.PropertyInfo_DateTime_Date)
                {
                    this.BuildCastState(exp.Expression, "DATE");
                    return exp;
                }

                if (this.IsDatePart(exp))
                {
                    return exp;
                }
            }


            DbParameterExpression newExp;
            if (DbExpressionExtension.TryConvertToParameterExpression(exp, out newExp))
            {
                return newExp.Accept(this);
            }

            if (member.Name == "Length" && member.DeclaringType == UtilConstants.TypeOfString)
            {
                this._sqlBuilder.Append("LEN(");
                exp.Expression.Accept(this);
                this._sqlBuilder.Append(")");

                return exp;
            }
            else if (member.Name == "Value" && ReflectionExtension.IsNullable(exp.Expression.Type))
            {
                exp.Expression.Accept(this);
                return exp;
            }

            throw new NotSupportedException(string.Format("'{0}.{1}' is not supported.", member.DeclaringType.FullName, member.Name));
        }
        public override DbExpression Visit(DbConstantExpression exp)
        {
            if (exp.Value == null || exp.Value == DBNull.Value)
            {
                this._sqlBuilder.Append("NULL");
                return exp;
            }

            var objType = exp.Value.GetType();
            if (objType == UtilConstants.TypeOfBoolean)
            {
                this._sqlBuilder.Append(((bool)exp.Value) ? "CAST(1 AS BIT)" : "CAST(0 AS BIT)");
                return exp;
            }
            else if (objType == UtilConstants.TypeOfString)
            {
                this._sqlBuilder.Append("N'", exp.Value, "'");
                return exp;
            }
            else if (objType.IsEnum())
            {
                this._sqlBuilder.Append(((int)exp.Value).ToString());
                return exp;
            }
            else if (NumericTypes.ContainsKey(exp.Value.GetType()))
            {
                this._sqlBuilder.Append(exp.Value);
                return exp;
            }

            DbParameterExpression p = new DbParameterExpression(exp.Value);
            p.Accept(this);

            return exp;
        }
        public override DbExpression Visit(DbParameterExpression exp)
        {
            object paramValue = exp.Value;
            Type paramType = exp.Type;

            if (paramType.IsEnum())
            {
                paramType = UtilConstants.TypeOfInt32;
                if (paramValue != null)
                {
                    paramValue = (int)paramValue;
                }
            }

            if (paramValue == null)
                paramValue = DBNull.Value;

            DbParam p;

            string paramName = GenParameterName(this._parameters.Count);
            p = DbParam.Create(paramName, paramValue, paramType);

            if (paramValue.GetType() == UtilConstants.TypeOfString)
            {
                if (exp.DbType == DbType.AnsiStringFixedLength || exp.DbType == DbType.StringFixedLength)
                    p.Size = ((string)paramValue).Length;
                else if (((string)paramValue).Length <= 4000)
                    p.Size = 4000;
            }

            if (exp.DbType != null)
                p.DbType = exp.DbType;

            this._parameters.Add(p);
            this._sqlBuilder.Append(paramName);
            return exp;
        }


        void AppendTableSegment(DbTableSegment seg)
        {
            seg.Body.Accept(this);
            this._sqlBuilder.Append(" AS ");
            this.QuoteName(seg.Alias);
        }
        internal void AppendColumnSegment(DbColumnSegment seg)
        {
            seg.Body.Accept(this.ValueExpressionVisitor);
            this._sqlBuilder.Append(" AS ");
            this.QuoteName(seg.Alias);
        }
        void AppendOrdering(DbOrdering ordering)
        {
            if (ordering.OrderType == DbOrderType.Asc)
            {
                ordering.Expression.Accept(this);
                this._sqlBuilder.Append(" ASC");
                return;
            }
            else if (ordering.OrderType == DbOrderType.Desc)
            {
                ordering.Expression.Accept(this);
                this._sqlBuilder.Append(" DESC");
                return;
            }

            throw new NotSupportedException("OrderType: " + ordering.OrderType);
        }

        void VisitDbJoinTableExpressions(List<DbJoinTableExpression> tables)
        {
            foreach (var table in tables)
            {
                table.Accept(this);
            }
        }
        void BuildGeneralSql(DbSqlQueryExpression exp)
        {
            this._sqlBuilder.Append("SELECT ");
            if (exp.TakeCount != null)
                this._sqlBuilder.Append("TOP (", exp.TakeCount.ToString(), ") ");

            List<DbColumnSegment> columns = exp.ColumnSegments;
            for (int i = 0; i < columns.Count; i++)
            {
                DbColumnSegment column = columns[i];
                if (i > 0)
                    this._sqlBuilder.Append(",");

                this.AppendColumnSegment(column);
            }

            this._sqlBuilder.Append(" FROM ");
            exp.Table.Accept(this);
            this.BuildWhereState(exp.Condition);
            this.BuildGroupState(exp);
            this.BuildOrderState(exp.Orderings);
        }
        protected virtual void BuildLimitSql(DbSqlQueryExpression exp)
        {
            this._sqlBuilder.Append("SELECT ");
            if (exp.TakeCount != null)
                this._sqlBuilder.Append("TOP (", exp.TakeCount.ToString(), ") ");

            string tableAlias = "T";

            List<DbColumnSegment> columns = exp.ColumnSegments;
            for (int i = 0; i < columns.Count; i++)
            {
                DbColumnSegment column = columns[i];
                if (i > 0)
                    this._sqlBuilder.Append(",");

                this.QuoteName(tableAlias);
                this._sqlBuilder.Append(".");
                this.QuoteName(column.Alias);
                this._sqlBuilder.Append(" AS ");
                this.QuoteName(column.Alias);
            }

            this._sqlBuilder.Append(" FROM ");
            this._sqlBuilder.Append("(");

            //------------------------//
            this._sqlBuilder.Append("SELECT ");
            for (int i = 0; i < columns.Count; i++)
            {
                DbColumnSegment column = columns[i];
                if (i > 0)
                    this._sqlBuilder.Append(",");

                column.Body.Accept(this.ValueExpressionVisitor);
                this._sqlBuilder.Append(" AS ");
                this.QuoteName(column.Alias);
            }

            List<DbOrdering> orderings = exp.Orderings;
            if (orderings.Count == 0)
            {
                DbOrdering ordering = new DbOrdering(UtilConstants.DbParameter_1, DbOrderType.Asc);
                orderings = new List<DbOrdering>(1);
                orderings.Add(ordering);
            }

            string row_numberName = GenRowNumberName(columns);
            this._sqlBuilder.Append(",ROW_NUMBER() OVER(ORDER BY ");
            this.ConcatOrderings(orderings);
            this._sqlBuilder.Append(") AS ");
            this.QuoteName(row_numberName);
            this._sqlBuilder.Append(" FROM ");
            exp.Table.Accept(this);
            this.BuildWhereState(exp.Condition);
            this.BuildGroupState(exp);
            //------------------------//

            this._sqlBuilder.Append(")");
            this._sqlBuilder.Append(" AS ");
            this.QuoteName(tableAlias);
            this._sqlBuilder.Append(" WHERE ");
            this.QuoteName(tableAlias);
            this._sqlBuilder.Append(".");
            this.QuoteName(row_numberName);
            this._sqlBuilder.Append(" > ");
            this._sqlBuilder.Append(exp.SkipCount.ToString());
        }


        internal void BuildWhereState(DbExpression whereExpression)
        {
            if (whereExpression != null)
            {
                this._sqlBuilder.Append(" WHERE ");
                whereExpression.Accept(this);
            }
        }
        internal void BuildOrderState(List<DbOrdering> orderings)
        {
            if (orderings.Count > 0)
            {
                this._sqlBuilder.Append(" ORDER BY ");
                this.ConcatOrderings(orderings);
            }
        }
        void ConcatOrderings(List<DbOrdering> orderings)
        {
            for (int i = 0; i < orderings.Count; i++)
            {
                if (i > 0)
                {
                    this._sqlBuilder.Append(",");
                }

                this.AppendOrdering(orderings[i]);
            }
        }
        internal void BuildGroupState(DbSqlQueryExpression exp)
        {
            var groupSegments = exp.GroupSegments;
            if (groupSegments.Count == 0)
                return;

            this._sqlBuilder.Append(" GROUP BY ");
            for (int i = 0; i < groupSegments.Count; i++)
            {
                if (i > 0)
                    this._sqlBuilder.Append(",");

                groupSegments[i].Accept(this);
            }

            if (exp.HavingCondition != null)
            {
                this._sqlBuilder.Append(" HAVING ");
                exp.HavingCondition.Accept(this);
            }
        }

        void ConcatOperands(IEnumerable<DbExpression> operands, string connector)
        {
            this._sqlBuilder.Append("(");

            bool first = true;
            foreach (DbExpression operand in operands)
            {
                if (first)
                    first = false;
                else
                    this._sqlBuilder.Append(connector);

                operand.Accept(this);
            }

            this._sqlBuilder.Append(")");
            return;
        }
        public override string GetQuoteName(string name)
        {
            return "[" + name + "]";
        }
        void QuoteName(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name");

            this._sqlBuilder.Append(GetQuoteName(name));
        }

        void BuildCastState(DbExpression castExp, string targetDbTypeString)
        {
            this._sqlBuilder.Append("CAST(");
            castExp.Accept(this);
            this._sqlBuilder.Append(" AS ", targetDbTypeString, ")");
        }
        void BuildCastState(object castObject, string targetDbTypeString)
        {
            this._sqlBuilder.Append("CAST(", castObject, " AS ", targetDbTypeString, ")");
        }

        bool IsDatePart(DbMemberExpression exp)
        {
            MemberInfo member = exp.Member;

            if (member == UtilConstants.PropertyInfo_DateTime_Year)
            {
                DbFunction_DATEPART(this, "YEAR", exp.Expression);
                return true;
            }

            if (member == UtilConstants.PropertyInfo_DateTime_Month)
            {
                DbFunction_DATEPART(this, "MONTH", exp.Expression);
                return true;
            }

            if (member == UtilConstants.PropertyInfo_DateTime_Day)
            {
                DbFunction_DATEPART(this, "DAY", exp.Expression);
                return true;
            }

            if (member == UtilConstants.PropertyInfo_DateTime_Hour)
            {
                DbFunction_DATEPART(this, "HOUR", exp.Expression);
                return true;
            }

            if (member == UtilConstants.PropertyInfo_DateTime_Minute)
            {
                DbFunction_DATEPART(this, "MINUTE", exp.Expression);
                return true;
            }

            if (member == UtilConstants.PropertyInfo_DateTime_Second)
            {
                DbFunction_DATEPART(this, "SECOND", exp.Expression);
                return true;
            }

            if (member == UtilConstants.PropertyInfo_DateTime_Millisecond)
            {
                DbFunction_DATEPART(this, "MILLISECOND", exp.Expression);
                return true;
            }

            if (member == UtilConstants.PropertyInfo_DateTime_DayOfWeek)
            {
                this._sqlBuilder.Append("(");
                DbFunction_DATEPART(this, "WEEKDAY", exp.Expression);
                this._sqlBuilder.Append(" - 1)");

                return true;
            }

            return false;
        }

        
    }
}
