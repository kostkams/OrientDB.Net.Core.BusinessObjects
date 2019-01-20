using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private ConcurrentBag<TransactionEdge> transactionEdges;

        private ConcurrentBag<TransactionItem> transactionItems;

        public Transaction(ODatabase database)
        {
            this.database = database;
            transactionItems = new ConcurrentBag<TransactionItem>();
            transactionEdges = new ConcurrentBag<TransactionEdge>();
        }

        public event EventHandler<bool> Commited;

        public void Commit()
        {
            var postProcessItems = new ConcurrentBag<Tuple<OVertex, BusinessObject>>();

            while (transactionItems.TryTake(out var transaction))
            {
                var businessObject = transaction.BusinessObject;
                CreateVertexClassNameInSchema(businessObject.ClassName);

                switch (transaction.Transaction)
                {
                    case ETransaction.Create:
                        HandleCreateVertex(businessObject, postProcessItems);
                        break;
                    case ETransaction.Update:
                        HandleUpdateVertex(businessObject, postProcessItems);
                        break;
                    case ETransaction.Delete:
                        HandleDeleteVertex(businessObject, postProcessItems);
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

            while (transactionEdges.TryTake(out var transaction))
            {
                CreateEdgeClassNameInSchema(transaction.EdgeClassName);

                switch (transaction.Transaction)
                {
                    case ETransaction.Create:
                        HandleCreateEdge(transaction);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            transactionItems = null;
            transactionEdges = null;

            Commited?.Invoke(this, true);
        }

        public void Reset()
        {
            while (transactionItems.TryTake(out _))
            {
            }

            while (transactionEdges.TryTake(out _))
            {
            }

            transactionItems = null;
            transactionEdges = null;
        }

        public void Create<TBO>(TBO businessObject) where TBO : IBusinessObject
        {
            transactionItems.Add(new TransactionItem(ETransaction.Create, businessObject));
        }

        public void Delete(IBusinessObject businessObject)
        {
            transactionItems.Add(new TransactionItem(ETransaction.Delete, businessObject));

            var children = GetChildren(businessObject);

            foreach (var child in children)
                transactionItems.Add(new TransactionItem(ETransaction.Delete, child));
        }

        public void Update(IBusinessObject businessObject)
        {
            transactionItems.Add(new TransactionItem(ETransaction.Update, businessObject));

            var children = GetChildren(businessObject);

            foreach (var child in children)
                transactionItems.Add(new TransactionItem(ETransaction.Update, child));
        }

        public void CreateEdge(IBusinessObject from, IBusinessObject to, string edgeClassName)
        {
            transactionEdges.Add(new TransactionEdge(from, to, edgeClassName, ETransaction.Create));
        }

        private void CreateEdgeClassNameInSchema(string className)
        {
            if (database.Schema.Classes().All(c => c.ToLower() != className.ToLower()))
                database.Create.Class(className).Extends<OEdge>().Run();
        }

        private void CreateVertexClassNameInSchema(string className)
        {
            if (database.Schema.Classes().All(c => c.ToLower() != className.ToLower()))
                database.Create.Class(className).Extends<OVertex>().Run();
        }

        private void FillVertex(IBusinessObject businessObject, OVertex vertex)
        {
            var props = (from prop in businessObject.GetType().GetProperties()
                         let attr = prop.GetCustomAttribute<DocumentPropertyAttribute>()
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

            var children = (from prop in businessObject.GetType().GetProperties()
                            let attr = prop.GetCustomAttribute<ChildAttribute>()
                            where attr != null
                            select new {Prop = prop, Attr = attr}).ToList();
            foreach (var referenceList in children)
            {
                var child = (IBusinessObject) referenceList.Prop.GetValue(businessObject);
                if (string.IsNullOrEmpty(child.Id))
                {
                    transactionItems.Add(new TransactionItem(ETransaction.Create, child));
                    transactionEdges.Add(new TransactionEdge(businessObject, child, referenceList.Attr.EdgeClassName, ETransaction.Create));
                }
                else
                    transactionItems.Add(new TransactionItem(ETransaction.Update, child));
            }

            var referenceLists = (from prop in businessObject.GetType().GetProperties()
                                  let attr = prop.GetCustomAttribute<ReferenceListAttribute>()
                                  where attr != null
                                  select new {Prop = prop, Attr = attr}).ToList();
            foreach (var referenceList in referenceLists)
            {
                var list = (IList) referenceList.Prop.GetValue(businessObject);
                foreach (var referenceBo in list.OfType<IBusinessObject>())
                    if (string.IsNullOrEmpty(referenceBo.Id))
                        transactionEdges.Add(new TransactionEdge(businessObject, referenceBo, referenceList.Attr.EdgeClassName, ETransaction.Create));
                    else
                        transactionItems.Add(new TransactionItem(ETransaction.Update, referenceBo));
            }
        }

        private static List<IBusinessObject> GetChildren(IBusinessObject businessObject)
        {
            var children = businessObject.GetType()
                                         .GetProperties()
                                         .Where(p => p.GetCustomAttribute(typeof(ChildAttribute)) != null)
                                         .Select(p => p.GetValue(businessObject))
                                         .OfType<IBusinessObject>()
                                         .ToList();
            return children;
        }

        private OVertex GetItemFromDatabase(IBusinessObject businessObject)
        {
            return database.Query(new PreparedQuery("SELECT * FROM V WHERE @rid=:id")
                                     .Set("id", businessObject.Id))
                           .Run()
                           .Single()
                           .To<OVertex>();
        }


        private void HandleCreateEdge(TransactionEdge transaction)
        {
            var fromId = new ORID(transaction.From.Id);
            var toId = new ORID(transaction.To.Id);

            database.Command(new PreparedCommand($"CREATE EDGE {transaction.EdgeClassName} FROM :out TO :in")
                            .Set("out", fromId)
                            .Set("in", toId))
                    .Run();
        }

        private void HandleCreateVertex(IBusinessObject businessObject, ConcurrentBag<Tuple<OVertex, BusinessObject>> postProcessItems)
        {
            var vertex = new OVertex {OClassName = businessObject.ClassName};
            FillVertex(businessObject, vertex);
            vertex = database.Insert(vertex).Run().To<OVertex>();
            postProcessItems.Add(new Tuple<OVertex, BusinessObject>(vertex, (BusinessObject) businessObject));
        }

        private void HandleDeleteVertex(IBusinessObject businessObject, ConcurrentBag<Tuple<OVertex, BusinessObject>> postProcessItems)
        {
            var vertex = new OVertex
                         {
                             OClassName = businessObject.ClassName,
                             ORID = new ORID(businessObject.Id),
                             OVersion = businessObject.Version
                         };
            postProcessItems.Add(new Tuple<OVertex, BusinessObject>(vertex, (BusinessObject) businessObject));
            database.Delete.Vertex(vertex).Run();
        }

        private void HandleUpdateVertex(IBusinessObject businessObject, ConcurrentBag<Tuple<OVertex, BusinessObject>> postProcessItems)
        {
            var vertex = new OVertex {OClassName = businessObject.ClassName, ORID = new ORID(businessObject.Id)};
            FillVertex(businessObject, vertex);
            database.Update(vertex).Run();
            postProcessItems.Add(new Tuple<OVertex, BusinessObject>(GetItemFromDatabase(businessObject), (BusinessObject) businessObject));
        }

        private class TransactionItem
        {
            public TransactionItem(ETransaction transaction, IBusinessObject businessObject)
            {
                BusinessObject = businessObject;
                Transaction = transaction;
            }

            public IBusinessObject BusinessObject { get; }
            public ETransaction Transaction { get; }
        }

        private class TransactionEdge
        {
            public TransactionEdge(IBusinessObject from, IBusinessObject to, string edgeClassName, ETransaction transaction)
            {
                From = from;
                To = to;
                EdgeClassName = edgeClassName;
                Transaction = transaction;
            }

            public IBusinessObject From { get; }
            public IBusinessObject To { get; }
            public string EdgeClassName { get; }
            public ETransaction Transaction { get; }
        }
    }
}