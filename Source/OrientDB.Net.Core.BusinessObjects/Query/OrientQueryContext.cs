using System.Collections.Generic;
using System.Linq.Expressions;
using Orient.Client;

namespace OrientDB.Net.Core.BusinessObjects.Query
{
    public class OrientQueryContext<TBO>: IQueryContext<TBO> where TBO : IBusinessObject
    {
        private readonly ODatabase database;

        public OrientQueryContext(ODatabase database)
        {
            this.database = database;
        }

        public IReadOnlyList<TBO> Execute(Expression expression, bool isEnumerable)
        {
            var orientExpressionVisitor = new OrientExpressionVisitor();
            var query = orientExpressionVisitor.Translate<TBO>(expression);

            var getQuery = new GetQuery(database);
            return getQuery.Execute<TBO>(query);
        }
    }
}