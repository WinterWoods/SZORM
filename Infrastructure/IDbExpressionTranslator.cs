using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SZORM.DbExpressions;

namespace SZORM.Infrastructure
{
    public interface IDbExpressionTranslator
    {
        string Translate(DbExpression expression, out List<DbParam> parameters);
        DbExpressionVisitor<DbExpression> GetSqlGenerator();
    }
}
