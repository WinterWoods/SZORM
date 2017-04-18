using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using SZORM.Factory;

namespace SZORM
{
    /// <summary>
    /// szorm核心类
    /// </summary>
    public static class SZORMCore
    {
        /// <summary>
        /// 用于多参数查询生成的参数列表
        /// </summary>
        private static List<string> Parameters = new List<string>();
        /// <summary>
        /// 当前使用随机参数变量值
        /// </summary>
        private static int ParametersNum = 0;
        /// <summary>
        /// 生成最大的随机参数变量个数
        /// </summary>
        private static int MaxParametersNum = 2000;
        static SZORMCore()
        {
            ParameterRandomStr();
        }
        internal static SqlAndParametersModel AddEntity<TEntity>(this DbSet<TEntity> dbSet, TEntity entity) where TEntity : class, new()
        {
            //获取该表的缓存
            EntityModel _table = dbSet.table;
            //生成该表的添加语句
            //初始化字段
            Dictionary<string, string> fieldAndParms = new Dictionary<string, string>();
            Dictionary<string, object> ParmsAndValues = new Dictionary<string, object>();
            foreach (EntityPropertyModel _field in _table.Fields)
            {
                object obj = GetObjectValue(_field, entity);

                if (_field.Att.IsKey)
                {
                    if(obj==null||string.IsNullOrEmpty(obj.ToString()))
                    {
                        obj = GetNewKey();
                    }
                    
                    //设置生成的主键
                    _field.Property.SetValue(entity, obj, null);
                }
                if (_field.Att.IsAddTime || _field.Att.IsEditTime)
                {
                    obj = DateTime.Now;
                }

                string parmsName = ParameterRandomStr();
                fieldAndParms.Add(_field.Name, parmsName);
                ParmsAndValues.Add(parmsName, obj);
            }
            SqlAndParametersModel model = new SqlAndParametersModel();
            model.Sql = dbSet.context.GenSqlInterface.Insert(_table.EntityName, fieldAndParms);
            model.Data = entity;
            model.Parameters = ParmsAndValues;
            return model;
        }
        internal static SqlAndParametersModel EditEntity<TEntity>(this DbSet<TEntity> dbSet, TEntity entity) where TEntity : class, new()
        {

            //获取该表的缓存
            EntityModel _table = dbSet.table;
            //生成该表的添加语句
            //初始化字段
            Dictionary<string, string> fieldAndParms = new Dictionary<string, string>();
            Dictionary<string, object> ParmsAndValues = new Dictionary<string, object>();
            string keyName = "";
            string Key = "";
            string wherestr = " where 1=1 ";
            foreach (EntityPropertyModel _field in _table.Fields)
            {
                if (_field.Att.IsAddTime)
                {
                    continue;
                }
                object obj = GetObjectValue(_field, entity);
                if (_field.Att.IsEditTime)
                {
                    obj = DateTime.Now;
                }
                string parmsName = ParameterRandomStr();
                fieldAndParms.Add(_field.Name, parmsName);
                ParmsAndValues.Add(parmsName, obj);
                //if(obj==null)
                //{
                //    fieldAndParms.Add(_field.Name, null);
                //}
                //else
                //{

                //    fieldAndParms.Add(_field.Name, parmsName);
                //    ParmsAndValues.Add(parmsName, obj);
                //}

                if (_field.Att.IsKey)
                {
                    keyName = _field.Name;
                    if (obj == null) throw new Exception("删除时主键不允许为空");
                    Key = obj.ToString();
                    wherestr = wherestr + " and " + dbSet.context.GenSqlInterface.GetColumnChar(keyName) + "=" + dbSet.context.GenSqlInterface.ParametersChar() + parmsName + " ";
                }

            }
            SqlAndParametersModel model = new SqlAndParametersModel();
            model.Sql = dbSet.context.GenSqlInterface.Update(_table.EntityName, fieldAndParms, wherestr);
            model.Data = entity;
            model.Parameters = ParmsAndValues;
            return model;
        }
        /// <summary>
        /// () => new Product() { Class = "Test", EndDate = DateTime.Now, StartDate = DateTime.Today, ArabicDescription = new Guid().ToString() }
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <param name="SetExpression"></param>
        /// <param name="whereExpression"></param>
        internal static SqlAndParametersModel EditEntity<TSource>(this ISZORM<TSource> source, Expression<Func<TSource>> SetExpression, Expression<Func<TSource, bool>> whereExpression)
        {

            if (SetExpression == null)
            {
                throw new ArgumentNullException("更新语句更新字段不可为空");
            }
            MemberInitExpression initExpression = SetExpression.Body as MemberInitExpression;
            if (SetExpression == null)
            {
                throw new ArgumentException("更新语句更新字段格式不正确");
            }
            EntityModel _table = source.Context.refListTable.Find(f => f.EntityName == typeof(TSource).Name);
            bool ishavEditTime = false;
            SqlAndParametersModel modelset = new SqlAndParametersModel();
            List<MemberAssignment> memberAssignments = initExpression.Bindings.OfType<MemberAssignment>().ToList();
            List<string> setField = new List<string>();
            foreach (var mem in memberAssignments)
            {
                string parmsName = ParameterRandomStr();
                object obj = GetExpressionValue(mem.Expression);
                var _field = _table.Fields.Find(f => f.Name == mem.Member.Name);
                if (_field.Att.IsEditTime)
                {
                    ishavEditTime = true;
                    obj = DateTime.Now;
                }
                setField.Add(source.Context.GenSqlInterface.GetColumnChar(mem.Member.Name) + "=" + source.Context.GenSqlInterface.ParametersChar() + parmsName);
                modelset.Parameters.Add(parmsName, obj);
            }

            if (!setField.Any()) throw new Exception("不允许更新字段为空");

            if (!ishavEditTime)
            {
                var _field = _table.Fields.Find(f => f.Att.IsEditTime);
                if (_field != null)
                {
                    string parmsName = ParameterRandomStr();
                    setField.Add(source.Context.GenSqlInterface.GetColumnChar(_field.Att.ColumnName) + "=" + source.Context.GenSqlInterface.ParametersChar() + parmsName);
                    modelset.Parameters.Add(parmsName, DateTime.Now);
                }

            }

            string wherestr = " WHERE 1=1";
            SqlAndParametersModel modelwhere = new SqlAndParametersModel();
            if (whereExpression != null)
            {
                modelwhere = source.GetWhereStringExpression(whereExpression.Body);
                wherestr = wherestr + " AND " + modelwhere.Sql;
            }
            foreach (var par in modelwhere.Parameters)
            {
                //modelset.Parameters.Add(source.Context.GenSqlInterface.ParametersChar() + par.Key + source.Context.GenSqlInterface.ParametersChar(), par.Value);
                modelset.Parameters.Add(par.Key, GetValue(par.Value));
            }
            modelset.Sql = string.Format("UPDATE {0} SET {1} {2}", source.Context.GenSqlInterface.GetTableChar(typeof(TSource).Name), string.Join(",", setField), wherestr);

            return modelset;
        }
        internal static SqlAndParametersModel RemoveEntity<TSource>(this ISZORM<TSource> source, Expression<Func<TSource, bool>> whereExpression)
        {
            string wherestr = " WHERE 1=1  ";
            SqlAndParametersModel modelwhere = new SqlAndParametersModel();
            if (whereExpression != null)
            {
                modelwhere = source.GetWhereStringExpression(whereExpression.Body);
                wherestr = wherestr + " AND " + modelwhere.Sql;
            }
            modelwhere.Sql = string.Format("delete from {0} {1}", source.Context.GenSqlInterface.GetTableChar(typeof(TSource).Name), wherestr);
            return modelwhere;
        }

