using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using SZORM.Factory;

namespace SZORM.Infrastructure
{
    public interface IDbContextServiceProvider
    {
        IDbConnection CreateConnection();
        IDbExpressionTranslator CreateDbExpressionTranslator();
        IStructure CreateStructureCheck();
    }
}
