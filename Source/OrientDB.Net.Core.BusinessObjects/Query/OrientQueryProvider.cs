using System;
using System.Linq;
using System.Linq.Expressions;

namespace OrientDB.Net.Core.BusinessObjects.Query
{
    public class OrientQueryProvider<TBO> : IQueryProvider
    {
        private readonly IQueryContext<TBO> queryContext;

        public OrientQueryProvider(IQueryContext<TBO> queryContext)
        {
            this.queryContext = queryContext;
        }

        public virtual IQueryable CreateQuery(Expression expression)
        {
            throw new NotImplementedException();
        }

        public virtual IQueryable<T> CreateQuery<T>(Expression expression)
        {
            return new OrientQueryable<T>(this, expression);
        }

        object IQueryProvider.Execute(Expression expression)
        {
            throw new NotImplementedException();
        }

        T IQueryProvider.Execute<T>(Expression expression)
        {
            return (T) queryContext.Execute(expression, typeof(T).Name == "IEnumerable`1");
        }
    }
}