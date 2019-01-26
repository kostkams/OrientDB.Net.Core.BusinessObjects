using System.Collections.Generic;
using System.Linq.Expressions;

namespace OrientDB.Net.Core.BusinessObjects.Query
{
    public interface IQueryContext<TBO>
    {
        IReadOnlyList<TBO> Execute(Expression expression, bool isEnumerable);
    }
}