        internal static SqlAndParametersModel RemoveEntity<TEntity>(this DbSet<TEntity> dbSet, TEntity entity) where TEntity : class, new()
        {
            //获取该表的缓存
            EntityModel _table = dbSet.table;
            //生成该表的添加语句
            //初始化字段
            Dictionary<string, string> fieldAndParms = new Dictionary<string, string>();
            Dictionary<string, object> ParmsAndValues = new Dictionary<string, object>();
            string keyName = "";
            string Key = "";
            string wherestr = " WHERE 1=1 ";
            foreach (EntityPropertyModel _field in _table.Fields)
            {

                if (_field.Att.IsKey)
                {
                    object obj = GetObjectValue(_field, entity);
                    string parmsName = ParameterRandomStr();
                    fieldAndParms.Add(_field.Name, parmsName);
                    ParmsAndValues.Add(parmsName, obj);
                    keyName = _field.Name;
                    if (obj == null) throw new Exception("删除时主键不允许为空");
                    Key = obj.ToString();
                    wherestr = wherestr + " AND " + dbSet.context.GenSqlInterface.GetColumnChar(keyName) + "=" + dbSet.context.GenSqlInterface.ParametersChar() + parmsName;
                    break;
                }

            }
            SqlAndParametersModel model = new SqlAndParametersModel();
            model.Sql = dbSet.context.GenSqlInterface.Delete(_table.EntityName, wherestr);
            model.Data = entity;
            model.Parameters = ParmsAndValues;
            return model;
        }
        internal static SqlAndParametersModel RemoveEntity<TEntity>(this DbSet<TEntity> dbSet, string Key) where TEntity : class, new()
        {
            if (string.IsNullOrEmpty(Key))
                throw new Exception("删除时主键不允许为空");
            //获取该表的缓存
            EntityModel _table = dbSet.table;
            //生成该表的添加语句
            //初始化字段
            Dictionary<string, string> fieldAndParms = new Dictionary<string, string>();
            Dictionary<string, object> ParmsAndValues = new Dictionary<string, object>();
            string keyName = "";
            string wherestr = " WHERE 1=1 ";
            foreach (EntityPropertyModel _field in _table.Fields)
            {

                if (_field.Att.IsKey)
                {
                    string parmsName = ParameterRandomStr();
                    fieldAndParms.Add(_field.Name, parmsName);
                    ParmsAndValues.Add(parmsName, Key);
                    keyName = _field.Name;
                    wherestr = wherestr + " AND " + dbSet.context.GenSqlInterface.GetColumnChar(keyName) + "=" + dbSet.context.GenSqlInterface.ParametersChar() + parmsName;
                    break;
                }

            }
            SqlAndParametersModel model = new SqlAndParametersModel();
            model.Sql = dbSet.context.GenSqlInterface.Delete(_table.EntityName, wherestr);
            model.Parameters = ParmsAndValues;
            return model;
        }
        internal static SqlAndParametersModel FindEntity<TEntity>(this DbSet<TEntity> dbSet, string Key) where TEntity : class, new()
        {
            if (string.IsNullOrEmpty(Key))
                throw new Exception("查询单个实例时主键不允许为空");
            //获取该表的缓存
            EntityModel _table = dbSet.table;
            //生成该表的添加语句
            //初始化字段
            Dictionary<string, string> fieldAndParms = new Dictionary<string, string>();
            Dictionary<string, object> ParmsAndValues = new Dictionary<string, object>();
            string keyName = "";
            string wherestr = " WHERE 1=1 ";
            List<string> fields = new List<string>();
            foreach (EntityPropertyModel _field in _table.Fields)
            {

                if (_field.Att.IsKey)
                {
                    string parmsName = ParameterRandomStr();
                    fieldAndParms.Add(_field.Name, parmsName);
                    ParmsAndValues.Add(parmsName, Key);
                    keyName = _field.Name;
                    wherestr = wherestr + " AND " + dbSet.context.GenSqlInterface.GetColumnChar(keyName) + "=" + dbSet.context.GenSqlInterface.ParametersChar() + parmsName;
                }
                fields.Add(dbSet.context.GenSqlInterface.GetColumnChar(_field.Name));
            }
            SqlAndParametersModel model = new SqlAndParametersModel();
            model.Sql = dbSet.context.GenSqlInterface.Query(_table.EntityName, fields, wherestr);
            model.Parameters = ParmsAndValues;
            return model;
        }
        /// <summary>
        /// 查询初始化
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static ISZORM<TSource> AsQuery<TSource>(this ISZORM<TSource> source) where TSource : class, new()
        {
            var tmp = new CacheQuery<TSource>(source.Context);
            tmp.IsGenSql = false;
            return tmp;
        }
        public static ISZORM<TSource> Where<TSource>(this ISZORM<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            source = source.SelectVerification();

            if (predicate == null)
            {
                return source;
            }
            if (source.ExpressionWhere == null) source.ExpressionWhere = predicate.Body;
            else
                source.ExpressionWhere = Expression.AndAlso(source.ExpressionWhere, predicate.Body);
            source.IsGenSql = false;
            return source;
        }
        public static ISZORM<TSource> LeftJoin<TSource, TSource1>(this ISZORM<TSource> source, ISZORM<TSource1> source1, Expression<Func<TSource, TSource1, bool>> predicateOn)
        {
            source = source.SelectVerification();

            //if (predicateOn == null)
            //{
            //    return source;
            //}
            //if (source.ExpressionWhere == null) source.ExpressionWhere = predicateOn.Body;
            //else
            //    source.ExpressionWhere = Expression.AndAlso(source.ExpressionWhere, predicateOn.Body);
            //source.IsGenSql = false;
            return source;
        }
        public static ISZORM<TSource> WhereOr<TSource>(this ISZORM<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            source = source.SelectVerification();
            if (predicate == null)
            {
                return null;
            }
            if (source.ExpressionWhere == null) source.ExpressionWhere = predicate.Body;
            else
                source.ExpressionWhere = Expression.OrElse(source.ExpressionWhere, predicate.Body);
            source.IsGenSql = false;
            return source;
        }
        /// <summary>
        /// 跳过多少行
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        public static ISZORM<TSource> Skip<TSource>(this ISZORM<TSource> source, int num)
        {
            source.BeginRowNum = num;
            return source;
        }
        /// <summary>
        /// 获取多少行
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        public static ISZORM<TSource> Take<TSource>(this ISZORM<TSource> source, int num)
        {
            source.EndRowNum = num;
            return source;
        }
        public static ISZORM<TSource> Order<TSource>(this ISZORM<TSource> source, string filed, string sc)
        {
            EntityModel _table = ReflectionCache.DbContextGet(source.Context).Find(f => f.EntityName == typeof(TSource).Name);
            if (_table.Fields.Any(a => a.Att.ColumnName == filed))
            {
                source = source.SelectVerification();
                if (string.IsNullOrEmpty(filed))
                {
                    return source;
                }

                if (source.ExpressionOrderBy.ContainsKey(filed))
                    source.ExpressionOrderBy.Remove(filed);

                source.ExpressionOrderBy.Add(filed, sc);

            }
            source.IsGenSql = false;
            return source;
        }

