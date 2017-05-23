using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SZORM.Query.QueryState
{

    class AggregateQueryState : QueryStateBase, IQueryState
    {
        public AggregateQueryState(ResultElement resultElement)
            : base(resultElement)
        {
        }
    }
}
