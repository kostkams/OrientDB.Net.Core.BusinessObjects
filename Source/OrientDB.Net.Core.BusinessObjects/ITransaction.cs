using System;

namespace OrientDB.Net.Core.BusinessObjects
{
    public interface ITransaction
    {
        event EventHandler<bool> Commited; 
        void Commit();
        void Reset();

        void Create<TBO>(TBO businessObject) where TBO : IBusinessObject;

        void Delete(IBusinessObject businessObject);

        void Update(IBusinessObject businessObject);

        void CreateEdge(IBusinessObject from, IBusinessObject to, string edgeClassName);
    }
}