        public static ISZORM<TSource> OrderBy<TSource, TResult>(this ISZORM<TSource> source, Expression<Func<TSource, TResult>> predicate)
        {
            source = source.SelectVerification();
            if (predicate == null)
            {
                return source;
            }
            if (predicate.Body.NodeType == ExpressionType.MemberAccess)
            {
                MemberExpression mem = predicate.Body as MemberExpression;
                if (source.ExpressionOrderBy.ContainsKey(mem.Member.Name))
                    source.ExpressionOrderBy.Remove(mem.Member.Name);
                source.ExpressionOrderBy.Add(mem.Member.Name, "ASC");
            }
            else if (predicate.Body.NodeType == ExpressionType.New)
            {
                NewExpression orderExpression = predicate.Body as NewExpression;
                if (orderExpression == null)
                {
                    throw new ArgumentException("错误的格式");
                }
                foreach (var mem in orderExpression.Members)
                {
                    if (source.ExpressionOrderBy.ContainsKey(mem.Name))
                        source.ExpressionOrderBy.Remove(mem.Name);
                    source.ExpressionOrderBy.Add(mem.Name, "ASC");
                }
            }
            source.IsGenSql = false;
            return source;
        }
        public static ISZORM<TSource> OrderDesc<TSource, TResult>(this ISZORM<TSource> source, Expression<Func<TSource, TResult>> predicate)
        {
            source = source.SelectVerification();
            if (predicate == null)
            {
                return source;
            }
            if (predicate.Body.NodeType == ExpressionType.MemberAccess)
            {
                MemberExpression mem = predicate.Body as MemberExpression;
                if (source.ExpressionOrderBy.ContainsKey(mem.Member.Name))
                    source.ExpressionOrderBy.Remove(mem.Member.Name);
                source.ExpressionOrderBy.Add(mem.Member.Name, "DESC");
            }
            else if (predicate.Body.NodeType == ExpressionType.New)
            {
                NewExpression orderExpression = predicate.Body as NewExpression;
                if (orderExpression == null)
                {
                    throw new ArgumentException("错误的格式");
                }
                foreach (var mem in orderExpression.Members)
                {
                    if (source.ExpressionOrderBy.ContainsKey(mem.Name))
                        source.ExpressionOrderBy.Remove(mem.Name);
                    source.ExpressionOrderBy.Add(mem.Name, "DESC");
                }
            }
            source.IsGenSql = false;
            return source;
        }
        /// <summary>
        /// new { s.Key, s.Order_Key }
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="source"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static ISZORM<TSource> Select<TSource, TResult>(this ISZORM<TSource> source, Expression<Func<TSource, TResult>> selector)
        {
            source = source.SelectVerification();
            if (selector == null)
            {
                return source;
            }
            NewExpression newExpression = selector.Body as NewExpression;
            if (newExpression == null)
            {
                throw new ArgumentException("错误的格式");
            }
            foreach (var mem in newExpression.Members)
            {
                if (!source.ExpressionSelect.Contains(mem.Name))
                    source.ExpressionSelect.Add(source.Context.GenSqlInterface.GetColumnChar(mem.Name));
            }
            source.IsGenSql = false;
            return source;
        }


