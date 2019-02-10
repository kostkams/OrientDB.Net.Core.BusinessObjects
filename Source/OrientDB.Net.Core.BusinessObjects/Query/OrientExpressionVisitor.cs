using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace OrientDB.Net.Core.BusinessObjects.Query
{
    public static class MyExtensionMethods
    {
        public static Dictionary<string, string> MatchNamedCaptures(this Regex regex, string input)
        {
            var namedCaptureDictionary = new Dictionary<string, string>();
            GroupCollection groups = regex.Match(input).Groups;
            string[] groupNames = regex.GetGroupNames();
            foreach (string groupName in groupNames)
                if (groups[groupName].Captures.Count > 0)
                    namedCaptureDictionary.Add(groupName, groups[groupName].Value);
            return namedCaptureDictionary;
        }
    }

    internal class OrientExpressionVisitor : ExpressionVisitor
    {
        private Type boType;
        private StringBuilder sb;
        private int? Skip { get; set; }

        private int? Take { get; set; }

        private string OrderBy { get; set; }

        private string WhereClause { get; set; }
        

        public string Translate<TBO>(Expression expression) where TBO : IBusinessObject
        {
            var bo = BoActivator.GetInstance(typeof(TBO));
            boType = bo.GetType();
            
            sb = new StringBuilder();
            Visit(expression);


            var matchCollection = new Regex("'%(?<variable>('.*?'))%'").MatchNamedCaptures(sb.ToString());

            WhereClause = matchCollection.ContainsKey("variable")
                              ? sb.ToString().Replace(matchCollection["variable"], matchCollection["variable"].Trim('\''))
                              : sb.ToString();

            var querySb = new StringBuilder($"SELECT * FROM {bo.ClassName} ");
            if (!string.IsNullOrEmpty(WhereClause))
                querySb.Append($"WHERE {WhereClause} ");
            if (!string.IsNullOrEmpty(OrderBy))
                querySb.Append($"ORDER BY {OrderBy} ");
            if (Skip.HasValue)
                querySb.Append($"SKIP {Skip.Value} ");
            if (Take.HasValue)
                querySb.Append($"LIMIT {Take.Value} ");


            return querySb.ToString();
        }

        protected bool IsNullConstant(Expression exp)
        {
            return exp.NodeType == ExpressionType.Constant && ((ConstantExpression) exp).Value == null;
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            sb.Append("(");
            Visit(b.Left);

            switch (b.NodeType)
            {
                case ExpressionType.And:
                    sb.Append(" AND ");
                    break;

                case ExpressionType.AndAlso:
                    sb.Append(" AND ");
                    break;

                case ExpressionType.Or:
                    sb.Append(" OR ");
                    break;

                case ExpressionType.OrElse:
                    sb.Append(" OR ");
                    break;

                case ExpressionType.Equal:
                    sb.Append(IsNullConstant(b.Right) ? " IS " : " = ");
                    break;

                case ExpressionType.NotEqual:
                    sb.Append(IsNullConstant(b.Right) ? " IS NOT " : " <> ");
                    break;

                case ExpressionType.LessThan:
                    sb.Append(" < ");
                    break;

                case ExpressionType.LessThanOrEqual:
                    sb.Append(" <= ");
                    break;

                case ExpressionType.GreaterThan:
                    sb.Append(" > ");
                    break;

                case ExpressionType.GreaterThanOrEqual:
                    sb.Append(" >= ");
                    break;

                default:
                    throw new NotSupportedException($"The binary operator '{b.NodeType}' is not supported");
            }

            Visit(b.Right);
            sb.Append(")");
            return b;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            var q = c.Value as IQueryable;

            if (q == null && c.Value == null)
            {
                sb.Append("NULL");
            }
            else if (q == null)
            {
                switch (Type.GetTypeCode(c.Value.GetType()))
                {
                    case TypeCode.Boolean:
                        sb.Append($"'{((bool) c.Value ? 1 : 0)}'");
                        break;

                    case TypeCode.String:
                        sb.Append($"'{c.Value}'");
                        break;

                    case TypeCode.DateTime:
                        sb.Append($"'{c.Value}'");
                        break;

                    case TypeCode.Object:
                        if (c.Value.GetType().BaseType == typeof(Array))
                        {
                            var arr = ((Array) c.Value);
                            var sb1 = new StringBuilder();
                            foreach (var item in arr)
                            {
                                sb1.Append($"'{item}',");
                            }
                            sb.Append($"[{sb1.ToString().Trim(',')}]");

                            break;
                        }
                        throw new NotSupportedException($"The constant for '{c.Value}' is not supported");

                    default:
                        sb.Append($"'{c.Value}'");
                        break;
                }
            }

            return c;
        }


        protected override Expression VisitMember(MemberExpression m)
        {
            if (m.Expression != null)
            {
                if (m.Expression.NodeType == ExpressionType.Parameter)
                {
                    var key = GetColumnName(m);
                    sb.Append(key);
                    return m;
                }

                if (m.Expression.NodeType == ExpressionType.MemberAccess)
                {
                    var res = Evaluator.PartialEval(m);
                    Visit(res);
                    return m;
                }

                if (m.Expression.NodeType == ExpressionType.Constant)
                {
                    var res = Evaluator.PartialEval(m);
                    Visit(res);
                    return m;
                }
            }

            throw new NotSupportedException($"The member '{m.Member.Name}' is not supported");
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "Where")
            {
                Visit(m.Arguments[0]);
                var lambda = (LambdaExpression) StripQuotes(m.Arguments[1]);
                Visit(lambda.Body);
                return m;
            }

            if (m.Method.Name == "Take")
            {
                if (ParseTakeExpression(m))
                {
                    var nextExpression = m.Arguments[0];
                    return Visit(nextExpression);
                }
            }
            else if (m.Method.Name == "Skip")
            {
                if (ParseSkipExpression(m))
                {
                    var nextExpression = m.Arguments[0];
                    return Visit(nextExpression);
                }
            }
            else if (m.Method.Name == "OrderBy")
            {
                if (ParseOrderByExpression(m, "ASC"))
                {
                    var nextExpression = m.Arguments[0];
                    return Visit(nextExpression);
                }
            }
            else if (m.Method.Name == "OrderByDescending")
            {
                if (ParseOrderByExpression(m, "DESC"))
                {
                    var nextExpression = m.Arguments[0];
                    return Visit(nextExpression);
                }
            }
            else if (m.Method.Name == "Contains")
            {
                if (ParseContainsExpression(m))
                {
                    var nextExpression = m.Arguments[0];
                    sb.Append("'%");
                    var visited = Visit(nextExpression);
                    sb.Append("%'");
                    return visited;
                }
                if (ParseContainsInArrayExpression(m))
                {
                    return m;
                }
            }

            throw new NotSupportedException($"The method '{m.Method.Name}' is not supported");
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            switch (u.NodeType)
            {
                case ExpressionType.Not:
                    sb.Append(" NOT ");
                    Visit(u.Operand);
                    break;
                case ExpressionType.Convert:
                    Visit(u.Operand);
                    break;
                default:
                    throw new NotSupportedException($"The unary operator '{u.NodeType}' is not supported");
            }

            return u;
        }

        private string GetColumnName(MemberExpression m)
        {
            var key = boType.GetProperty(m.Member.Name).GetCustomAttribute<DocumentPropertyAttribute>().Key;
            return key;
        }

        private bool ParseContainsExpression(MethodCallExpression expression)
        {
            if (expression.Object is MemberExpression memberExpression)
            {
                sb.Append($"{GetColumnName(memberExpression)} like ");
                return true;
            }

            return false;
        }

        private bool ParseContainsInArrayExpression(MethodCallExpression expression)
        {
            var methodCallExpression = (MethodCallExpression)Evaluator.PartialEval(expression);

            Visit(methodCallExpression.Arguments[1]);
            sb.Append(" in ");
            Visit(methodCallExpression.Arguments[0]);

            return true;
        }

        private bool ParseOrderByExpression(MethodCallExpression expression, string order)
        {
            var unary = (UnaryExpression) expression.Arguments[1];
            var lambdaExpression = (LambdaExpression) unary.Operand;

            lambdaExpression = (LambdaExpression) Evaluator.PartialEval(lambdaExpression);

            if (lambdaExpression.Body is MemberExpression body)
            {
                var key = GetColumnName(body);
                OrderBy = string.IsNullOrEmpty(OrderBy)
                              ? $"{key} {order}"
                              : $"{OrderBy}, {key} {order}";

                return true;
            }

            return false;
        }

        private bool ParseSkipExpression(MethodCallExpression expression)
        {
            var sizeExpression = (ConstantExpression) expression.Arguments[1];

            if (int.TryParse(sizeExpression.Value.ToString(), out var size))
            {
                Skip = size;
                return true;
            }

            return false;
        }

        private bool ParseTakeExpression(MethodCallExpression expression)
        {
            var sizeExpression = (ConstantExpression) expression.Arguments[1];

            if (int.TryParse(sizeExpression.Value.ToString(), out var size))
            {
                Take = size;
                return true;
            }

            return false;
        }

        private static Expression StripQuotes(Expression e)
        {
            while (e.NodeType == ExpressionType.Quote)
                e = ((UnaryExpression) e).Operand;
            return e;
        }
    }
}