﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SZORM.Query.QueryExpressions;

namespace SZORM.Query.QueryState
{
    internal abstract class SubQueryState : QueryStateBase
    {
        protected SubQueryState(ResultElement resultElement)
            : base(resultElement)
        {
        }

        public override IQueryState Accept(WhereExpression exp)
        {
            IQueryState state = this.AsSubQueryState();
            return state.Accept(exp);
        }
        public override IQueryState Accept(OrderExpression exp)
        {
            IQueryState state = this.AsSubQueryState();
            return state.Accept(exp);
        }
        public override IQueryState Accept(SelectExpression exp)
        {
            IQueryState queryState = this.AsSubQueryState();
            return queryState.Accept(exp);
        }
        public override IQueryState Accept(SkipExpression exp)
        {
            GeneralQueryState subQueryState = this.AsSubQueryState();

            SkipQueryState state = new SkipQueryState(subQueryState.Result, exp.Count);
            return state;
        }
        public override IQueryState Accept(TakeExpression exp)
        {
            GeneralQueryState subQueryState = this.AsSubQueryState();

            TakeQueryState state = new TakeQueryState(subQueryState.Result, exp.Count);
            return state;
        }
        public override IQueryState Accept(AggregateQueryExpression exp)
        {
            IQueryState subQueryState = this.AsSubQueryState();

            IQueryState state = subQueryState.Accept(exp);
            return state;
        }
    }
}
