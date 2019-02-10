using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Orient.Client;
using Orient.Client.API.Query;

namespace OrientDB.Net.Core.BusinessObjects.Query
{
    internal class GetQuery
    {
        private readonly ODatabase database;

        public GetQuery(ODatabase database)
        {
            this.database = database;
        }

        public IReadOnlyList<TBO> Execute<TBO>(string query) where TBO : IBusinessObject
        {
            var items = database.Query(query);

            var result = ConvertItems<TBO>(items, BoActivator.GetImplementationType(typeof(TBO)));
            return new ReadOnlyCollection<TBO>(result);
        }

        public TBO ExecuteById<TBO>(string id) where TBO : IBusinessObject
        {
            return (TBO) ExecuteById(typeof(TBO), id);
        }


        private BusinessObject ConvertItem(Type boType, ODocument item)
        {
            var realBo = BoActivator.GetInstance(boType);
            realBo.Id = item.ORID.ToString();
            realBo.Version = item.OVersion;

            var propertyInfosToSet = (from prop in boType.GetProperties()
                                      let docPropAttr = prop.GetCustomAttribute<DocumentPropertyAttribute>()
                                      where docPropAttr != null
                                      select new
                                             {
                                                 Prop = prop,
                                                 Info = docPropAttr
                                             }).ToList();
            foreach (var propertyInfo in propertyInfosToSet)
                if (item.TryGetValue(propertyInfo.Info.Key, out var value))
                    SetValueWithType(propertyInfo.Prop, realBo, value.ToString());

            var children = (from prop in boType.GetProperties()
                            let childAttr = prop.GetCustomAttribute<ChildAttribute>()
                            where childAttr != null
                            select new {Prop = prop, Child = childAttr}).ToList();
            foreach (var propertyInfo in children)
            {
                var outORIDKey = item.Keys.SingleOrDefault(k => k.ToLower() == $"out_{propertyInfo.Child.EdgeClassName.ToLower()}");
                if (outORIDKey == null)
                    continue;

                var outORID = item.GetField<List<ORID>>(outORIDKey).Single().ToString();

                var childEdge = database.Query(new PreparedQuery($"SELECT * FROM {propertyInfo.Child.EdgeClassName} WHERE @rid=:id")
                                                  .Set("id", outORID))
                                        .Run()
                                        .Single().To<OEdge>();

                var child = ExecuteById(propertyInfo.Prop.PropertyType, childEdge.InV.ToString());
                propertyInfo.Prop.SetValue(realBo, child);
            }

            var referenceLists = (from prop in boType.GetProperties()
                                  let attr = prop.GetCustomAttribute<ReferenceListAttribute>()
                                  where attr != null
                                  select new {Prop = prop, Attr = attr}).ToList();
            foreach (var referenceList in referenceLists)
            {
                var outORIDKey = item.Keys.SingleOrDefault(k => k.ToLower() == $"out_{referenceList.Attr.EdgeClassName.ToLower()}");
                if (outORIDKey == null)
                    continue;

                var outORIDKeys = item.GetField<List<ORID>>(outORIDKey);
                foreach (var outORID in outORIDKeys)
                {
                    var referenceBos = database.Query(new PreparedQuery($"SELECT * FROM {referenceList.Attr.EdgeClassName} WHERE @rid=:id")
                                                         .Set("id", outORID))
                                               .Run()
                                               .Select(referenceEdge => referenceEdge.To<OEdge>())
                                               .Select(referenceBo => ExecuteById(referenceList.Prop.PropertyType, referenceBo.InV.ToString()))
                                               .ToList();
                    var list = (IList) referenceList.Prop.GetValue(realBo);
                    foreach (var referenceBo in referenceBos) list.Add(referenceBo);
                }
            }

            return realBo;
        }

        private IList<TBO> ConvertItems<TBO>(List<ODocument> items, Type boType) where TBO : IBusinessObject
        {
            var result = new List<TBO>();
            foreach (var item in items)
            {
                var realBo = ConvertItem(boType, item);

                result.Add((TBO) (object) realBo);
            }

            return result;
        }

        private object ExecuteById(Type type, string id)
        {
            var listGenericArguments = type.GetTypeInfo().GenericTypeArguments;

            var bo = BoActivator.GetInstance(listGenericArguments.Length == 1 ? listGenericArguments.First() : type);
            var boType = bo.GetType();

            var item = database.Query(new PreparedQuery($"SELECT * FROM {bo.ClassName} WHERE @rid=:id")
                                         .Set("id", id))
                               .Run()
                               .FirstOrDefault();


            var result = ConvertItem(boType, item);
            return result;
        }

        private static void SetValueWithType(PropertyInfo propertyInfo, BusinessObject realBo, string value)
        {
            if (propertyInfo.PropertyType == typeof(string))
                propertyInfo.SetValue(realBo, value);
            else if (propertyInfo.PropertyType == typeof(DateTime))
                propertyInfo.SetValue(realBo, DateTime.Parse(value));
            else if (propertyInfo.PropertyType == typeof(Guid))
                propertyInfo.SetValue(realBo, Guid.Parse(value));
            else if (propertyInfo.PropertyType == typeof(int))
                propertyInfo.SetValue(realBo, int.Parse(value));
            else if (propertyInfo.PropertyType == typeof(double))
                propertyInfo.SetValue(realBo, double.Parse(value));
            else if (propertyInfo.PropertyType == typeof(bool))
                propertyInfo.SetValue(realBo, bool.Parse(value));
            else
                throw new Exception($"{propertyInfo.PropertyType} not allowed");
        }
    }
}