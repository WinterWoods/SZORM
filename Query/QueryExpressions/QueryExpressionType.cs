using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SZORM.Query.QueryExpressions
{
    enum QueryExpressionType
    {
        Root = 1,
        Where,
        Take,
        Skip,
        OrderBy,
        OrderByDesc,
        ThenBy,
        ThenByDesc,
        Select,
        Include,
        Aggregate,
        JoinQuery,
        GroupingQuery,
    }
}
