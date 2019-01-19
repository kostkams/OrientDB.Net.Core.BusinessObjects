using System;
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

        public IReadOnlyList<TBO> Execute<TBO>(Expression<Func<TBO, bool>> query) where TBO : IBusinessObject
        {
            var bo = GetInstanceOf<TBO>() as BusinessObject;
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

        private IList<TBO> ConvertItems<TBO>(List<ODocument> items, Type boType) where TBO : IBusinessObject
        {
            var result = new List<TBO>();
            foreach (var item in items)
            {
                var realBo = GetInstanceOf<TBO>() as BusinessObject;
                realBo.Id = item.ORID.ToString();
                realBo.Version = item.OVersion;

                var propertyInfosToSet = (from prop in boType.GetProperties()
                                          where prop.GetCustomAttribute(typeof(DocumentPropertyAttribute)) != null
                                          select new
                                                 {
                                                     Prop = prop,
                                                     Info = (DocumentPropertyAttribute) prop.GetCustomAttribute(typeof(DocumentPropertyAttribute))
                                                 }).ToList();
                foreach (var propertyInfo in propertyInfosToSet)
                    if (item.TryGetValue(propertyInfo.Info.Key, out var value))
                        propertyInfo.Prop.SetValue(realBo, value);

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
            foreach (var parsingForQuery in flatten) preparedQuery.Set(parsingForQuery.Key, parsingForQuery.Value);

            return preparedQuery;
        }

        private T GetInstanceOf<T>()
        {
            var type = typeof(T).Assembly
                                .GetExportedTypes()
                                .Single(t => !t.IsInterface && !t.IsAbstract && typeof(T).IsAssignableFrom(t));
            if (type != null)
            {
                var instance = (T) Activator.CreateInstance(type);
                return instance;
            }

            return default(T);
        }


        private ParsedExpression GetBinaryExpression(BinaryExpression expression, Type boType, ParsedExpression parent)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                    var variable = expression.Right.ToString().Contains(".") ? GetKey(boType, expression.Right.ToString()) : GetKey(boType, expression.Left.ToString());
                    var value = !expression.Left.ToString().Contains(".") ? expression.Left.ToString() : expression.Right.ToString();
                    parent.Value = new ParsingForQuery(value.Replace("\"", ""),
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

        private string GetKey(Type boType, string expressionString)
        {
            var attribute = boType.GetProperties()
                                  .Where(p => p.Name == expressionString.Substring(2))
                                  .Select(p => p.GetCustomAttribute(typeof(DocumentPropertyAttribute)))
                                  .OfType<DocumentPropertyAttribute>()
                                  .SingleOrDefault();
            if (attribute == null)
                throw new Exception($"'{expressionString.Substring(2)}' is no db property");
            return attribute.Key;
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

            private string RandomString(int length)
            {
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                return new string(Enumerable.Repeat(chars, length)
                                            .Select(s => s[random.Next(s.Length)]).ToArray());
            }

            public override string ToString()
            {
                return $"{Variable} {Operator} :{Key}";
            }
        }
    }
}