        //声明委托
        internal delegate long AsyncEventHandler<TSource>(ISZORM<TSource> source, bool havWhereSql);
        public static long Count<TSource>(this ISZORM<TSource> source, bool havWhereSql = true)
        {
            source = source.SelectVerification();
            string sql = "";
            if (havWhereSql)
            {
                SqlAndParametersModel where = source.GetWhereStringExpression(source.ExpressionWhere);
                source.IsGenSql = true;
                source.wheresql = where;
                string wheresql = "";
                if (!string.IsNullOrEmpty(where.Sql))
                {
                    wheresql = " WHERE " + where.Sql;
                }


                //转换为标准格式,传去sql生成中生成执行sql
                sql = string.Format("SELECT COUNT(*) FROM {0} {1}", source.Context.GenSqlInterface.GetTableChar(typeof(TSource).Name), wheresql);
                DbParameter[] pars = new DbParameter[where.Parameters.Count];
                int i = 0;
                foreach (var par in where.Parameters)
                {
                    pars[i] = source.Context.transaction.ProviderFactory.CreateParameter();
                    pars[i].ParameterName = par.Key;
                    pars[i].Value = GetValue(par.Value);
                    i++;
                }
                object obj = source.Context.ExecuteScalar(sql, pars);
                return Convert.ToInt64(obj);
            }
            else
            {
                sql = string.Format("SELECT COUNT(*) FROM {0} {1}", source.Context.GenSqlInterface.GetTableChar(typeof(TSource).Name), "");
                object obj = source.Context.ExecuteScalar(sql);
                //var dbexe = source.Context.CreateNewTrans();
                //object obj = dbexe.ExecuteScalar(sql);
                //dbexe.Dispose();
                return Convert.ToInt64(obj);
            }
        }
        public static TResult Min<TSource, TResult>(this ISZORM<TSource> source, Expression<Func<TSource, TResult>> predicate)
        {
            source = source.SelectVerification();
            string sql = "";
            SqlAndParametersModel where = source.GetWhereStringExpression(source.ExpressionWhere);
            source.IsGenSql = true;
            source.wheresql = where;
            string wheresql = "";
            if (!string.IsNullOrEmpty(where.Sql))
            {
                wheresql = " WHERE " + where.Sql;
            }
            string field = GetMemberName(predicate);

            //转换为标准格式,传去sql生成中生成执行sql
            sql = string.Format("SELECT MIN({0}) FROM {1} {2}", source.Context.GenSqlInterface.GetColumnChar(field), source.Context.GenSqlInterface.GetTableChar(typeof(TSource).Name), wheresql);
            DbParameter[] pars = new DbParameter[where.Parameters.Count];
            int i = 0;
            foreach (var par in where.Parameters)
            {
                pars[i] = source.Context.transaction.ProviderFactory.CreateParameter();
                pars[i].ParameterName = par.Key;
                pars[i].Value = GetValue(par.Value);
                i++;
            }
            object obj = source.Context.ExecuteScalar(sql, pars);
            if (obj == DBNull.Value)
            {
                return default(TResult);
            }
            else
            {

                return ConvertTo<TResult>(obj);
            }

        }
        public static TResult Max<TSource, TResult>(this ISZORM<TSource> source, Expression<Func<TSource, TResult>> predicate)
        {
            source = source.SelectVerification();
            string sql = "";
            SqlAndParametersModel where = source.GetWhereStringExpression(source.ExpressionWhere);
            source.IsGenSql = true;
            source.wheresql = where;
            string wheresql = "";
            if (!string.IsNullOrEmpty(where.Sql))
            {
                wheresql = " WHERE " + where.Sql;
            }
            string field = GetMemberName(predicate);

            //转换为标准格式,传去sql生成中生成执行sql
            sql = string.Format("SELECT MAX({0}) FROM {1} {2}", source.Context.GenSqlInterface.GetColumnChar(field), source.Context.GenSqlInterface.GetTableChar(typeof(TSource).Name), wheresql);
            DbParameter[] pars = new DbParameter[where.Parameters.Count];
            int i = 0;
            foreach (var par in where.Parameters)
            {
                pars[i] = source.Context.transaction.ProviderFactory.CreateParameter();
                pars[i].ParameterName = par.Key;
                pars[i].Value = GetValue(par.Value);
                i++;
            }
            object obj = source.Context.ExecuteScalar(sql, pars);

            if (obj == DBNull.Value)
            {
                return default(TResult);
            }
            else
            {
                return ConvertTo<TResult>(obj);
            }

        }

