using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using SZORM.DbExpressions;
using SZORM.Descriptors;
using SZORM.Exceptions;

namespace SZORM.Core.Visitors
{
    public class DefaultExpressionVisitor : ExpressionVisitorBase
    {
        TypeDescriptor _typeDescriptor;

        public DefaultExpressionVisitor(TypeDescriptor typeDescriptor)
        {
            this._typeDescriptor = typeDescriptor;
        }

        public DbExpression VisitFilterPredicate(LambdaExpression lambda)
        {
            lambda = ExpressionVisitorBase.ReBuildFilterPredicate(lambda);
            return this.Visit(lambda);
        }

        protected override DbExpression VisitMemberAccess(MemberExpression exp)
        {
            if (ExpressionExtension.IsDerivedFromParameter(exp))
            {
                Stack<MemberExpression> reversedExps = ExpressionExtension.Reverse(exp);

                DbExpression dbExp = null;
                bool first = true;
                foreach (var me in reversedExps)
                {
                    if (first)
                    {
                        DbColumnAccessExpression dbColumnAccessExpression = this._typeDescriptor.TryGetColumnAccessExpression(me.Member);
                        if (dbColumnAccessExpression == null)
                        {
                            throw new SZORMException(string.Format("The member '{0}' does not map any column.", me.Member.Name));
                        }

                        dbExp = dbColumnAccessExpression;
                        first = false;
                    }
                    else
                    {
                        DbMemberExpression dbMe = new DbMemberExpression(me.Member, dbExp);
                        dbExp = dbMe;
                    }
                }

                if (dbExp != null)
                {
                    return dbExp;
                }
                else
                    throw new Exception();
            }
            else
            {
                return base.VisitMemberAccess(exp);
            }
        }

        protected override DbExpression VisitParameter(ParameterExpression exp)
        {
            throw new NotSupportedException(exp.ToString());
        }
    }
}
