using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using SZORM.Core.Visitors;
using SZORM.DbExpressions;

namespace SZORM.Query.Visitors
{
    class FilterPredicateExpressionVisitor : ExpressionVisitor<DbExpression>
    {
        public static DbExpression ParseFilterPredicate(LambdaExpression lambda, List<IMappingObjectExpression> moeList)
        {
            return GeneralExpressionVisitor.ParseLambda(ExpressionVisitorBase.ReBuildFilterPredicate(lambda), moeList);
        }
    }
}