        /// <summary>
        /// 求和
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="source"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static TResult Sum<TSource, TResult>(this ISZORM<TSource> source, Expression<Func<TSource, TResult>> predicate)
        {
            source = source.SelectVerification();
            string sql = "";
            SqlAndParametersModel where = source.GetWhereStringExpression(source.ExpressionWhere);
            source.IsGenSql = true;
            source.wheresql = where;
            string wheresql = "";
            if (!string.IsNullOrEmpty(where.Sql))
            {
                wheresql = " WHERE " + where.Sql;
            }
            string field = GetMemberName(predicate);

            //转换为标准格式,传去sql生成中生成执行sql
            sql = string.Format("SELECT SUM({0}) FROM {1} {2}", source.Context.GenSqlInterface.GetColumnChar(field), source.Context.GenSqlInterface.GetTableChar(typeof(TSource).Name), wheresql);
            DbParameter[] pars = new DbParameter[where.Parameters.Count];
            int i = 0;
            foreach (var par in where.Parameters)
            {
                pars[i] = source.Context.transaction.ProviderFactory.CreateParameter();
                pars[i].ParameterName = par.Key;
                pars[i].Value = GetValue(par.Value);
                i++;
            }
            object obj = source.Context.ExecuteScalar(sql, pars);
            if (obj == DBNull.Value)
            {
                return default(TResult);
            }
            else
            {
                return ConvertTo<TResult>(obj);
            }

        }
        /// <summary>
        /// 平均值
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="source"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static TResult Avg<TSource, TResult>(this ISZORM<TSource> source, Expression<Func<TSource, TResult>> predicate)
        {
            source = source.SelectVerification();
            string sql = "";
            SqlAndParametersModel where = source.GetWhereStringExpression(source.ExpressionWhere);
            source.IsGenSql = true;
            source.wheresql = where;
            string wheresql = "";
            if (!string.IsNullOrEmpty(where.Sql))
            {
                wheresql = " WHERE " + where.Sql;
            }
            string field = GetMemberName(predicate);

            //转换为标准格式,传去sql生成中生成执行sql
            sql = string.Format("SELECT AVG({0}) FROM {1} {2}", source.Context.GenSqlInterface.GetColumnChar(field), source.Context.GenSqlInterface.GetTableChar(typeof(TSource).Name), wheresql);
            DbParameter[] pars = new DbParameter[where.Parameters.Count];
            int i = 0;
            foreach (var par in where.Parameters)
            {
                pars[i] = source.Context.transaction.ProviderFactory.CreateParameter();
                pars[i].ParameterName = par.Key;
                pars[i].Value = GetValue(par.Value);
                i++;
            }
            object obj = source.Context.ExecuteScalar(sql, pars);
            if (obj == DBNull.Value)
            {
                return default(TResult);
            }
            else
            {
                return ConvertTo<TResult>(obj);
            }
        }
        public static List<GroupData<TResult>> GroupBy<TSource, TResult>(this ISZORM<TSource> source, Expression<Func<TSource, TResult>> group, Expression<Func<TSource, TResult>> groupCount = null, Expression<Func<TSource, TResult>> groupSum = null, Expression<Func<TSource, TResult>> groupAvg = null)
        {
            List<GroupData<TResult>> result = new List<GroupData<TResult>>();
            source.ExpressionGroup = GetMemberName(group);
            if (string.IsNullOrEmpty(source.ExpressionGroup)) throw new Exception("Group不能空字段.");
            string groupCountStr = GetMemberName(groupCount);
            string groupSumStr = GetMemberName(groupSum);
            string groupAvgStr = GetMemberName(groupAvg);
            StringBuilder str = new StringBuilder();
            str.Append("SELECT ");

            if (!string.IsNullOrEmpty(source.ExpressionGroup))
            {
                str.Append(source.Context.GenSqlInterface.GetColumnChar(source.ExpressionGroup));
            }
            if (!string.IsNullOrEmpty(groupCountStr))
            {
                str.Append(",COUNT(" + source.Context.GenSqlInterface.GetColumnChar(groupCountStr) + ") Count");
            }
            if (!string.IsNullOrEmpty(groupSumStr))
            {
                str.Append(",SUM(" + source.Context.GenSqlInterface.GetColumnChar(groupCountStr) + ") Sum");
            }
            if (!string.IsNullOrEmpty(groupAvgStr))
            {
                str.Append(",AVG(" + source.Context.GenSqlInterface.GetColumnChar(groupCountStr) + ") Avg");
            }
            str.Append(" FROM " + source.Context.GenSqlInterface.GetTableChar(typeof(TSource).Name));


            SqlAndParametersModel where = source.GetWhereStringExpression(source.ExpressionWhere);
            source.IsGenSql = true;
            source.wheresql = where;
            string wheresql = "";
            if (!string.IsNullOrEmpty(where.Sql))
            {
                wheresql = " WHERE " + where.Sql;
            }
            str.Append(" " + wheresql);
            if (!string.IsNullOrEmpty(source.ExpressionGroup))
            {
                str.Append(" GROUP BY " + source.Context.GenSqlInterface.GetColumnChar(source.ExpressionGroup));
            }

            DbParameter[] pars = new DbParameter[where.Parameters.Count];
            int i = 0;
            foreach (var par in where.Parameters)
            {
                pars[i] = source.Context.transaction.ProviderFactory.CreateParameter();
                pars[i].ParameterName = par.Key;
                pars[i].Value = GetValue(par.Value);
                i++;
            }

            var dataTable = source.Context.ExceuteDataTable(str.ToString(), pars);
            for (int j = 0; j < dataTable.Rows.Count; j++)
            {
                GroupData<TResult> _result = new GroupData<TResult>();
                if (!string.IsNullOrEmpty(source.ExpressionGroup))
                {
                    if (dataTable.Rows[j][source.ExpressionGroup] != DBNull.Value)
                    {
                        _result.Data = (TResult)dataTable.Rows[j][source.ExpressionGroup];
                    }
                    else
                    {
                        _result.Data = default(TResult);
                    }

                }
                if (!string.IsNullOrEmpty(groupCountStr))
                {
                    _result.Count = Convert.ToInt64(dataTable.Rows[j]["Count"]);
                }
                if (!string.IsNullOrEmpty(groupSumStr))
                {
                    object obj = dataTable.Rows[j]["SUM"];
                    if (obj == DBNull.Value)
                    {
                        _result.Sum = 0;
                    }
                    else
                    {
                        _result.Sum = Convert.ToInt64(obj);
                    }

                }
                if (!string.IsNullOrEmpty(groupAvgStr))
                {
                    object obj = dataTable.Rows[j]["Avg"];
                    if (obj == DBNull.Value)
                    {
                        _result.Avg = 0;
                    }
                    else
                    {
                        _result.Avg = Convert.ToInt64(obj);
                    }
                }
                result.Add(_result);
            }
            return result;
        }
        private static string GetMemberName<TSource, TResult>(Expression<Func<TSource, TResult>> predicate)
        {
            if (predicate == null)
            {
                return "";
            }
            MemberExpression mem = predicate.Body as MemberExpression;
            if (mem == null)
            {
                return "";
            }
            return mem.Member.Name;
        }
        public static TSource ToEntity<TSource>(this ISZORM<TSource> source) where TSource : class, new()
        {
            var list = source.Take(1).ToList();
            if (list.Any())
            {
                return (TSource)list[0];
            }
            return null;
        }
        public static void ToList<TSource>(this ISZORM<TSource> source, Action<TSource> action, out Action Next, out Action Close) where TSource : class, new()
        {
            source = source.SelectVerification();

            SqlAndParametersModel wheresql = source.GetWhereStringExpression(source.ExpressionWhere);
            source.IsGenSql = true;
            source.wheresql = wheresql;
            //转换为标准格式,传去sql生成中生成执行sql
            string sql = source.Context.GenSqlInterface.Query(source.ExpressionSelect, typeof(TSource).Name, wheresql.Sql, source.ExpressionOrderBy, source.BeginRowNum, source.EndRowNum);


            DbParameter[] pars = new DbParameter[wheresql.Parameters.Count];
            int i = 0;
            foreach (var par in wheresql.Parameters)
            {
                pars[i] = source.Context.transaction.ProviderFactory.CreateParameter();
                pars[i].ParameterName = par.Key;
                pars[i].Value = GetValue(par.Value);
                i++;
            }
            EntityModel _table = ReflectionCache.DbContextGet(source.Context).Find(f => f.EntityName == typeof(TSource).Name);
            source.Context.transaction.ExceuteDataReader(sql, pars).ToListReader<TSource>(_table, action, out Next, out Close);
        }

