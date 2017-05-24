﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SZORM.DbExpressions;
using SZORM.InternalExtensions;

namespace SZORM.Factory.SQLite
{
    partial class SqlGenerator : DbExpressionVisitor<DbExpression>
    {
        static string GenParameterName(int ordinal)
        {
            if (ordinal < CacheParameterNames.Count)
            {
                return CacheParameterNames[ordinal];
            }

            return ParameterPrefix + ordinal.ToString();
        }
        static void AmendDbInfo(DbExpression exp1, DbExpression exp2)
        {
            DbColumnAccessExpression datumPointExp = null;
            DbParameterExpression expToAmend = null;

            DbExpression e = Trim_Nullable_Value(exp1);
            if (e.NodeType == DbExpressionType.ColumnAccess && exp2.NodeType == DbExpressionType.Parameter)
            {
                datumPointExp = (DbColumnAccessExpression)e;
                expToAmend = (DbParameterExpression)exp2;
            }
            else if ((e = Trim_Nullable_Value(exp2)).NodeType == DbExpressionType.ColumnAccess && exp1.NodeType == DbExpressionType.Parameter)
            {
                datumPointExp = (DbColumnAccessExpression)e;
                expToAmend = (DbParameterExpression)exp1;
            }
            else
                return;

            if (datumPointExp.Column.DbType != null)
            {
                if (expToAmend.DbType == null)
                    expToAmend.DbType = datumPointExp.Column.DbType;
            }
        }
        static void AmendDbInfo(DbColumn column, DbExpression exp)
        {
            if (column.DbType == null || exp.NodeType != DbExpressionType.Parameter)
                return;

            DbParameterExpression expToAmend = (DbParameterExpression)exp;

            if (expToAmend.DbType == null)
                expToAmend.DbType = column.DbType;
        }
        static DbExpression Trim_Nullable_Value(DbExpression exp)
        {
            DbMemberExpression memberExp = exp as DbMemberExpression;
            if (memberExp == null)
                return exp;

            if (memberExp.Member.Name == "Value" && ReflectionExtension.IsNullable(memberExp.Expression.Type))
                return memberExp.Expression;

            return exp;
        }


        static Stack<DbExpression> GatherBinaryExpressionOperand(DbBinaryExpression exp)
        {
            DbExpressionType nodeType = exp.NodeType;

            Stack<DbExpression> items = new Stack<DbExpression>();
            items.Push(exp.Right);

            DbExpression left = exp.Left;
            while (left.NodeType == nodeType)
            {
                exp = (DbBinaryExpression)left;
                items.Push(exp.Right);
                left = exp.Left;
            }

            items.Push(left);
            return items;
        }
        static void EnsureMethodDeclaringType(DbMethodCallExpression exp, Type ensureType)
        {
            if (exp.Method.DeclaringType != ensureType)
                throw UtilExceptions.NotSupportedMethod(exp.Method);
        }
        static void EnsureMethod(DbMethodCallExpression exp, MethodInfo methodInfo)
        {
            if (exp.Method != methodInfo)
                throw UtilExceptions.NotSupportedMethod(exp.Method);
        }


        static void EnsureTrimCharArgumentIsSpaces(DbExpression exp)
        {
            var m = exp as DbMemberExpression;
            if (m == null)
                throw new NotSupportedException();

            DbParameterExpression p;
            if (!DbExpressionExtension.TryConvertToParameterExpression(m, out p))
            {
                throw new NotSupportedException();
            }

            var arg = p.Value;

            if (arg == null)
                throw new NotSupportedException();

            var chars = arg as char[];
            if (chars.Length != 1 || chars[0] != ' ')
            {
                throw new NotSupportedException();
            }
        }
        static bool TryGetCastTargetDbTypeString(Type sourceType, Type targetType, out string dbTypeString, bool throwNotSupportedException = true)
        {
            dbTypeString = null;

            sourceType = ReflectionExtension.GetUnderlyingType(sourceType);
            targetType = ReflectionExtension.GetUnderlyingType(targetType);

            if (sourceType == targetType)
                return false;

            if (CastTypeMap.TryGetValue(targetType, out dbTypeString))
            {
                return true;
            }

            if (throwNotSupportedException)
                throw new NotSupportedException(AppendNotSupportedCastErrorMsg(sourceType, targetType));
            else
                return false;
        }
        static string AppendNotSupportedCastErrorMsg(Type sourceType, Type targetType)
        {
            return string.Format("Does not support the type '{0}' converted to type '{1}'.", sourceType.FullName, targetType.FullName);
        }

