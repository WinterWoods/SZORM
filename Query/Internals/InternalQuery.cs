using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SZORM.Core;
using SZORM.Infrastructure;
using SZORM.Mapper;
using SZORM.Query.Mapping;
using SZORM.Query.QueryExpressions;
using SZORM.Query.QueryState;
using SZORM.Query.Visitors;

namespace SZORM.Query.Internals
{
    class InternalQuery<T> : IEnumerable<T>, IEnumerable
    {
        Query<T> _query;

        internal InternalQuery(Query<T> query)
        {
            this._query = query;
        }

        DbCommandFactor GenerateCommandFactor()
        {
            IQueryState qs = QueryExpressionVisitor.VisitQueryExpression(this._query.QueryExpression);
            MappingData data = qs.GenerateMappingData();

            IObjectActivator objectActivator;
            objectActivator = data.ObjectActivatorCreator.CreateObjectActivator();

            IDbExpressionTranslator translator = this._query.DbContext._dbContextServiceProvider.CreateDbExpressionTranslator();
            List<DbParam> parameters;
            string cmdText = translator.Translate(data.SqlQuery, out parameters);

            DbCommandFactor commandFactor = new DbCommandFactor(objectActivator, cmdText, parameters.ToArray());
            return commandFactor;
        }

        public IEnumerator<T> GetEnumerator()
        {
            DbCommandFactor commandFactor = this.GenerateCommandFactor();
            var enumerator = QueryEnumeratorCreator.CreateEnumerator<T>(this._query.DbContext.InternalAdoSession, commandFactor);
            return enumerator;
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public override string ToString()
        {
            DbCommandFactor commandFactor = this.GenerateCommandFactor();
            return InternalAdoSession.AppendDbCommandInfo(commandFactor.CommandText, commandFactor.Parameters);
        }
    }
}