        public static List<TSource> ToList<TSource>(this ISZORM<TSource> source) where TSource : class, new()
        {
            source = source.SelectVerification();

            SqlAndParametersModel wheresql = source.GetWhereStringExpression(source.ExpressionWhere);
            source.IsGenSql = true;
            source.wheresql = wheresql;
            //转换为标准格式,传去sql生成中生成执行sql
            string sql = source.Context.GenSqlInterface.Query(source.ExpressionSelect, typeof(TSource).Name, wheresql.Sql, source.ExpressionOrderBy, source.BeginRowNum, source.EndRowNum);


            DbParameter[] pars = new DbParameter[wheresql.Parameters.Count];
            int i = 0;
            foreach (var par in wheresql.Parameters)
            {
                pars[i] = source.Context.transaction.ProviderFactory.CreateParameter();
                pars[i].ParameterName = par.Key;
                pars[i].Value = GetValue(par.Value);
                i++;
            }
            EntityModel _table = ReflectionCache.DbContextGet(source.Context).Find(f => f.EntityName == typeof(TSource).Name);
            return source.Context.transaction.ExceuteDataTable(sql, pars).ToList<TSource>(_table);
        }

        public static List<TSource> ToList<TSource>(this ISZORM<TSource> source, out long total) where TSource : class, new()
        {
            source = source.SelectVerification();
            SqlAndParametersModel wheresql = source.GetWhereStringExpression(source.ExpressionWhere);
            source.IsGenSql = true;
            source.wheresql = wheresql;

            AsyncEventHandler<TSource> asy = new AsyncEventHandler<TSource>(Count);
            IAsyncResult ia = asy.BeginInvoke(source, true, null, null);

            total = 0;
            if (source.Context.GenSqlInterface.ToString() == "SZORM.Factory.StructureToMySql")
            {
                total = asy.EndInvoke(ia);
            }
            //转换为标准格式,传去sql生成中生成执行sql
            string sql = source.Context.GenSqlInterface.Query(source.ExpressionSelect, typeof(TSource).Name, wheresql.Sql, source.ExpressionOrderBy, source.BeginRowNum, source.EndRowNum);


            DbParameter[] pars = new DbParameter[wheresql.Parameters.Count];
            int i = 0;
            foreach (var par in wheresql.Parameters)
            {
                pars[i] = source.Context.transaction.ProviderFactory.CreateParameter();
                pars[i].ParameterName = par.Key;
                pars[i].Value = GetValue(par.Value);
                i++;
            }
            EntityModel _table = ReflectionCache.DbContextGet(source.Context).Find(f => f.EntityName == typeof(TSource).Name);
            List<TSource> list = source.Context.transaction.ExceuteDataTable(sql, pars).ToList<TSource>(_table);
            if (source.Context.GenSqlInterface.ToString() != "SZORM.Factory.StructureToMySql")
            {
                total = asy.EndInvoke(ia);
            }

            return list;
        }
        public static List<TSource> ToList<TSource>(this ISZORM<TSource> source, out long total, out long noWhereTotal) where TSource : class, new()
        {
            source = source.SelectVerification();
            SqlAndParametersModel wheresql = source.GetWhereStringExpression(source.ExpressionWhere);
            source.IsGenSql = true;
            source.wheresql = wheresql;

            AsyncEventHandler<TSource> asy = new AsyncEventHandler<TSource>(Count);
            IAsyncResult ia = asy.BeginInvoke(source, true, null, null);

            AsyncEventHandler<TSource> asy1 = new AsyncEventHandler<TSource>(Count);
            IAsyncResult ia1 = asy1.BeginInvoke(source, false, null, null);

            total = 0;
            noWhereTotal = 0;
            if (source.Context.GenSqlInterface.ToString() == "SZORM.Factory.StructureToMySql")
            {
                total = asy.EndInvoke(ia);
                noWhereTotal = asy1.EndInvoke(ia1);
            }


            //转换为标准格式,传去sql生成中生成执行sql
            string sql = source.Context.GenSqlInterface.Query(source.ExpressionSelect, typeof(TSource).Name, wheresql.Sql, source.ExpressionOrderBy, source.BeginRowNum, source.EndRowNum);


            DbParameter[] pars = new DbParameter[wheresql.Parameters.Count];
            int i = 0;
            foreach (var par in wheresql.Parameters)
            {
                pars[i] = source.Context.transaction.ProviderFactory.CreateParameter();
                pars[i].ParameterName = par.Key;
                pars[i].Value = GetValue(par.Value);
                i++;
            }
            EntityModel _table = ReflectionCache.DbContextGet(source.Context).Find(f => f.EntityName == typeof(TSource).Name);
            List<TSource> list = source.Context.transaction.ExceuteDataTable(sql, pars).ToList<TSource>(_table);
            if (source.Context.GenSqlInterface.ToString() != "SZORM.Factory.StructureToMySql")
            {
                total = asy.EndInvoke(ia);
                noWhereTotal = asy1.EndInvoke(ia1);
            }
            return list;
        }
        private static ISZORM<TSource> SelectVerification<TSource>(this ISZORM<TSource> source)
        {
            if (source.ExpressionSelect == null)
            {
                var tmp = new CacheQuery<TSource>(source.Context);
                return tmp;
            }
            return source;
        }
        private static void ClearGenSql<TSource>(this ISZORM<TSource> source)
        {
            source.IsGenSql = false;
            source.wheresql = new SqlAndParametersModel();
        }
        private static object GetObjectValue<TEntity>(this EntityPropertyModel field, TEntity entity)
        {
            object obj = field.Property.GetValue(entity, null);
            if (obj == null) return null;
            if (field.IsEnmu)
            {
                return ((int)field.Property.GetValue(entity, null)).ToString();
            }
            //获取值
            return obj;
        }
        private static string GetNewKey()
        {
            return System.Guid.NewGuid().ToString().Replace("-", "");
        }

