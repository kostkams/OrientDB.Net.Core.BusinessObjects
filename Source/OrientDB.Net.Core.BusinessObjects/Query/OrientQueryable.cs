using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace OrientDB.Net.Core.BusinessObjects.Query
{
    public class OrientQueryable<TBO> : IOrderedQueryable<TBO>
    {
        public OrientQueryable(IQueryContext<TBO> queryContext)
        {
            Initialize(new OrientQueryProvider<TBO>(queryContext), null);
        }

        public OrientQueryable(IQueryProvider provider)
        {
            Initialize(provider, null);
        }

        internal OrientQueryable(IQueryProvider provider, Expression expression)
        {
            Initialize(provider, expression);
        }

        public IEnumerator<TBO> GetEnumerator()
        {
            return Provider.Execute<IEnumerable<TBO>>(Expression).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Provider.Execute<IEnumerable>(Expression).GetEnumerator();
        }

        public Type ElementType => typeof(TBO);

        public Expression Expression { get; private set; }
        public IQueryProvider Provider { get; private set; }

        private void Initialize(IQueryProvider provider, Expression expression)
        {
            if (expression != null && !typeof(IQueryable<TBO>).IsAssignableFrom(expression.Type))
                throw new ArgumentException($"Not assignable from {expression.Type}", nameof(expression));

            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Expression = expression ?? Expression.Constant(this);
        }
    }
}