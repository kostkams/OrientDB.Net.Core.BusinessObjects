using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Orient.Client;
using Orient.Client.API.Query;
using OrientDB.Net.Core.BusinessObjects.Query;

namespace OrientDB.Net.Core.BusinessObjects
{
    public class Session : ISession
    {
        private ODatabase database;
        private GetQuery getQuery;
        private ITransaction transaction;

        public Session(ConnectionInfo connectionInfo)
        {
            database = new ODatabase(connectionInfo.HostName,
                                     connectionInfo.Port,
                                     connectionInfo.DatabaseName,
                                     Convert(connectionInfo.DatabaseType),
                                     connectionInfo.UserName,
                                     connectionInfo.Password);
        }

        public void Dispose()
        {
            database?.Dispose();
            database = null;
        }
        

        public IOrderedQueryable<TBO> Get<TBO>() where TBO : IBusinessObject
        {
            CheckConnection();
            return new OrientQueryable<TBO>(new OrientQueryContext<TBO>(database));
        }

        public TBO GetById<TBO>(string id) where TBO : IBusinessObject
        {
            CheckConnection();
            return getQuery.ExecuteById<TBO>(id);
        }
      
        public ITransaction BeginTransaction()
        {
            if (transaction == null)
            {
                transaction = new Transaction(database);
                transaction.Commited += TransactionOnCommited;
            }
            else
            {
                throw new Exception("An other transaction is open");
            }

            return transaction;
        }

        private void CheckConnection()
        {
            if (database == null)
                throw new Exception($"{nameof(database)} is disposed");
            if (getQuery == null)
                getQuery = new GetQuery(database);
            getQuery = new GetQuery(database);
        }

        private ODatabaseType Convert(EDatabaseType databaseType)
        {
            switch (databaseType)
            {
                case EDatabaseType.Document:
                    return ODatabaseType.Document;
                case EDatabaseType.Graph:
                    return ODatabaseType.Graph;
                default:
                    throw new ArgumentOutOfRangeException(nameof(databaseType), databaseType, null);
            }
        }

        private void TransactionOnCommited(object sender, bool e)
        {
            transaction.Commited -= TransactionOnCommited;
            transaction = null;
        }
    }
}