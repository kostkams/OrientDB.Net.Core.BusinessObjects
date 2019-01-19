using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Orient.Client;
using Orient.Client.API.Query;

namespace OrientDB.Net.Core.BusinessObjects
{
    internal enum ETransaction
    {
        Create,
        Update,
        Delete
    }

    public class Transaction : ITransaction
    {
        private readonly ODatabase database;

        private ConcurrentBag<Tuple<ETransaction, IBusinessObject>> transactions;

        public Transaction(ODatabase database)
        {
            this.database = database;
            transactions = new ConcurrentBag<Tuple<ETransaction, IBusinessObject>>();
        }

        public event EventHandler<bool> Commited;

        public void Commit()
        {
            var postProcessItems = new ConcurrentBag<Tuple<OVertex, BusinessObject>>();

            while (transactions.TryTake(out var transaction))
            {
                var businessObject = transaction.Item2;
                OVertex vertex;
                switch (transaction.Item1)
                {
                    case ETransaction.Create:
                        vertex = new OVertex {OClassName = businessObject.ClassName};
                        FillVertex(businessObject, vertex);
                        postProcessItems.Add(new Tuple<OVertex, BusinessObject>(database.Insert(vertex).Run().To<OVertex>(), (BusinessObject) businessObject));
                        break;
                    case ETransaction.Update:
                        vertex = new OVertex { OClassName = businessObject.ClassName };
                        FillVertex(businessObject, vertex);
                        database.Update(vertex).Run();
                        postProcessItems.Add(new Tuple<OVertex, BusinessObject>(database.Query(new PreparedQuery("SELECT * FROM V WHERE @rid=:id")
                                                                                                  .Set("id", businessObject.Id))
                                                                                        .Run()
                                                                                        .Single()
                                                                                        .To<OVertex>(), 
                                                                                (BusinessObject) businessObject));
                        break;
                    case ETransaction.Delete:
                        vertex = new OVertex
                                 {
                                     OClassName = businessObject.ClassName,
                                     ORID = new ORID(businessObject.Id),
                                     OVersion = businessObject.Version
                                 };
                        postProcessItems.Add(new Tuple<OVertex, BusinessObject>(vertex, (BusinessObject)businessObject));
                        database.Delete.Vertex(vertex).Run();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            

            while (postProcessItems.TryTake(out var item))
            {
                item.Item2.Id = item.Item1.ORID.ToString();
                item.Item2.Version = item.Item1.OVersion;
            }

            transactions = null;

            Commited?.Invoke(this, true);
        }

        public void Reset()
        {
            while (transactions.TryTake(out _))
            {
            }

            transactions = null;
        }

        public void Create<TBO>(TBO businessObject) where TBO : IBusinessObject
        {
            transactions.Add(new Tuple<ETransaction, IBusinessObject>(ETransaction.Create, businessObject));
        }

        public void Delete(IBusinessObject businessObject)
        {
            transactions.Add(new Tuple<ETransaction, IBusinessObject>(ETransaction.Delete, businessObject));
        }

        public void Update(IBusinessObject businessObject)
        {
            transactions.Add(new Tuple<ETransaction, IBusinessObject>(ETransaction.Update, businessObject));
        }

        private static void FillVertex(IBusinessObject businessObject, OVertex vertex)
        {
            var props = (from prop in businessObject.GetType().GetProperties()
                         let attr = (DocumentPropertyAttribute) prop.GetCustomAttribute(typeof(DocumentPropertyAttribute))
                         where attr != null
                         select new {Prop = prop, Attr = attr}).ToList();
            foreach (var propertyInfo in props)
            {
                var value = propertyInfo.Prop.GetValue(businessObject);
                if (propertyInfo.Attr.Required && (value == null || value is string && string.IsNullOrEmpty(value.ToString())))
                    throw new Exception($"The property '{nameof(propertyInfo.Attr.Key)}' is required");

                if (vertex.ContainsKey(propertyInfo.Attr.Key))
                    vertex[propertyInfo.Attr.Key] = value;
                else
                    vertex.Add(propertyInfo.Attr.Key, value);
            }
        }
    }
}