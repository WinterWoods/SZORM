using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SZORM
{
    public interface IDbSet<TEntity> 
       where TEntity : class
    {
        /// <summary>
        /// 查询必须使用该方法.
        /// </summary>
        /// <returns></returns>
        IQuery<TEntity> AsQuery();
        TEntity Find(object keyValue);
        Task<TEntity> FindAsync(object keyValue);
        
        TEntity Add(TEntity entity);
        Task<TEntity> AddAsync(TEntity entity);
        string Add(Expression<Func<TEntity>> body);
        Task<string> AddAsync(Expression<Func<TEntity>> body);
        TEntity Edit(TEntity entity);
        Task<TEntity> EditAsync(TEntity entity);
        int Edit(Expression<Func<TEntity, bool>> condition, Expression<Func<TEntity, TEntity>> body);
        Task<int> EditAsync(Expression<Func<TEntity, bool>> condition, Expression<Func<TEntity, TEntity>> body);
        int Edit(Expression<Func<TEntity, TEntity>> body, Expression<Func<TEntity, bool>> condition);
        Task<int> EditAsync(Expression<Func<TEntity, TEntity>> body, Expression<Func<TEntity, bool>> condition);
        int Remove(TEntity entity);
        Task<int> RemoveAsync(TEntity entity);
        int Remove(Expression<Func<TEntity, bool>> condition);
        Task<int> RemoveAsync(Expression<Func<TEntity, bool>> condition);
        int Remove(object keyValues);
        Task<int> RemoveAsync(object keyValues);
    }
}