        static void DbFunction_DATEADD(SqlGenerator generator, string interval, DbMethodCallExpression exp)
        {
            /* DATETIME(@P_0,'+' || 1 || ' years') */

            generator._sqlBuilder.Append("DATETIME(");
            exp.Object.Accept(generator);
            generator._sqlBuilder.Append(",'+' || ");
            exp.Arguments[0].Accept(generator);
            generator._sqlBuilder.Append(" || ' ", interval, "'");
            generator._sqlBuilder.Append(")");
        }
        static void DbFunction_DATEPART(SqlGenerator generator, string interval, DbExpression exp)
        {
            /* CAST(STRFTIME('%M','2016-08-06 09:01:24') AS INTEGER) */
            generator._sqlBuilder.Append("CAST(");
            generator._sqlBuilder.Append("STRFTIME('%", interval, "',");
            exp.Accept(generator);
            generator._sqlBuilder.Append(")");
            generator._sqlBuilder.Append(" AS INTEGER)");
        }

        #region AggregateFunction
        static void Aggregate_Count(SqlGenerator generator)
        {
            generator._sqlBuilder.Append("COUNT(1)");
        }
        static void Aggregate_LongCount(SqlGenerator generator)
        {
            generator._sqlBuilder.Append("COUNT(1)");
        }
        static void Aggregate_Max(SqlGenerator generator, DbExpression exp, Type retType)
        {
            AppendAggregateFunction(generator, exp, retType, "MAX", false);
        }
        static void Aggregate_Min(SqlGenerator generator, DbExpression exp, Type retType)
        {
            AppendAggregateFunction(generator, exp, retType, "MIN", false);
        }
        static void Aggregate_Sum(SqlGenerator generator, DbExpression exp, Type retType)
        {
            if (retType.IsNullable())
            {
                AppendAggregateFunction(generator, exp, retType, "SUM", true);
            }
            else
            {
                generator._sqlBuilder.Append("IFNULL(");
                AppendAggregateFunction(generator, exp, retType, "SUM", true);
                generator._sqlBuilder.Append(",");
                generator._sqlBuilder.Append("0");
                generator._sqlBuilder.Append(")");
            }
        }
        static void Aggregate_Average(SqlGenerator generator, DbExpression exp, Type retType)
        {
            AppendAggregateFunction(generator, exp, retType, "AVG", true);
        }

        static void AppendAggregateFunction(SqlGenerator generator, DbExpression exp, Type retType, string functionName, bool withCast)
        {
            string dbTypeString = null;
            if (withCast == true)
            {
                Type underlyingType = ReflectionExtension.GetUnderlyingType(retType);
                if (underlyingType != UtilConstants.TypeOfDecimal/* We don't know the precision and scale,so,we can not cast exp to decimal,otherwise maybe cause problems. */ && CastTypeMap.TryGetValue(underlyingType, out dbTypeString))
                {
                    generator._sqlBuilder.Append("CAST(");
                }
            }

            generator._sqlBuilder.Append(functionName, "(");
            exp.Accept(generator);
            generator._sqlBuilder.Append(")");

            if (dbTypeString != null)
            {
                generator._sqlBuilder.Append(" AS ", dbTypeString, ")");
            }
        }
        #endregion

    }
}
