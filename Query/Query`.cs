﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SZORM.DbExpressions;
using SZORM.Infrastructure;
using SZORM.Query.Internals;
using SZORM.Query.QueryExpressions;
using SZORM.Utility;

namespace SZORM.Query
{
    class Query<T> : QueryBase, IQuery<T>
    {
        static readonly List<Expression> EmptyArgumentList = new List<Expression>(0);

        DbContext _dbContext;
        QueryExpression _expression;
        
        public DbContext DbContext { get { return this._dbContext; } }

        

        public Query(DbContext dbContext)
            : this(dbContext, new RootQueryExpression(typeof(T)))
        {

        }
        public Query(DbContext dbContext, QueryExpression exp)
        {
            this._dbContext = dbContext;
            this._expression = exp;
        }

        public IQuery<TResult> Select<TResult>(Expression<Func<T, TResult>> selector)
        {
            Checks.NotNull(selector, "selector");
            SelectExpression e = new SelectExpression(typeof(TResult), _expression, selector);
            return new Query<TResult>(this._dbContext, e);
        }

        public IQuery<T> Where(Expression<Func<T, bool>> predicate)
        {
            Checks.NotNull(predicate, "predicate");
            WhereExpression e = new WhereExpression(_expression, typeof(T), predicate);
            return new Query<T>(this._dbContext, e);
        }
        public IOrderedQuery<T> Order<K>(string orderKey, string orderType)
        {
            Checks.NotNull(orderKey, "orderKey");
            Checks.NotNull(orderType, "orderType");
            LambdaExpression keySelector = ConvertToLambda<T>(orderKey);

            QueryExpressionType orderMethod;
           
            if (orderType.ToUpper() == "ASC")
                orderMethod = QueryExpressionType.OrderBy;
            else
                orderMethod = QueryExpressionType.OrderByDesc;
            
            OrderExpression e = new OrderExpression(orderMethod, typeof(T), this._expression, keySelector);
            return new OrderedQuery<T>(this._dbContext, e);
        }
        public IOrderedQuery<T> OrderBy<K>(Expression<Func<T, K>> keySelector)
        {
            Checks.NotNull(keySelector, "keySelector");
            OrderExpression e = new OrderExpression(QueryExpressionType.OrderBy, typeof(T), this._expression, keySelector);
            return new OrderedQuery<T>(this._dbContext, e);
        }
        public IOrderedQuery<T> OrderByDesc<K>(Expression<Func<T, K>> keySelector)
        {
            Checks.NotNull(keySelector, "keySelector");
            OrderExpression e = new OrderExpression(QueryExpressionType.OrderByDesc, typeof(T), this._expression, keySelector);
            return new OrderedQuery<T>(this._dbContext, e);
        }
        public IQuery<T> Skip(int count)
        {
            SkipExpression e = new SkipExpression(typeof(T), this._expression, count);
            return new Query<T>(this._dbContext, e);
        }
        public IQuery<T> Take(int count)
        {
            TakeExpression e = new TakeExpression(typeof(T), this._expression, count);
            return new Query<T>(this._dbContext, e);
        }
        public IQuery<T> TakePage(int pageNumber, int pageSize)
        {
            int skipCount = (pageNumber - 1) * pageSize;
            int takeCount = pageSize;

            IQuery<T> q = this.Skip(skipCount).Take(takeCount);
            return q;
        }

        public IGroupingQuery<T> GroupBy<K>(Expression<Func<T, K>> keySelector)
        {
            Checks.NotNull(keySelector, "keySelector");
            return new GroupingQuery<T>(this, keySelector);
        }
        /// <summary>
        /// IJoiningQuery<User, City> user_city = users.InnerJoin<City>((user, city) => user.CityId == city.Id);
        /// </summary>
        /// <typeparam name="TOther"></typeparam>
        /// <param name="on"></param>
        /// <returns></returns>
        public IJoiningQuery<T, TOther> InnerJoin<TOther>(Expression<Func<T, TOther, bool>> on)
        {
            return this.InnerJoin<TOther>(new Query<TOther>(this._dbContext), on);
        }
        /// <summary>
        /// IJoiningQuery<User, City> user_city = users.InnerJoin<City>((user, city) => user.CityId == city.Id);
        /// </summary>
        /// <typeparam name="TOther"></typeparam>
        /// <param name="on"></param>
        /// <returns></returns>
        public IJoiningQuery<T, TOther> LeftJoin<TOther>(Expression<Func<T, TOther, bool>> on)
        {
            return this.LeftJoin<TOther>(new Query<TOther>(this._dbContext), on);
        }
        /// <summary>
        /// IJoiningQuery<User, City> user_city = users.InnerJoin<City>((user, city) => user.CityId == city.Id);
        /// </summary>
        /// <typeparam name="TOther"></typeparam>
        /// <param name="on"></param>
        /// <returns></returns>
        public IJoiningQuery<T, TOther> RightJoin<TOther>(Expression<Func<T, TOther, bool>> on)
        {
            return this.RightJoin<TOther>(new Query<TOther>(this._dbContext), on);
        }
        /// <summary>
        /// IJoiningQuery<User, City> user_city = users.InnerJoin<City>((user, city) => user.CityId == city.Id);
        /// </summary>
        /// <typeparam name="TOther"></typeparam>
        /// <param name="on"></param>
        /// <returns></returns>
        public IJoiningQuery<T, TOther> FullJoin<TOther>(Expression<Func<T, TOther, bool>> on)
        {
            return this.FullJoin<TOther>(new Query<TOther>(this._dbContext), on);
        }
        
        public IJoiningQuery<T, TOther> InnerJoin<TOther>(IQuery<TOther> q, Expression<Func<T, TOther, bool>> on)
        {

            Checks.NotNull(q, "q");
            Checks.NotNull(on, "on");
            return new JoiningQuery<T, TOther>(this, (Query<TOther>)q, DbJoinType.InnerJoin, on);
        }
        public IJoiningQuery<T, TOther> LeftJoin<TOther>(IQuery<TOther> q, Expression<Func<T, TOther, bool>> on)
        {
            Checks.NotNull(q, "q");
            Checks.NotNull(on, "on");
            return new JoiningQuery<T, TOther>(this, (Query<TOther>)q, DbJoinType.LeftJoin, on);
        }
        public IJoiningQuery<T, TOther> RightJoin<TOther>(IQuery<TOther> q, Expression<Func<T, TOther, bool>> on)
        {
            Checks.NotNull(q, "q");
            Checks.NotNull(on, "on");
            return new JoiningQuery<T, TOther>(this, (Query<TOther>)q, DbJoinType.RightJoin, on);
        }
        public IJoiningQuery<T, TOther> FullJoin<TOther>(IQuery<TOther> q, Expression<Func<T, TOther, bool>> on)
        {
            Checks.NotNull(q, "q");
            Checks.NotNull(on, "on");
            return new JoiningQuery<T, TOther>(this, (Query<TOther>)q, DbJoinType.FullJoin, on);
        }

        public T First()
        {
            var q = (Query<T>)this.Take(1);
            IEnumerable<T> iterator = q.GenerateIterator();
            return iterator.First();
        }
        public T First(Expression<Func<T, bool>> predicate)
        {
            return this.Where(predicate).First();
        }
        public T FirstOrDefault()
        {
            var q = (Query<T>)this.Take(1);
            IEnumerable<T> iterator = q.GenerateIterator();
            return iterator.FirstOrDefault();
        }
        public T FirstOrDefault(Expression<Func<T, bool>> predicate)
        {
            return this.Where(predicate).FirstOrDefault();
        }
        public List<T> ToList()
        {
            IEnumerable<T> iterator = this.GenerateIterator();
            return iterator.ToList();
        }

        public bool Any()
        {
            var q = (Query<string>)this.Select(a => "1").Take(1);
            return q.GenerateIterator().Any();
        }
        public bool Any(Expression<Func<T, bool>> predicate)
        {
            return this.Where(predicate).Any();
        }

        public int Count()
        {
            return this.ExecuteAggregateQuery<int>(GetCalledMethod(() => default(IQuery<T>).Count()), null, false);
        }
        public long LongCount()
        {
            return this.ExecuteAggregateQuery<long>(GetCalledMethod(() => default(IQuery<T>).LongCount()), null, false);
        }

        public TResult Max<TResult>(Expression<Func<T, TResult>> selector)
        {
            return this.ExecuteAggregateQuery<TResult>(GetCalledMethod(() => default(IQuery<T>).Max(default(Expression<Func<T, TResult>>))), selector);
        }
        public TResult Min<TResult>(Expression<Func<T, TResult>> selector)
        {
            return this.ExecuteAggregateQuery<TResult>(GetCalledMethod(() => default(IQuery<T>).Min(default(Expression<Func<T, TResult>>))), selector);
        }

        public int Sum(Expression<Func<T, int>> selector)
        {
            return this.ExecuteAggregateQuery<int>(GetCalledMethod(() => default(IQuery<T>).Sum(default(Expression<Func<T, int>>))), selector);
        }
        public int? Sum(Expression<Func<T, int?>> selector)
        {
            return this.ExecuteAggregateQuery<int?>(GetCalledMethod(() => default(IQuery<T>).Sum(default(Expression<Func<T, int?>>))), selector);
        }
        public long Sum(Expression<Func<T, long>> selector)
        {
            return this.ExecuteAggregateQuery<long>(GetCalledMethod(() => default(IQuery<T>).Sum(default(Expression<Func<T, long>>))), selector);
        }
        public long? Sum(Expression<Func<T, long?>> selector)
        {
            return this.ExecuteAggregateQuery<long?>(GetCalledMethod(() => default(IQuery<T>).Sum(default(Expression<Func<T, long?>>))), selector);
        }
        public decimal Sum(Expression<Func<T, decimal>> selector)
        {
            return this.ExecuteAggregateQuery<decimal>(GetCalledMethod(() => default(IQuery<T>).Sum(default(Expression<Func<T, decimal>>))), selector);
        }
        public decimal? Sum(Expression<Func<T, decimal?>> selector)
        {
            return this.ExecuteAggregateQuery<decimal?>(GetCalledMethod(() => default(IQuery<T>).Sum(default(Expression<Func<T, decimal?>>))), selector);
        }
        public double Sum(Expression<Func<T, double>> selector)
        {
            return this.ExecuteAggregateQuery<double>(GetCalledMethod(() => default(IQuery<T>).Sum(default(Expression<Func<T, double>>))), selector);
        }
        public double? Sum(Expression<Func<T, double?>> selector)
        {
            return this.ExecuteAggregateQuery<double?>(GetCalledMethod(() => default(IQuery<T>).Sum(default(Expression<Func<T, double?>>))), selector);
        }
        public float Sum(Expression<Func<T, float>> selector)
        {
            return this.ExecuteAggregateQuery<float>(GetCalledMethod(() => default(IQuery<T>).Sum(default(Expression<Func<T, float>>))), selector);
        }
        public float? Sum(Expression<Func<T, float?>> selector)
        {
            return this.ExecuteAggregateQuery<float?>(GetCalledMethod(() => default(IQuery<T>).Sum(default(Expression<Func<T, float?>>))), selector);
        }

        public double Average(Expression<Func<T, int>> selector)
        {
            return this.ExecuteAggregateQuery<double>(GetCalledMethod(() => default(IQuery<T>).Average(default(Expression<Func<T, int>>))), selector);
        }
        public double? Average(Expression<Func<T, int?>> selector)
        {
            return this.ExecuteAggregateQuery<double>(GetCalledMethod(() => default(IQuery<T>).Average(default(Expression<Func<T, int?>>))), selector);
        }
        public double Average(Expression<Func<T, long>> selector)
        {
            return this.ExecuteAggregateQuery<double>(GetCalledMethod(() => default(IQuery<T>).Average(default(Expression<Func<T, long>>))), selector);
        }
        public double? Average(Expression<Func<T, long?>> selector)
        {
            return this.ExecuteAggregateQuery<double?>(GetCalledMethod(() => default(IQuery<T>).Average(default(Expression<Func<T, long?>>))), selector);
        }
        public decimal Average(Expression<Func<T, decimal>> selector)
        {
            return this.ExecuteAggregateQuery<decimal>(GetCalledMethod(() => default(IQuery<T>).Average(default(Expression<Func<T, decimal>>))), selector);
        }
        public decimal? Average(Expression<Func<T, decimal?>> selector)
        {
            return this.ExecuteAggregateQuery<decimal?>(GetCalledMethod(() => default(IQuery<T>).Average(default(Expression<Func<T, decimal?>>))), selector);
        }
        public double Average(Expression<Func<T, double>> selector)
        {
            return this.ExecuteAggregateQuery<double>(GetCalledMethod(() => default(IQuery<T>).Average(default(Expression<Func<T, double>>))), selector);
        }
        public double? Average(Expression<Func<T, double?>> selector)
        {
            return this.ExecuteAggregateQuery<double?>(GetCalledMethod(() => default(IQuery<T>).Average(default(Expression<Func<T, double?>>))), selector);
        }
        public float Average(Expression<Func<T, float>> selector)
        {
            return this.ExecuteAggregateQuery<float>(GetCalledMethod(() => default(IQuery<T>).Average(default(Expression<Func<T, float>>))), selector);
        }
        public float? Average(Expression<Func<T, float?>> selector)
        {
            return this.ExecuteAggregateQuery<float?>(GetCalledMethod(() => default(IQuery<T>).Average(default(Expression<Func<T, float?>>))), selector);
        }

        public override QueryExpression QueryExpression { get { return this._expression; } }

        public IEnumerable<T> AsEnumerable()
        {
            return this.GenerateIterator();
        }

        InternalQuery<T> GenerateIterator()
        {
            InternalQuery<T> internalQuery = new InternalQuery<T>(this);
            return internalQuery;
        }


        TResult ExecuteAggregateQuery<TResult>(MethodInfo method, Expression argument, bool checkArgument = true)
        {
            if (checkArgument)
                Checks.NotNull(argument, "argument");

            List<Expression> arguments = argument == null ? EmptyArgumentList : new List<Expression>(1) { argument };

            IEnumerable<TResult> iterator = this.CreateAggregateQuery<TResult>(method, arguments);
            return iterator.Single();
        }
        InternalQuery<TResult> CreateAggregateQuery<TResult>(MethodInfo method, List<Expression> arguments)
        {
            AggregateQueryExpression e = new AggregateQueryExpression(this._expression, method, arguments);
            var q = new Query<TResult>(this._dbContext, e);
            InternalQuery<TResult> iterator = q.GenerateIterator();
            return iterator;
        }
        MethodInfo GetCalledMethod<TResult>(Expression<Func<TResult>> exp)
        {
            var body = (MethodCallExpression)exp.Body;
            return body.Method;
        }


        public override string ToString()
        {
            InternalQuery<T> internalQuery = this.GenerateIterator();
            return internalQuery.ToString();
        }

        static LambdaExpression ConvertToLambda<T>(string memberName)
        {
            Type entityType = typeof(T);

            Type currType = entityType;
            ParameterExpression parameterExp = Expression.Parameter(entityType, "a");
            Expression exp = parameterExp;
            MemberInfo memberIfo = currType.GetProperty(memberName);

            if (memberIfo == null)
                throw new ArgumentException(string.Format("实体类 '{0}' 没有找到该属性 '{1}'", currType.FullName, memberName));

            exp = Expression.MakeMemberAccess(exp, memberIfo);
            currType = exp.Type;

            Type delegateType = null;

            delegateType = typeof(Func<,>).MakeGenericType(new Type[] { typeof(T), exp.Type });

            LambdaExpression lambda = Expression.Lambda(delegateType, exp, parameterExp);

            return lambda;
        }
    }
}
