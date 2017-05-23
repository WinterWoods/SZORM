using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SZORM.DbExpressions
{
    public abstract class DbMainTableExpression : DbExpression
    {
        DbTableSegment _table;
        List<DbJoinTableExpression> _joinTables;
        protected DbMainTableExpression(DbExpressionType nodeType, DbTableSegment table)
            : base(nodeType)
        {
            this._table = table;
            this._joinTables = new List<DbJoinTableExpression>();
        }
        public DbTableSegment Table { get { return this._table; } }

        public List<DbJoinTableExpression> JoinTables { get { return this._joinTables; } }
    }
}
