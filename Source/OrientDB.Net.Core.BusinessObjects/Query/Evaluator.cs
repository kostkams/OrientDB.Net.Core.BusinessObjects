﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace OrientDB.Net.Core.BusinessObjects.Query
{
    public static class Evaluator
    {
        /// <summary>
        ///     Performs evaluation & replacement of independent sub-trees
        /// </summary>
        /// <param name="expression">The root of the expression tree.</param>
        /// <param name="fnCanBeEvaluated">
        ///     A function that decides whether a given expression node can be part of the local
        ///     function.
        /// </param>
        /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
        public static Expression PartialEval(Expression expression, Func<Expression, bool> fnCanBeEvaluated)
        {
            return new SubtreeEvaluator(new Nominator(fnCanBeEvaluated).Nominate(expression)).Eval(expression);
        }


        /// <summary>
        ///     Performs evaluation & replacement of independent sub-trees
        /// </summary>
        /// <param name="expression">The root of the expression tree.</param>
        /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
        public static Expression PartialEval(Expression expression)
        {
            return PartialEval(expression, Evaluator.CanBeEvaluatedLocally);
        }


        private static bool CanBeEvaluatedLocally(Expression expression)
        {
            return expression.NodeType != ExpressionType.Parameter;
        }


        /// <summary>
        ///     Evaluates & replaces sub-trees when first candidate is reached (top-down)
        /// </summary>
        private class SubtreeEvaluator : ExpressionVisitor
        {
            private readonly HashSet<Expression> candidates;


            internal SubtreeEvaluator(HashSet<Expression> candidates)
            {
                this.candidates = candidates;
            }


            public override Expression Visit(Expression exp)
            {
                if (exp == null) return null;


                if (candidates.Contains(exp)) return Evaluate(exp);


                return base.Visit(exp);
            }


            internal Expression Eval(Expression exp)
            {
                return Visit(exp);
            }


            private Expression Evaluate(Expression e)
            {
                if (e.NodeType == ExpressionType.Constant) return e;


                LambdaExpression lambda = Expression.Lambda(e);


                Delegate fn = lambda.Compile();


                return Expression.Constant(fn.DynamicInvoke(null), e.Type);
            }
        }


        /// <summary>
        ///     Performs bottom-up analysis to determine which nodes can possibly
        ///     be part of an evaluated sub-tree.
        /// </summary>
        private class Nominator : ExpressionVisitor
        {
            private HashSet<Expression> candidates;


            private bool cannotBeEvaluated;


            private readonly Func<Expression, bool> fnCanBeEvaluated;


            internal Nominator(Func<Expression, bool> fnCanBeEvaluated)
            {
                this.fnCanBeEvaluated = fnCanBeEvaluated;
            }


            public override Expression Visit(Expression expression)
            {
                if (expression != null)
                {
                    var saveCannotBeEvaluated = cannotBeEvaluated;


                    cannotBeEvaluated = false;


                    base.Visit(expression);


                    if (!cannotBeEvaluated)
                    {
                        if (this.fnCanBeEvaluated(expression))
                            candidates.Add(expression);


                        else
                            cannotBeEvaluated = true;
                    }


                    cannotBeEvaluated |= saveCannotBeEvaluated;
                }


                return expression;
            }


            internal HashSet<Expression> Nominate(Expression expression)
            {
                candidates = new HashSet<Expression>();


                Visit(expression);


                return candidates;
            }
        }
    }
}