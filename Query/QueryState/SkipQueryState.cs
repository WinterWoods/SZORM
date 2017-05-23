﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SZORM.DbExpressions;
using SZORM.Query.QueryExpressions;

namespace SZORM.Query.QueryState
{
    internal sealed class SkipQueryState : SubQueryState
    {
        int _count;
        public SkipQueryState(ResultElement resultElement, int count)
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
                throw new ArgumentException("The skip count could not less than 0.");
            }
        }

        public override IQueryState Accept(SelectExpression exp)
        {
            ResultElement result = this.CreateNewResult(exp.Selector);
            return this.CreateQueryState(result);
        }
        public override IQueryState Accept(SkipExpression exp)
        {
            if (exp.Count < 1)
            {
                return this;
            }

            this.Count += this.Count;

            return this;
        }
        public override IQueryState Accept(TakeExpression exp)
        {
            var state = new LimitQueryState(this.Result, this.Count, exp.Count);
            return state;
        }
        public override IQueryState CreateQueryState(ResultElement result)
        {
            return new SkipQueryState(result, this.Count);
        }

        public override DbSqlQueryExpression CreateSqlQuery()
        {
            DbSqlQueryExpression sqlQuery = base.CreateSqlQuery();

            sqlQuery.TakeCount = null;
            sqlQuery.SkipCount = this.Count;

            return sqlQuery;
        }
    }
}
