using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace OrientDB.Net.Core.BusinessObjects
{
    public interface ISession : IDisposable
    {
        IReadOnlyList<TBO> Get<TBO>(Expression<Func<TBO, bool>> query) where TBO : IBusinessObject;
        IReadOnlyList<TBO> Get<TBO>() where TBO : IBusinessObject;

        ITransaction BeginTransaction();
    }
}