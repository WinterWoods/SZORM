using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SZORM;
using SZORM.Core.Visitors;
using SZORM.DbExpressions;
using SZORM.Descriptors;
using SZORM.Exceptions;
using SZORM.Infrastructure;
using SZORM.Query;
using SZORM.Utility;

namespace SZORM
{
    public class DbSet<TEntity> : IDbSet<TEntity>,ISZORM<TEntity> where TEntity : class
    {
        public DbContext DbContext
        {
            get;set;
        }
        /// <summary>
        /// 查询必须使用该方法
        /// </summary>
        /// <returns></returns>
        public IQuery<TEntity> AsQuery()
        {
            return new Query<TEntity>(this.DbContext);
        }
        /// <summary>
        /// 增加
        /// </summary>
        /// <param name="body">() => new User() { Name = "lu", Age = 18, Gender = Gender.Man, CityId = 1, OpTime = DateTime.Now }</param>
        /// <returns></returns>
        public string Add(Expression<Func<TEntity>> body)
        {
            Checks.NotNull(body, "body");

            TypeDescriptor typeDescriptor = TypeDescriptor.GetDescriptor(typeof(TEntity));

            

            Dictionary<MemberInfo, Expression> insertColumns = InitMemberExtractor.Extract(body);

            DbInsertExpression e = new DbInsertExpression(typeDescriptor.Table);

            string keyVal = null;

            foreach (var kv in insertColumns)
            {
                MemberInfo key = kv.Key;
                MappingMemberDescriptor memberDescriptor = typeDescriptor.TryGetMappingMemberDescriptor(key);

                //如果是主键
                if (memberDescriptor.SZColumnAttribute.IsKey)
                {
                    object val = ExpressionEvaluator.Evaluate(kv.Value);
                    if (val == null || string.IsNullOrEmpty(val.ToString()))
                    {
                        val = GetNewKey();
                        keyVal = val.ToString();
                        e.InsertColumns.Add(memberDescriptor.Column, DbExpression.Parameter(keyVal));
                        continue;
                    }
                }
                //如果是添加或修改时间
                if (memberDescriptor.SZColumnAttribute.IsAddTime || memberDescriptor.SZColumnAttribute.IsEditTime)
                {
                    object val = ExpressionEvaluator.Evaluate(kv.Value);
                    val = DateTime.Now;
                    e.InsertColumns.Add(memberDescriptor.Column, DbExpression.Parameter(keyVal));
                    continue;
                }
                e.InsertColumns.Add(memberDescriptor.Column, typeDescriptor.Visitor.Visit(kv.Value));
            }


            IDbExpressionTranslator translator = this.DbContext._dbContextServiceProvider.CreateDbExpressionTranslator();
            List<DbParam> parameters;
            string sql = translator.Translate(e, out parameters);

            this.DbContext.ExecuteNoQuery(sql, parameters.ToArray());
            return keyVal;
        }
        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public TEntity Add(TEntity entity)
        {
            Checks.NotNull(entity, "entity");

            TypeDescriptor typeDescriptor = TypeDescriptor.GetDescriptor(entity.GetType());
            
            Dictionary<MappingMemberDescriptor, DbExpression> insertColumns = new Dictionary<MappingMemberDescriptor, DbExpression>();
            foreach (var kv in typeDescriptor.MappingMemberDescriptors)
            {
                MemberInfo member = kv.Key;
                MappingMemberDescriptor memberDescriptor = kv.Value;
                
                object val = memberDescriptor.GetValue(entity);

                if (memberDescriptor.SZColumnAttribute.IsKey)
                {
                    if(val==null || string.IsNullOrEmpty(val.ToString()))
                    {
                        val = GetNewKey();
                        memberDescriptor.SetValue(entity, val);
                    }
                }
                if (memberDescriptor.SZColumnAttribute.IsAddTime || memberDescriptor.SZColumnAttribute.IsEditTime)
                {
                    val = DateTime.Now;
                    memberDescriptor.SetValue(entity, val);
                }
                DbExpression valExp = DbExpression.Parameter(val, memberDescriptor.MemberInfoType);
                insertColumns.Add(memberDescriptor, valExp);
            }

            DbInsertExpression e = new DbInsertExpression(typeDescriptor.Table);

            foreach (var kv in insertColumns)
            {
                e.InsertColumns.Add(kv.Key.Column, kv.Value);
            }
            IDbExpressionTranslator translator = this.DbContext._dbContextServiceProvider.CreateDbExpressionTranslator();
            List<DbParam> parameters;
            string sql = translator.Translate(e, out parameters);

            this.DbContext.ExecuteNoQuery(sql, parameters.ToArray());

            return entity;
        }
        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public TEntity Edit(TEntity entity)
        {
            Checks.NotNull(entity, "entity");

            TypeDescriptor typeDescriptor = TypeDescriptor.GetDescriptor(entity.GetType());

            object keyVal = null;
            MappingMemberDescriptor keyMemberDescriptor = null;

            Dictionary<MappingMemberDescriptor, DbExpression> updateColumns = new Dictionary<MappingMemberDescriptor, DbExpression>();
            foreach (var kv in typeDescriptor.MappingMemberDescriptors)
            {
                MemberInfo member = kv.Key;
                MappingMemberDescriptor memberDescriptor = kv.Value;

                object val = memberDescriptor.GetValue(entity);
                if (memberDescriptor.SZColumnAttribute.IsKey)
                {
                    if(val==null)
                    throw new SZORMException("主键值不允许为空.");
                    keyVal = val;
                    keyMemberDescriptor = memberDescriptor;
                    continue;
                }
                
                if (memberDescriptor.SZColumnAttribute.IsEditTime)
                {
                    val = DateTime.Now;
                    memberDescriptor.SetValue(entity, val);
                }
                

                DbExpression valExp = DbExpression.Parameter(val, memberDescriptor.MemberInfoType);
                updateColumns.Add(memberDescriptor, valExp);
            }

            DbExpression left = new DbColumnAccessExpression(typeDescriptor.Table, keyMemberDescriptor.Column);
            DbExpression right = DbExpression.Parameter(keyVal, keyMemberDescriptor.MemberInfoType);
            DbExpression conditionExp = new DbEqualExpression(left, right);

            DbUpdateExpression e = new DbUpdateExpression(typeDescriptor.Table, conditionExp);

            foreach (var item in updateColumns)
            {
                e.UpdateColumns.Add(item.Key.Column, item.Value);
            }

            IDbExpressionTranslator translator = this.DbContext._dbContextServiceProvider.CreateDbExpressionTranslator();
            List<DbParam> parameters;
            string sql = translator.Translate(e, out parameters);

            this.DbContext.ExecuteNoQuery(sql, parameters.ToArray());

            return entity;
        }
        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="body">a => new User() { Name = a.Name, Age = a.Age + 100, Gender = Gender.Man, OpTime = DateTime.Now }</param>
        /// <param name="condition">a => a.Id == 1</param>
        /// <returns></returns>
        public int Edit(Expression<Func<TEntity, TEntity>> body, Expression<Func<TEntity, bool>> condition)
        {
            Checks.NotNull(body, "body");

            TypeDescriptor typeDescriptor = TypeDescriptor.GetDescriptor(typeof(TEntity));

            Dictionary<MemberInfo, Expression> updateColumns = InitMemberExtractor.Extract(body);
            DbExpression conditionExp = typeDescriptor.Visitor.VisitFilterPredicate(condition);

            DbUpdateExpression e = new DbUpdateExpression(typeDescriptor.Table, conditionExp);

            foreach (var kv in updateColumns)
            {
                MemberInfo key = kv.Key;
                MappingMemberDescriptor memberDescriptor = typeDescriptor.TryGetMappingMemberDescriptor(key);
                
                e.UpdateColumns.Add(memberDescriptor.Column, typeDescriptor.Visitor.Visit(kv.Value));
            }

            IDbExpressionTranslator translator = this.DbContext._dbContextServiceProvider.CreateDbExpressionTranslator();
            List<DbParam> parameters;
            string sql = translator.Translate(e, out parameters);

            return this.DbContext.ExecuteNoQuery(sql, parameters.ToArray());
        }
        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="condition">a => a.Id == 1</param>
        /// <param name="body">a => new User() { Name = a.Name, Age = a.Age + 100, Gender = Gender.Man, OpTime = DateTime.Now }</param>
        /// <returns></returns>
        public int Edit(Expression<Func<TEntity, bool>> condition, Expression<Func<TEntity, TEntity>> body)
        {
            return Edit(body, condition);
        }
        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="keyValue">主键Key,如果没有主键会报错</param>
        /// <returns></returns>
        public TEntity Find(object keyValue)
        {
            Expression<Func<TEntity, bool>> predicate = PredicateBuilds.BuildPredicate<TEntity>(keyValue);
            var q = new Query<TEntity>(this.DbContext).Where(predicate);

            return q.FirstOrDefault();
        }
        public Task<TEntity> FindAsync(object keyValue)
        {
            return Utils.MakeTask(() => Find(keyValue));
        }
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="keyValue">主键Key,如果没有主键会报错</param>
        /// <returns></returns>
        public int Remove(object keyValue)
        {
            Expression<Func<TEntity, bool>> predicate = PredicateBuilds.BuildPredicate<TEntity>(keyValue);
            return Remove(predicate);
        }
        /// <summary>
        /// 条件删除
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        public int Remove(Expression<Func<TEntity, bool>> condition)
        {
            Checks.NotNull(condition, "condition");

            TypeDescriptor typeDescriptor = TypeDescriptor.GetDescriptor(typeof(TEntity));
            DbExpression conditionExp = typeDescriptor.Visitor.VisitFilterPredicate(condition);

            DbDeleteExpression e = new DbDeleteExpression(typeDescriptor.Table, conditionExp);

            IDbExpressionTranslator translator = this.DbContext._dbContextServiceProvider.CreateDbExpressionTranslator();
            List<DbParam> parameters;
            string sql = translator.Translate(e, out parameters);

            return this.DbContext.ExecuteNoQuery(sql, parameters.ToArray());
        }
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public int Remove(TEntity entity)
        {
            Checks.NotNull(entity, "entity");

            TypeDescriptor typeDescriptor = TypeDescriptor.GetDescriptor(entity.GetType());

            MappingMemberDescriptor keyMemberDescriptor = typeDescriptor.PrimaryKey;
            MemberInfo keyMember = typeDescriptor.PrimaryKey.MemberInfo;
            if (keyMemberDescriptor == null)
            {
                throw new SZORMException(string.Format("该表没有主键不允许这样删除."));
            }

            var keyVal = keyMemberDescriptor.GetValue(entity);

            if (keyVal == null)
                throw new SZORMException(string.Format("{0}主键字段不允许为空.", keyMember.Name));

            DbExpression left = new DbColumnAccessExpression(typeDescriptor.Table, keyMemberDescriptor.Column);
            DbExpression right = new DbParameterExpression(keyVal);
            DbExpression conditionExp = new DbEqualExpression(left, right);

            DbDeleteExpression e = new DbDeleteExpression(typeDescriptor.Table, conditionExp);
            IDbExpressionTranslator translator = this.DbContext._dbContextServiceProvider.CreateDbExpressionTranslator();
            List<DbParam> parameters;
            string sql = translator.Translate(e, out parameters);

            return this.DbContext.ExecuteNoQuery(sql, parameters.ToArray());
        }
        private static string GetNewKey()
        {
            return Guid.NewGuid().ToString().Replace("-", "");
        }

        public Task<TEntity> AddAsync(TEntity entity)
        {
            return Utils.MakeTask(() => Add(entity));
        }

        public Task<string> AddAsync(Expression<Func<TEntity>> body)
        {
            return Utils.MakeTask(() => Add(body));
        }

        public Task<TEntity> EditAsync(TEntity entity)
        {
            return Utils.MakeTask(() => Edit(entity));
        }

        public Task<int> EditAsync(Expression<Func<TEntity, bool>> condition, Expression<Func<TEntity, TEntity>> body)
        {
            return Utils.MakeTask(() => Edit(condition,body));
        }

        public Task<int> EditAsync(Expression<Func<TEntity, TEntity>> body, Expression<Func<TEntity, bool>> condition)
        {
            return Utils.MakeTask(() => Edit(body, condition));
        }

        public Task<int> RemoveAsync(TEntity entity)
        {
            return Utils.MakeTask(() => Remove(entity));
        }

        public Task<int> RemoveAsync(Expression<Func<TEntity, bool>> condition)
        {
            return Utils.MakeTask(() => Remove(condition));
        }

        public Task<int> RemoveAsync(object keyValues)
        {
            return Utils.MakeTask(() => Remove(keyValues));
        }
    }
}
