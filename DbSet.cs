using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using System.Data.Common;
using System.Collections;
using System.Data;
using System.Threading;
using SZORM.Factory;

namespace SZORM
{
    public class DbSet<TEntity> : ISZORM<TEntity> where TEntity : class,new()
    {
        internal DbContext context;
        internal EntityModel table;
        #region Add
        /// <summary>
        /// 新增一个实体类
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public TEntity Add(TEntity entity)
        {
            SqlAndParametersModel model = this.AddEntity(entity);
            
            context.transaction.ExcuteNoQuery(model.Sql, GetParameters(model.Parameters));
            entity = (TEntity)model.Data;
            return entity;
        }
        #endregion
        
        #region Edit
        public TEntity Edit(TEntity entity)
        {
            
            SqlAndParametersModel model = this.EditEntity(entity);
            
            context.transaction.ExcuteNoQuery(model.Sql, GetParameters(model.Parameters));
            entity = (TEntity)model.Data;
            return entity;
        }
        /// <summary>
        /// 如果不出现提示，请加命名空间 using System.Linq.Expressions;  db.Dict.Edit(w => w.Key == "", () => new  Dict { PId="", Key="", Search="" });
        /// </summary>
        /// <param name="whereExpression"></param>
        /// <param name="SetExpression"></param>
        public void Edit(Expression<Func<TEntity, bool>> whereExpression,Expression<Func<TEntity>> SetExpression)
        {

            SqlAndParametersModel model = this.EditEntity<TEntity>(SetExpression, whereExpression);
            
            context.transaction.ExcuteNoQuery(model.Sql, GetParameters(model.Parameters));
        }
        #endregion
        
        #region Remove
        public void Remove(TEntity entity)
        {
            
            SqlAndParametersModel model = this.RemoveEntity(entity);
            
            context.transaction.ExcuteNoQuery(model.Sql, GetParameters(model.Parameters));
        }
        public void Remove(string Key)
        {
            
            SqlAndParametersModel model = this.RemoveEntity(Key);
            
            context.transaction.ExcuteNoQuery(model.Sql, GetParameters(model.Parameters));
        }
        /// <summary>
        /// 如果不出现提示，请加命名空间 
        /// </summary>
        /// <param name="whereExpression"></param>
        public void Remove(Expression<Func<TEntity, bool>> whereExpression)
        {
            
            SqlAndParametersModel model = this.RemoveEntity<TEntity>(whereExpression);
            
            context.transaction.ExcuteNoQuery(model.Sql, GetParameters(model.Parameters));
        }
        #endregion
        
        #region Find
        public TEntity Find(string Key)
        {
           
            SqlAndParametersModel model = this.FindEntity(Key);
            
            var list = context.transaction.ExceuteDataTable(model.Sql, GetParameters(model.Parameters)).ToList<TEntity>(table);
            if (list.Any())
            {
                return (TEntity)list[0];
            }
            return null;
        }
        #endregion
        #region 私有函数


        private DbParameter[] GetParameters(Dictionary<string, object> modelParameters)
        {

            DbParameter[] pars = new DbParameter[modelParameters.Count];
            int i = 0;
            foreach (var par in modelParameters)
            {
                pars[i] = context.transaction.ProviderFactory.CreateParameter();
                pars[i].ParameterName = par.Key;
                if (par.Value != null && par.Value.GetType().ToString() == "System.Boolean")
                {
                    pars[i].Value = (bool)par.Value ? "1" : "0";
                }
                else
                {
                    pars[i].Value = par.Value;
                }

                i++;
            }
            return pars;
        }
        /// <summary>
        /// 用于反射赋值
        /// </summary>
        /// <param name="_context"></param>
        internal void SetDbContext(DbContext _context)
        {
            context = _context;
        }
        /// <summary>
        /// 用于反射赋值
        /// </summary>
        /// <param name="_table"></param>
        internal void SetTableCache(EntityModel _table)
        {
            table = _table;
        }
        #endregion
        #region 无用,但不可删除


       

        DbContext ISZORM.Context
        {
            get { return this.context; }
        }

        Expression ISZORM.ExpressionWhere
        {
            get
            {
                return null;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        List<string> ISZORM.ExpressionSelect
        {
            get
            {
                return null;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        Dictionary<string, string> ISZORM.ExpressionOrderBy
        {
            get
            {
                return null;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        string ISZORM.ExpressionGroup
        {
            get
            {
                return null;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        SqlAndParametersModel ISZORM.wheresql
        {
            get
            {
                return null;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        int ISZORM.BeginRowNum
        {
            get
            {
                return 0;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        int ISZORM.EndRowNum
        {
            get
            {
                return 0;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        bool ISZORM.IsGenSql
        {
            get
            {
                return false;
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        #endregion
    }
    #region 查询缓存类
    internal class CacheQuery<TEntity> : ISZORM<TEntity>, IDisposable
    {
        public CacheQuery(DbContext _Context)
        {
            ExpressionWhere = null;
            ExpressionSelect = new List<string>();
            ExpressionOrderBy = new Dictionary<string, string>();
            Context = _Context;
            wheresql = new SqlAndParametersModel();
            IsGenSql = false;
            EndRowNum = -1;
        }
        public Expression ExpressionWhere
        {
            get;
            set;
        }

        public List<string> ExpressionSelect
        {
            get;
            set;
        }

        public void Dispose()
        {
            ExpressionSelect.Clear();
            ExpressionOrderBy.Clear();
            ExpressionWhere = null;
        }

        public DbContext Context
        {
            get;
            set;
        }


        public Dictionary<string, string> ExpressionOrderBy
        {
            get;
            set;
        }

        public int BeginRowNum
        {
            get;
            set;
        }

        public int EndRowNum
        {
            get;
            set;
        }


        public SqlAndParametersModel wheresql
        {
            get;
            set;
        }


        public bool IsGenSql
        {
            get;
            set;
        }


        public string ExpressionGroup
        {
            get;
            set;
        }
    }
    #endregion
    
}
