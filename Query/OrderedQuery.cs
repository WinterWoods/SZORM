﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using SZORM.Query.QueryExpressions;

namespace SZORM.Query
{
    class OrderedQuery<T> : Query<T>, IOrderedQuery<T>
    {
        public OrderedQuery(DbContext dbContext, QueryExpression exp)
            : base(dbContext, exp)
        {

        }
        public IOrderedQuery<T> ThenBy<K>(Expression<Func<T, K>> keySelector)
        {
            OrderExpression e = new OrderExpression(QueryExpressionType.ThenBy, typeof(T), this.QueryExpression, keySelector);
            return new OrderedQuery<T>(this.DbContext, e);
        }
        public IOrderedQuery<T> ThenByDesc<K>(Expression<Func<T, K>> keySelector)
        {
            OrderExpression e = new OrderExpression(QueryExpressionType.ThenByDesc, typeof(T), this.QueryExpression, keySelector);
            return new OrderedQuery<T>(this.DbContext, e);
        }
    }
}
