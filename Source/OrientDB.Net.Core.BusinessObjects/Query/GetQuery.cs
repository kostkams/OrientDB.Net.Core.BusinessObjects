using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
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


        public IReadOnlyList<TBO> Execute<TBO>() where TBO : IBusinessObject
        {
            return Execute<TBO>(null);
        }

        public IReadOnlyList<TBO> Execute<TBO>(Expression<Func<TBO, bool>> query) where TBO : IBusinessObject
        {
            var bo = (BusinessObject) GetInstanceOf(typeof(TBO));
            var boType = bo.GetType();

            List<ODocument> items;
            if (query == null)
            {
                items = database.Query($"SELECT * FROM {bo.ClassName}");
            }
            else
            {
                var preparedQuery = CreatePreparedQuery(query, boType, bo);

                items = database.Query(preparedQuery).Run();
            }

            var result = ConvertItems<TBO>(items, boType);
            return new ReadOnlyCollection<TBO>(result);
        }

        public TBO ExecuteById<TBO>(string id) where TBO : IBusinessObject
        {
            return (TBO) ExecuteById(typeof(TBO), id);
        }

        private BusinessObject ConvertItem(Type boType, ODocument item)
        {
            var realBo = GetInstanceOf(boType) as BusinessObject;
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
                    propertyInfo.Prop.SetValue(realBo, value);

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
                var outORID = item.GetField<List<ORID>>(outORIDKey).Single().ToString();
                var referenceBos = database.Query(new PreparedQuery($"SELECT * FROM {referenceList.Attr.EdgeClassName} WHERE @rid=:id")
                                                     .Set("id", outORID))
                                           .Run()
                                           .Select(referenceEdge => referenceEdge.To<OEdge>())
                                           .Select(referenceBo => ExecuteById(referenceList.Prop.PropertyType, referenceBo.InV.ToString()))
                                           .ToList();
                var list = (IList) referenceList.Prop.GetValue(realBo);
                foreach (var referenceBo in referenceBos) list.Add(referenceBo);
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

        private PreparedQuery CreatePreparedQuery<TBO>(Expression<Func<TBO, bool>> query, Type boType, BusinessObject bo) where TBO : IBusinessObject
        {
            var queryBody = (BinaryExpression) query.Body;
            var parsedExpression = GetBinaryExpression(queryBody, boType, new ParsedExpression());

            var sb = new StringBuilder($"SELECT * FROM {bo.ClassName} WHERE {parsedExpression}");
            var flatten = Flatten(parsedExpression).Where(item => item != null).ToList();

            var preparedQuery = new PreparedQuery(sb.ToString());
            foreach (var parsingForQuery in flatten)
                preparedQuery.Set(parsingForQuery.Key, parsingForQuery.Value);

            return preparedQuery;
        }

        private object ExecuteById(Type type, string id)
        {
            var listGenericArguments = type.GetTypeInfo().GenericTypeArguments;

            var bo = (BusinessObject) GetInstanceOf(listGenericArguments.Length == 1 ? listGenericArguments.First() : type);
            var boType = bo.GetType();

            var item = database.Query(new PreparedQuery($"SELECT * FROM {bo.ClassName} WHERE @rid=:id")
                                         .Set("id", id))
                               .Run()
                               .FirstOrDefault();


            var result = ConvertItem(boType, item);
            return result;
        }

        private IList<ParsingForQuery> Flatten(ParsedExpression parsedExpression)
        {
            var result = new List<ParsingForQuery>();
            if (parsedExpression != null)
            {
                result.AddRange(Flatten(parsedExpression.Left));
                result.Add(parsedExpression.Value);
                result.AddRange(Flatten(parsedExpression.Right));
            }

            return result;
        }

        private ParsedExpression GetBinaryExpression(BinaryExpression expression, Type boType, ParsedExpression parent)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                    var variable = expression.Left.NodeType == ExpressionType.MemberAccess
                                       ? ((MemberExpression) expression.Left).Member.Name
                                       : ((MemberExpression) expression.Right).Member.Name;
                    var value = expression.Right.NodeType == ExpressionType.Constant
                                    ? ((ConstantExpression) expression.Right).Value
                                    : ((ConstantExpression) expression.Left).Value;
                    parent.Value = new ParsingForQuery(value.ToString(),
                                                       expression.NodeType == ExpressionType.Equal ? "=" : "!=",
                                                       variable);
                    break;
                case ExpressionType.AndAlso:
                    parent.Left = GetBinaryExpression((BinaryExpression) expression.Left, boType, new ParsedExpression());
                    parent.Right = GetBinaryExpression((BinaryExpression) expression.Right, boType, new ParsedExpression());
                    parent.Combination = "AND";
                    break;
                case ExpressionType.OrElse:
                    parent.Left = GetBinaryExpression((BinaryExpression) expression.Left, boType, new ParsedExpression());
                    parent.Right = GetBinaryExpression((BinaryExpression) expression.Right, boType, new ParsedExpression());
                    parent.Combination = "OR";
                    break;
            }

            return parent;
        }

        private object GetInstanceOf(Type requestedType)
        {
            var type = requestedType.Assembly
                                    .GetExportedTypes()
                                    .Single(t => !t.IsInterface && !t.IsAbstract && requestedType.IsAssignableFrom(t));
            if (type != null)
            {
                var instance = Activator.CreateInstance(type);
                return instance;
            }

            return null;
        }

        private class ParsedExpression
        {
            public ParsedExpression Left { get; set; }
            public ParsedExpression Right { get; set; }

            public ParsingForQuery Value { get; set; }
            public string Combination { get; set; }

            public override string ToString()
            {
                return $"({Left} {Value} {Combination} {Right})";
            }
        }

        private class ParsingForQuery
        {
            private static readonly Random random = new Random();
            private readonly string key;

            public ParsingForQuery(string value, string @operator, string variable)
            {
                Value = value;
                Operator = @operator;
                Variable = variable;
                key = RandomString(5);
            }


            public string Value { get; }
            public string Operator { get; }
            public string Variable { get; }

            public string Key => $"{Variable.ToLower()}{key}";

            public override string ToString()
            {
                return $"{Variable} {Operator} :{Key}";
            }

            private string RandomString(int length)
            {
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                return new string(Enumerable.Repeat(chars, length)
                                            .Select(s => s[random.Next(s.Length)]).ToArray());
            }
        }
    }
}