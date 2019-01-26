using System;
using System.Collections.Generic;
using System.Linq;

namespace OrientDB.Net.Core.BusinessObjects
{
    public interface ISession : IDisposable
    {
        IOrderedQueryable<TBO> Get<TBO>() where TBO : IBusinessObject;
        TBO GetById<TBO>(string id) where TBO : IBusinessObject;

        ITransaction BeginTransaction();
    }
}