using System;
using System.Collections.Generic;
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

        public IReadOnlyList<TBO> Get<TBO>(Expression<Func<TBO, bool>> query) where TBO : IBusinessObject
        {
            CheckConnection();
            return getQuery.Execute(query);
        }

        public IReadOnlyList<TBO> Get<TBO>() where TBO : IBusinessObject
        {
            CheckConnection();
            return getQuery.Execute<TBO>();
        }

        public TBO GetById<TBO>(string id) where TBO : IBusinessObject
        {
            CheckConnection();
            return getQuery.ExecuteById<TBO>(id);
        }

        public IList<IGenericBusinessObject> Query(IQuery query)
        {
            CheckConnection();

            var preparedQuery = new PreparedQuery(query.QueryString);
            foreach (var queryParameter in query.Parameters)
                preparedQuery.Set(queryParameter.Key, queryParameter.Value);

            var items = database.Query(preparedQuery).Run();
            var boList = new List<IGenericBusinessObject>();
            foreach (var item in items)
            {
                var genericBusinessObject = new GenericBusinessObject
                                            {
                                                Document = item
                                            };
                boList.Add(genericBusinessObject);
            }

            return boList;
        }

        public IList<IGenericBusinessObject> Command(IQuery query)
        {
            CheckConnection();

            var preparedCommand = new PreparedCommand(query.QueryString);
            foreach (var queryParameter in query.Parameters)
                preparedCommand.Set(queryParameter.Key, queryParameter.Value);

            var items = database.Command(preparedCommand).Run().ToList();

            var boList = new List<IGenericBusinessObject>();
            foreach (var item in items)
            {
                var genericBusinessObject = new GenericBusinessObject
                                            {
                                                Document = item
                                            };
                boList.Add(genericBusinessObject);
            }

            return boList;
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