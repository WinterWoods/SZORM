using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace SZORM
{
    public interface IGroupingQuery<T>
    {
        IGroupingQuery<T> AndBy<K>(Expression<Func<T, K>> keySelector);
        IGroupingQuery<T> Having(Expression<Func<T, bool>> predicate);
        IQuery<TResult> Select<TResult>(Expression<Func<T, TResult>> selector);
    }
}