        #region 解析Expression语句
        /// <summary>
        /// 解析Where的Expression
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="tableMetadata"></param>
        /// <returns></returns>
        private static SqlAndParametersModel GetWhereStringExpression<TSource>(this ISZORM<TSource> source, Expression expression)
        {
            if (source.IsGenSql) return source.wheresql;
            SqlAndParametersModel model = new SqlAndParametersModel();

            if (expression == null) return model;
            if (expression is BinaryExpression)
            {
                // And OR
                string oper;

                BinaryExpression binaryExpression = expression as BinaryExpression;
                SqlAndParametersModel left = source.GetWhereStringExpression(binaryExpression.Left as Expression);
                foreach (var par in left.Parameters)
                {
                    model.Parameters.Add(par.Key, GetValue(par.Value));
                }

                SqlAndParametersModel right = source.GetWhereStringExpression(binaryExpression.Right as Expression);

                foreach (var par in right.Parameters)
                {
                    model.Parameters.Add(par.Key, GetValue(par.Value));
                }
                //=NULL 換成 IS NULL !=NULL 換成 IS NOT NULL
                if (binaryExpression.NodeType == ExpressionType.Equal && right.Data == null)
                {
                    oper = " IS ";
                    model.Sql = string.Format("({0} {1} NULL)", left.Sql, oper);
                }
                else if (binaryExpression.NodeType == ExpressionType.NotEqual && right.Data == null)
                {
                    oper = " IS NOT ";
                    model.Sql = string.Format("({0} {1} NULL)", left.Sql, oper);
                }
                else
                {
                    string par = ParameterRandomStr();
                    oper = GetOperator(binaryExpression.NodeType);
                    if (right.Data != null)
                    {
                        model.Sql = string.Format("({0} {1} " + source.Context.GenSqlInterface.ParametersChar() + "{2})", left.Sql, oper, par);
                        model.Parameters.Add(par, right.Data);
                    }
                    else
                    {
                        model.Sql = string.Format("({0} {1} {2})", left.Sql, oper, right.Sql);
                    }
                }
            }
            else if (expression is MemberExpression)
            {
                MemberExpression memberExpression = expression as MemberExpression;
                try
                {
                    model.Data = GetExpressionValue(expression);
                }
                catch
                {
                    model.Sql = source.Context.GenSqlInterface.GetColumnChar(memberExpression.Member.Name);
                }
            }
            else if (expression is MethodCallExpression)
            {
                MethodCallExpression methodCallExpression = expression as MethodCallExpression;
                if (methodCallExpression.Method.Name == "Contains")
                {
                    try
                    {
                        string par = ParameterRandomStr();
                        model.Data = GetExpressionValue(methodCallExpression.Arguments[0]);
                        //这个是like
                        MemberExpression member = methodCallExpression.Object as MemberExpression;
                        model.Sql = string.Format("({0} {1} " + source.Context.GenSqlInterface.JoinChar("'%'", source.Context.GenSqlInterface.ParametersChar() + "{2}", "'%')"), source.Context.GenSqlInterface.GetColumnChar(member.Member.Name), " LIKE ", par);
                        model.Parameters.Add(par, model.Data);
                    }
                    catch
                    {
                        string typename = methodCallExpression.Object.Type.Name;
                        if (typename != "List`1")
                        {
                            throw new Exception("in类型必须使用List<>");
                        }
                        Type[] type = methodCallExpression.Object.Type.GetGenericArguments();
                        model.Data = GetExpressionValue(methodCallExpression.Object);

                        List<string> par = new List<string>();

                        if (type[0].Name == "String")
                        {
                            List<string> _data = (List<string>)model.Data;
                            foreach (var d in _data)
                            {
                                string _tmp = ParameterRandomStr();
                                par.Add(source.Context.GenSqlInterface.ParametersChar() + _tmp);
                                model.Parameters.Add(_tmp, d);
                            }
                        }
                        else if (type[0].Name == "Decimal")
                        {
                            List<decimal> _data = (List<decimal>)model.Data;
                            foreach (var d in _data)
                            {
                                string _tmp = ParameterRandomStr();
                                par.Add(source.Context.GenSqlInterface.ParametersChar() + _tmp);
                                model.Parameters.Add(_tmp, d);
                            }
                        }
                        else if (type[0].Name == "Int32")
                        {
                            List<int> _data = (List<int>)model.Data;
                            foreach (var d in _data)
                            {
                                string _tmp = ParameterRandomStr();
                                par.Add(source.Context.GenSqlInterface.ParametersChar() + _tmp);
                                model.Parameters.Add(_tmp, d);
                            }
                        }
                        else
                        {
                            throw new Exception("不支持的in类型");
                        }

                        //这个是in
                        MemberExpression member = methodCallExpression.Arguments[0] as MemberExpression;
                        if (par.Any())
                        {
                            model.Sql = string.Format("({0} {1} ({2}))", source.Context.GenSqlInterface.GetColumnChar(member.Member.Name), "IN", string.Join(",", par));
                        }
                        else
                        {
                            model.Sql = " (1=2) ";
                        }

                    }
                }
                else if (methodCallExpression.Method.Name == "StartsWith")
                {
                    string par = ParameterRandomStr();
                    model.Data = GetExpressionValue(methodCallExpression.Arguments[0]);
                    //这个是like
                    MemberExpression member = methodCallExpression.Object as MemberExpression;
                    //model.Sql = string.Format("({0} {1} ''" + source.Context.GenSqlInterface.JoinChar() + source.Context.GenSqlInterface.ParametersChar() + "{2}" + source.Context.GenSqlInterface.JoinChar() + "'%')", member.Member.Name, " like ", par);
                    model.Sql = string.Format("({0} {1} " + source.Context.GenSqlInterface.JoinChar(source.Context.GenSqlInterface.ParametersChar() + "{2}", "'%')"), source.Context.GenSqlInterface.GetColumnChar(member.Member.Name), " LIKE ", par);
                    model.Parameters.Add(par, model.Data);
                }
                else
                {
                    throw new Exception("暂不支持的查询方法");
                }
                model.Data = null;
            }
            else if (expression is UnaryExpression)
            {
                model = source.GetWhereStringExpression(((UnaryExpression)expression).Operand);
            }
            else
            {
                if (expression.NodeType == ExpressionType.Convert)
                {
                    var aa = ((UnaryExpression)expression);
                }
                ExpressionVisitor exp = new ExpressionVisitor();
                var aaa = exp.Visit(expression);
                model.Data = GetExpressionValue(expression);
            }
            return model;
        }
        //public static Expression VisitUnary(UnaryExpression u)
        //{
        //    Expression operand = Visit(u.Operand);
        //    if (operand != u.Operand)
        //    {
        //        return Expression.MakeUnary(u.NodeType, operand, u.Type, u.Method);
        //    }
        //    return u;
        //}
        /// <summary>
        /// 取得操作子
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static string GetOperator(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.AndAlso:
                    return " AND ";
                case ExpressionType.Equal:
                    return "=";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                case ExpressionType.NotEqual:
                    return "<>";
                case ExpressionType.OrElse:
                    return " OR ";

                default:
                    throw new ArgumentException("不支持的Where操作");
            }
        }
        private static object GetExpressionValue(Expression expression)
        {
            if (expression is ConstantExpression)
            {

                //直接有值的Expression
                var ce = expression as ConstantExpression;

                if (expression.Type.IsEnum)
                {
                    if (expression.Type.FullName.StartsWith("System.Nullable"))
                    {
                        foreach (var value in expression.Type.GetGenericArguments()[0].GetEnumValues())
                        {
                            if (value.ToString() == ce.ToString())
                            {
                                return ((int)value).ToString();
                            }
                        }
                    }
                    else
                    {
                        foreach (var value in expression.Type.GetEnumValues())
                        {
                            if (value.ToString() == ce.ToString())
                            {
                                return ((int)value).ToString();
                            }
                        }
                    }

                }

                return ce.Value;
            }
            else if (expression is UnaryExpression)
            {
                //表示有一元 (Unary) 运算子的运算式
                UnaryExpression ue = expression as UnaryExpression;

                if (ue.Operand is MemberExpression)
                {
                    //取属性值
                    MemberExpression me = ue.Operand as MemberExpression;
                    return GetExpressionValue(me);
                    //return Expression.Lambda(me).Compile().DynamicInvoke();
                }
                else
                {
                    var v = Expression.Lambda(ue.Operand).Compile().DynamicInvoke();
                    if (ue.Operand.Type.IsEnum)
                    {
                        if (ue.Operand.Type.FullName.StartsWith("System.Nullable"))
                        {
                            foreach (var value in ue.Operand.Type.GetGenericArguments()[0].GetEnumValues())
                            {
                                if (value.ToString() == v.ToString())
                                {
                                    return ((int)value).ToString();
                                }
                            }
                        }
                        else
                        {
                            foreach (var value in ue.Operand.Type.GetEnumValues())
                            {
                                if (value.ToString() == v.ToString())
                                {
                                    return ((int)value).ToString();
                                }
                            }
                        }

                    }
                    return v;
                }
            }
            else if (expression is MemberExpression)
            {
                MemberExpression me = expression as MemberExpression;
                try
                {
                    var v = Expression.Lambda(me).Compile().DynamicInvoke();

                    if (me.Type.GetGenericArguments()[0].IsEnum)
                    {
                        if (me.Type.FullName.StartsWith("System.Nullable"))
                        {
                            foreach (var value in me.Type.GetGenericArguments()[0].GetEnumValues())
                            {
                                if (value.ToString() == v.ToString())
                                {
                                    return ((int)value).ToString();
                                }
                            }
                        }
                        else
                        {
                            foreach (var value in me.Type.GetEnumValues())
                            {
                                if (value.ToString() == v.ToString())
                                {
                                    return ((int)value).ToString();
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("未识别的类型");
                    }


                }
                catch
                {
                    return Expression.Lambda(expression).Compile().DynamicInvoke();

                }
            }
            return Expression.Lambda(expression).Compile().DynamicInvoke();
        }
        #endregion
        private static object GetValue(object value)
        {
            if (value != null && value.GetType().ToString() == "System.Boolean")
            {
                return (bool)value ? "1" : "0";
            }
            else
            {
                return value;
            }
        }
        #region 随机字符串
        /// <summary>
        /// 生成随机数
        /// </summary>
        /// <param name="len"></param>
        /// <returns></returns>
        private static string ParameterRandomStr(int len = 8)
        {
            string result = "";
            if (Parameters.Any())
            {
                if (ParametersNum > MaxParametersNum - 1) ParametersNum = 0;
                return Parameters[ParametersNum++];
            }
            else
            {
                string str = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";

                char[] chr = str.ToCharArray();
                Random rd = new Random();
                while (true)
                {
                    if (Parameters.Count > MaxParametersNum) break;
                    result = "";
                    //循环产生字符串
                    for (int i = 0; i <= len - 1; i++)
                    {
                        int no = rd.Next(0, str.Length);
                        result += chr[no].ToString();
                    }
                    lock (Parameters)
                    {
                        result = "p" + result;
                        if (!Parameters.Any(s => s == result))
                        {
                            Parameters.Add(result);
                        }
                    }
                }
            }
            ParametersNum++;
            return result;
        }
        #endregion
        private static T ConvertTo<T>(object convertibleValue)
        {
            if (null == convertibleValue)
            {
                return default(T);
            }

            if (!typeof(T).IsGenericType)
            {
                return (T)Convert.ChangeType(convertibleValue, typeof(T));
            }
            else
            {
                Type genericTypeDefinition = typeof(T).GetGenericTypeDefinition();
                if (genericTypeDefinition == typeof(Nullable<>))
                {
                    return (T)Convert.ChangeType(convertibleValue, Nullable.GetUnderlyingType(typeof(T)));
                }
            }
            throw new InvalidCastException(string.Format("Invalid cast from type \"{0}\" to type \"{1\".", convertibleValue.GetType().FullName, typeof(T).FullName));
        }
    }
}
