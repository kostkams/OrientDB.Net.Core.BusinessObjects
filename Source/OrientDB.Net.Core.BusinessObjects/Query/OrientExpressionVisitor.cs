using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace OrientDB.Net.Core.BusinessObjects.Query
{
    internal class OrientExpressionVisitor : ExpressionVisitor
    {
        private Type boType;
        private StringBuilder sb;
        private int? Skip { get; set; }

        private int? Take { get;  set; }

        private string OrderBy { get; set; }

        private string WhereClause { get; set; }

        public IDictionary<string, string> Parameters { get; private set; }

        public string Translate<TBO>(Expression expression) where TBO : IBusinessObject
        {
            var bo = BoActivator.GetInstance(typeof(TBO));
            boType = bo.GetType();

            Parameters = new Dictionary<string, string>();
            sb = new StringBuilder();
            Visit(expression);
            WhereClause = sb.ToString();

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
                    if (IsNullConstant(b.Right))
                        sb.Append(" IS ");
                    else
                        sb.Append(" = ");
                    break;

                case ExpressionType.NotEqual:
                    if (IsNullConstant(b.Right))
                        sb.Append(" IS NOT ");
                    else
                        sb.Append(" <> ");
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

        private string RandomString()
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new string(Enumerable.Repeat(chars, 5)
                                        .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        protected override Expression VisitConstant(ConstantExpression c)
        {
            var q = c.Value as IQueryable;

            if (q == null && c.Value == null)
                sb.Append("NULL");
            else if (q == null)
            {
                var key = RandomString();
                sb.Append($":{key}");
                switch (Type.GetTypeCode(c.Value.GetType()))
                {
                    case TypeCode.Boolean:
                        Parameters.Add(key, ((bool)c.Value ? 1 : 0).ToString());
                        break;

                    case TypeCode.String:
                        Parameters.Add(key, c.Value.ToString());
                        break;

                    case TypeCode.DateTime:
                        Parameters.Add(key, c.Value.ToString());
                        break;

                    case TypeCode.Object:
                        throw new NotSupportedException($"The constant for '{c.Value}' is not supported");

                    default:
                        Parameters.Add(key, c.Value.ToString());
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
            }

            if (m.Expression != null && m.Expression.NodeType == ExpressionType.Parameter)
            {
            }

            throw new NotSupportedException($"The member '{m.Member.Name}' is not supported");
        }

        private string GetColumnName(MemberExpression m)
        {
            var key = boType.GetProperty(m.Member.Name).GetCustomAttribute<DocumentPropertyAttribute>().Key;
            return key;
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