﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SZORM.DbExpressions;
using SZORM.Query.QueryExpressions;

namespace SZORM.Query.QueryState
{
    internal sealed class TakeQueryState : SubQueryState
    {
        int _count;
        public TakeQueryState(ResultElement resultElement, int count)
            : base(resultElement)
        {
            this.Count = count;
        }

        public int Count
        {
            get
            {
                return this._count;
            }
            set
            {
                this.CheckInputCount(value);
                this._count = value;
            }
        }

        void CheckInputCount(int count)
        {
            if (count < 0)
            {
                throw new ArgumentException("The take count could not less than 0.");
            }
        }

        public override IQueryState Accept(SelectExpression exp)
        {
            ResultElement result = this.CreateNewResult(exp.Selector);
            return this.CreateQueryState(result);
        }
        public override IQueryState Accept(TakeExpression exp)
        {
            if (exp.Count < this.Count)
                this.Count = exp.Count;

            return this;
        }

        public override IQueryState CreateQueryState(ResultElement result)
        {
            return new TakeQueryState(result, this.Count);
        }

        public override DbSqlQueryExpression CreateSqlQuery()
        {
            DbSqlQueryExpression sqlQuery = base.CreateSqlQuery();

            sqlQuery.TakeCount = this.Count;
            sqlQuery.SkipCount = null;

            return sqlQuery;
        }
    }
}
