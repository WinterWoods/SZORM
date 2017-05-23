using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SZORM.DbExpressions;

namespace SZORM.Query.Mapping
{
    public class MappingData
    {
        public MappingData()
        {
        }
        public IObjectActivatorCreator ObjectActivatorCreator { get; set; }
        public DbSqlQueryExpression SqlQuery { get; set; }
    }
}
