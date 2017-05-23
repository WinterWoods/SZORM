using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SZORM.DbExpressions;

namespace SZORM.Query
{
    class FromQueryResult
    {
        public DbFromTableExpression FromTable { get; set; }
        public IMappingObjectExpression MappingObjectExpression { get; set; }
    }
}
