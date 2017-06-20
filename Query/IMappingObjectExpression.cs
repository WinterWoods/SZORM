﻿using SZORM.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using SZORM.DbExpressions;
using SZORM.Query.Mapping;

namespace SZORM.Query
{
    public interface IMappingObjectExpression
    {
        IObjectActivatorCreator GenarateObjectActivatorCreator(DbSqlQueryExpression sqlQuery);
        IMappingObjectExpression ToNewObjectExpression(DbSqlQueryExpression sqlQuery, DbTable table);
        void AddConstructorParameter(ParameterInfo p, DbExpression exp);
        void AddConstructorEntityParameter(ParameterInfo p, IMappingObjectExpression exp);
        void AddMemberExpression(MemberInfo memberInfo, DbExpression exp);
        void AddNavMemberExpression(MemberInfo memberInfo, IMappingObjectExpression moe);
        DbExpression GetMemberExpression(MemberInfo memberInfo);
        IMappingObjectExpression GetNavMemberExpression(MemberInfo memberInfo);
        DbExpression GetDbExpression(MemberExpression memberExpressionDeriveParameter);
        IMappingObjectExpression GetNavMemberExpression(MemberExpression exp);

        void SetNullChecking(DbExpression exp);
    }

    public static class MappingObjectExpressionHelper
    {
        public static DbExpression TryGetOrAddNullChecking(DbSqlQueryExpression sqlQuery, DbTable table, DbExpression exp)
        {
            if (exp == null)
                return null;

            List<DbColumnSegment> columnList = sqlQuery.ColumnSegments;
            DbColumnSegment columnSeg = null;

            columnSeg = columnList.Where(a => DbExpressionEqualityComparer.EqualsCompare(a.Body, exp)).FirstOrDefault();

            if (columnSeg == null)
            {
                string alias = Utils.GenerateUniqueColumnAlias(sqlQuery);
                columnSeg = new DbColumnSegment(exp, alias);

                columnList.Add(columnSeg);
            }

            DbColumnAccessExpression cae = new DbColumnAccessExpression(table, DbColumn.MakeColumn(columnSeg.Body, columnSeg.Alias));
            return cae;
        }
        public static int? TryGetOrAddColumn(DbSqlQueryExpression sqlQuery, DbExpression exp, string addDefaultAlias = UtilConstants.DefaultColumnAlias)
        {
            if (exp == null)
                return null;

            List<DbColumnSegment> columnList = sqlQuery.ColumnSegments;
            DbColumnSegment columnSeg = null;

            int? ordinal = null;
            for (int i = 0; i < columnList.Count; i++)
            {
                var item = columnList[i];
                if (DbExpressionEqualityComparer.EqualsCompare(item.Body, exp))
                {
                    ordinal = i;
                    columnSeg = item;
                    break;
                }
            }

            if (ordinal == null)
            {
                string alias = Utils.GenerateUniqueColumnAlias(sqlQuery, addDefaultAlias);
                columnSeg = new DbColumnSegment(exp, alias);

                columnList.Add(columnSeg);
                ordinal = columnList.Count - 1;
            }

            return ordinal.Value;
        }
        public static DbColumnAccessExpression ParseColumnAccessExpression(DbSqlQueryExpression sqlQuery, DbTable table, DbExpression exp, string defaultAlias = UtilConstants.DefaultColumnAlias)
        {
            string alias = Utils.GenerateUniqueColumnAlias(sqlQuery, defaultAlias);
            DbColumnSegment columnSeg = new DbColumnSegment(exp, alias);

            sqlQuery.ColumnSegments.Add(columnSeg);

            DbColumnAccessExpression cae = new DbColumnAccessExpression(table, DbColumn.MakeColumn(exp, alias));
            return cae;
        }
    }
}
