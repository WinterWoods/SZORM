using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SZORM.DbExpressions;

namespace SZORM.Query
{
    public class JoinQueryResult
    {
        public IMappingObjectExpression MappingObjectExpression { get; set; }
        public DbJoinTableExpression JoinTable { get; set; }
        //public DbExpression LeftKeySelector { get; set; }
        //public DbExpression RightKeySelector { get; set; }
    }
}
