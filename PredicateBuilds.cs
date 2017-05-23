using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using SZORM.Descriptors;
using SZORM.Exceptions;
using SZORM.Utility;

namespace SZORM
{
   internal class PredicateBuilds
    {
        public static Expression<Func<TEntity, bool>> BuildPredicate<TEntity>(object key)
        {
            Checks.NotNull(key, "key");
            Type entityType = typeof(TEntity);
            TypeDescriptor typeDescriptor = TypeDescriptor.GetDescriptor(entityType);
            if (typeDescriptor.PrimaryKey == null) throw new SZORMException("表没有定义主键.");
            ParameterExpression parameter = Expression.Parameter(entityType, "a");
            Expression propOrField = Expression.PropertyOrField(parameter, typeDescriptor.PrimaryKey.MemberInfo.Name);
            Expression keyValue = ExpressionExtension.MakeWrapperAccess(key, typeDescriptor.PrimaryKey.MemberInfoType);
            Expression lambdaBody = Expression.Equal(propOrField, keyValue);

            Expression<Func<TEntity, bool>> predicate = Expression.Lambda<Func<TEntity, bool>>(lambdaBody, parameter);

            return predicate;
        }
    }
}
