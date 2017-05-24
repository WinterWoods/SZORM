﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using SZORM.DbExpressions;

namespace SZORM.Query
{
    class JoiningQueryInfo
    {
        public JoiningQueryInfo(QueryBase query, DbJoinType joinType, LambdaExpression condition)
        {
            this.Query = query;
            this.JoinType = joinType;
            this.Condition = condition;
        }
        public QueryBase Query { get; set; }
        public DbJoinType JoinType { get; set; }
        public LambdaExpression Condition { get; set; }
    }
}
