using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace WhaleES.Denormalizers
{
    internal class QueryTranslator : ExpressionVisitor
    {
        private Expression expression = null;
        private StringBuilder query = null;
        private string lastAttribute = string.Empty;

        private int precision = 0;
        private int scale = 0;

        private int whereDepth = 0;

        private bool inBrackets = false;

        public QueryTranslator(Expression exp, int precision, int scale)
        {
            this.expression = exp;
            this.precision = precision;
            this.scale = scale;
        }

        public string Query
        {
            get
            {
                if (query == null)
                {
                    query = new StringBuilder();
                    //query.Append("[");
                    this.Visit(this.expression);
                    if (inBrackets)
                    {
                        query.Append("]");
                        inBrackets = false;
                    }
                }
                string queryText = query.ToString();

                return queryText.Length <= 2 ? string.Empty : queryText;
            }
        }

        protected override Expression VisitUnary(UnaryExpression ue)
        {
            switch (ue.NodeType)
            {
                case ExpressionType.Not:
                    query.Append("not ");
                    break;
                case ExpressionType.Quote:
                    return ue;
                default:
                    throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "The unary operator '{0}' is not supported", ue.NodeType));
            }
            this.Visit(ue.Operand);
            return ue;
        }

        protected override Expression VisitBinary(BinaryExpression be)
        {
            if (!inBrackets)
            {
                query.Append("[");
                inBrackets = true;
            }
            this.Visit(be.Left);
            switch (be.NodeType)
            {
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    query.Append(" and ");
                    break;
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    query.Append(" or ");
                    break;
                case ExpressionType.Equal:
                    query.Append(" = ");
                    break;
                case ExpressionType.NotEqual:
                    query.Append(" != ");
                    break;
                case ExpressionType.LessThan:
                    query.Append(" < ");
                    break;
                case ExpressionType.LessThanOrEqual:
                    query.Append(" <= ");
                    break;
                case ExpressionType.GreaterThan:
                    query.Append(" > ");
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    query.Append(" >= ");
                    break;
                default:
                    throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "The binary operator '{0}' is not supported", be.NodeType));
            }
            this.Visit(be.Right);
            return be;
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Object == null && m.Method.Name == "Where")
            {
                LambdaExpression lambdaExpression = (LambdaExpression)((UnaryExpression)(m.Arguments[1])).Operand;

                // Send the lambda expression through the partial evaluator.
                lambdaExpression = (LambdaExpression)Evaluator.PartialEval(lambdaExpression);

                whereDepth++;
                if (whereDepth > 1)
                {
                    throw new Exception("SimpleDB does not support nested \"WHERE\" clauses");
                }
                Visit(m.Arguments[0]);
                Visit(lambdaExpression.Body);

                whereDepth--;

                return m;
            }
            else if (m.Object != null && String.Equals(m.Method.Name, "get_Item"))
            {
                string val = m.Arguments[0].ToString();
                if (val.Length > 2)
                {
                    string attribute = val.Substring(1, val.Length - 2);
                    if (!String.IsNullOrEmpty(lastAttribute) && attribute != lastAttribute)
                    {
                        throw new Exception("You cannot use more than one apiAttribute in a single query, use a Union or Intersect to join multiple attributes");
                    }
                    lastAttribute = attribute;
                    query.Append("'");
                    query.Append(attribute);
                    query.Append("'");
                }
                return m;
            }
            else if (m.Object == null && String.Equals(m.Method.Name, "Union"))
            {
                //reset the last apiAttribute        
                this.Visit(m.Arguments[0]);
                if (inBrackets)
                {
                    query.Append("]");
                    inBrackets = false;
                }
                query.Append(" UNION ");
                lastAttribute = string.Empty;
                this.Visit(m.Arguments[1]);
                return m;
            }
            else if (m.Object == null && String.Equals(m.Method.Name, "Intersect"))
            {
                //reset the last apiAttribute
                this.Visit(m.Arguments[0]);
                if (inBrackets)
                {
                    query.Append("]");
                    inBrackets = false;
                }
                query.Append(" INTERSECT ");
                lastAttribute = string.Empty;
                this.Visit(m.Arguments[1]);
                return m;
            }
            else if (m.Object == null && String.Equals(m.Method.Name, "Join"))
            {
                throw new Exception("SimpleDB does not support joins.");
            }
            else if (m.Object == null && String.Equals(m.Method.Name, "OrderBy"))
            {
                throw new Exception("SimpleDB does not support ordering.");
            }
            else if (m.Method.Name == "StartsWith")
            {
                if (!inBrackets)
                {
                    query.Append("[");
                    inBrackets = true;
                }
                this.Visit(m.Object);
                query.Append(" starts-with ");
                this.Visit(m.Arguments[0]);
                return m;
            }
            return base.VisitMethodCall(m);
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            IQueryable q = c.Value as IQueryable;
            if (q == null)
            {
                switch (Type.GetTypeCode(c.Value.GetType()))
                {
                    case TypeCode.Boolean:
                        query.Append("'");
                        query.Append(((bool)c.Value) ? "true" : "false");
                        query.Append("'");
                        break;
                    case TypeCode.String:
                        query.Append("'");
                        query.Append(c.Value);
                        query.Append("'");
                        break;
                    case TypeCode.Decimal:
                        query.Append("'");
                        query.Append(SimpleDBNumericHelper.PadAndFormatNumber((Decimal)c.Value, precision, scale));
                        query.Append("'");
                        break;
                    case TypeCode.Single:
                    case TypeCode.Double:
                        query.Append("'");
                        query.Append(SimpleDBNumericHelper.PadAndFormatNumber((Double)c.Value, precision, scale));
                        query.Append("'");
                        break;
                    case TypeCode.SByte:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                        query.Append("'");
                        query.Append(SimpleDBNumericHelper.PadAndFormatNumber((Int32)c.Value, precision, scale));
                        query.Append("'");
                        break;
                    case TypeCode.Int64:
                        query.Append("'");
                        query.Append(SimpleDBNumericHelper.PadAndFormatNumber((Int64)c.Value, precision, scale));
                        query.Append("'");
                        break;
                    case TypeCode.Byte:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                        query.Append("'");
                        query.Append(SimpleDBNumericHelper.PadAndFormatNumber((UInt32)c.Value, precision, scale));
                        query.Append("'");
                        break;
                    case TypeCode.UInt64:
                        query.Append("'");
                        query.Append(SimpleDBNumericHelper.PadAndFormatNumber((UInt64)c.Value, precision, scale));
                        query.Append("'");
                        break;
                    case TypeCode.Object:
                        throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "The constant for '{0}' is not supported", c.Value));
                    default:
                        throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "The constant for '{0}' is not supported", c.Value));
                }
            }
            return c;
        }
    }
    public abstract class ExpressionVisitor
    {
        protected ExpressionVisitor()
        {
        }

        protected virtual Expression Visit(Expression exp)
        {
            if (exp == null)
                return exp;
            switch (exp.NodeType)
            {
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.ArrayLength:
                case ExpressionType.Quote:
                case ExpressionType.TypeAs:
                    return this.VisitUnary((UnaryExpression)exp);
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.Coalesce:
                case ExpressionType.ArrayIndex:
                case ExpressionType.RightShift:
                case ExpressionType.LeftShift:
                case ExpressionType.ExclusiveOr:
                    return this.VisitBinary((BinaryExpression)exp);
                case ExpressionType.TypeIs:
                    return this.VisitTypeIs((TypeBinaryExpression)exp);
                case ExpressionType.Conditional:
                    return this.VisitConditional((ConditionalExpression)exp);
                case ExpressionType.Constant:
                    return this.VisitConstant((ConstantExpression)exp);
                case ExpressionType.Parameter:
                    return this.VisitParameter((ParameterExpression)exp);
                case ExpressionType.MemberAccess:
                    return this.VisitMemberAccess((MemberExpression)exp);
                case ExpressionType.Call:
                    return this.VisitMethodCall((MethodCallExpression)exp);
                case ExpressionType.Lambda:
                    return this.VisitLambda((LambdaExpression)exp);
                case ExpressionType.New:
                    return this.VisitNew((NewExpression)exp);
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    return this.VisitNewArray((NewArrayExpression)exp);
                case ExpressionType.Invoke:
                    return this.VisitInvocation((InvocationExpression)exp);
                case ExpressionType.MemberInit:
                    return this.VisitMemberInit((MemberInitExpression)exp);
                case ExpressionType.ListInit:
                    return this.VisitListInit((ListInitExpression)exp);
                default:
                    throw new Exception(string.Format("Unhandled expression type: '{0}'", exp.NodeType));
            }
        }

        protected virtual MemberBinding VisitBinding(MemberBinding binding)
        {
            switch (binding.BindingType)
            {
                case MemberBindingType.Assignment:
                    return this.VisitMemberAssignment((MemberAssignment)binding);
                case MemberBindingType.MemberBinding:
                    return this.VisitMemberMemberBinding((MemberMemberBinding)binding);
                case MemberBindingType.ListBinding:
                    return this.VisitMemberListBinding((MemberListBinding)binding);
                default:
                    throw new Exception(string.Format("Unhandled binding type '{0}'", binding.BindingType));
            }
        }

        protected virtual ElementInit VisitElementInitializer(ElementInit initializer)
        {
            ReadOnlyCollection<Expression> arguments = this.VisitExpressionList(initializer.Arguments);
            if (arguments != initializer.Arguments)
            {
                return Expression.ElementInit(initializer.AddMethod, arguments);
            }
            return initializer;
        }

        protected virtual Expression VisitUnary(UnaryExpression u)
        {
            Expression operand = this.Visit(u.Operand);
            if (operand != u.Operand)
            {
                return Expression.MakeUnary(u.NodeType, operand, u.Type, u.Method);
            }
            return u;
        }

        protected virtual Expression VisitBinary(BinaryExpression b)
        {
            Expression left = this.Visit(b.Left);
            Expression right = this.Visit(b.Right);
            Expression conversion = this.Visit(b.Conversion);
            if (left != b.Left || right != b.Right || conversion != b.Conversion)
            {
                if (b.NodeType == ExpressionType.Coalesce && b.Conversion != null)
                    return Expression.Coalesce(left, right, conversion as LambdaExpression);
                else
                    return Expression.MakeBinary(b.NodeType, left, right, b.IsLiftedToNull, b.Method);
            }
            return b;
        }

        protected virtual Expression VisitTypeIs(TypeBinaryExpression b)
        {
            Expression expr = this.Visit(b.Expression);
            if (expr != b.Expression)
            {
                return Expression.TypeIs(expr, b.TypeOperand);
            }
            return b;
        }

        protected virtual Expression VisitConstant(ConstantExpression c)
        {
            return c;
        }

        protected virtual Expression VisitConditional(ConditionalExpression c)
        {
            Expression test = this.Visit(c.Test);
            Expression ifTrue = this.Visit(c.IfTrue);
            Expression ifFalse = this.Visit(c.IfFalse);
            if (test != c.Test || ifTrue != c.IfTrue || ifFalse != c.IfFalse)
            {
                return Expression.Condition(test, ifTrue, ifFalse);
            }
            return c;
        }

        protected virtual Expression VisitParameter(ParameterExpression p)
        {
            return p;
        }

        protected virtual Expression VisitMemberAccess(MemberExpression m)
        {
            Expression exp = this.Visit(m.Expression);
            if (exp != m.Expression)
            {
                return Expression.MakeMemberAccess(exp, m.Member);
            }
            return m;
        }

        protected virtual Expression VisitMethodCall(MethodCallExpression m)
        {
            Expression obj = this.Visit(m.Object);
            IEnumerable<Expression> args = this.VisitExpressionList(m.Arguments);
            if (obj != m.Object || args != m.Arguments)
            {
                return Expression.Call(obj, m.Method, args);
            }
            return m;
        }

        protected virtual ReadOnlyCollection<Expression> VisitExpressionList(ReadOnlyCollection<Expression> original)
        {
            List<Expression> list = null;
            for (int i = 0, n = original.Count; i < n; i++)
            {
                Expression p = this.Visit(original[i]);
                if (list != null)
                {
                    list.Add(p);
                }
                else if (p != original[i])
                {
                    list = new List<Expression>(n);
                    for (int j = 0; j < i; j++)
                    {
                        list.Add(original[j]);
                    }
                    list.Add(p);
                }
            }
            if (list != null)
            {
                return list.AsReadOnly();
            }
            return original;
        }

        protected virtual MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
        {
            Expression e = this.Visit(assignment.Expression);
            if (e != assignment.Expression)
            {
                return Expression.Bind(assignment.Member, e);
            }
            return assignment;
        }

        protected virtual MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding)
        {
            IEnumerable<MemberBinding> bindings = this.VisitBindingList(binding.Bindings);
            if (bindings != binding.Bindings)
            {
                return Expression.MemberBind(binding.Member, bindings);
            }
            return binding;
        }

        protected virtual MemberListBinding VisitMemberListBinding(MemberListBinding binding)
        {
            IEnumerable<ElementInit> initializers = this.VisitElementInitializerList(binding.Initializers);
            if (initializers != binding.Initializers)
            {
                return Expression.ListBind(binding.Member, initializers);
            }
            return binding;
        }

        protected virtual IEnumerable<MemberBinding> VisitBindingList(ReadOnlyCollection<MemberBinding> original)
        {
            List<MemberBinding> list = null;
            for (int i = 0, n = original.Count; i < n; i++)
            {
                MemberBinding b = this.VisitBinding(original[i]);
                if (list != null)
                {
                    list.Add(b);
                }
                else if (b != original[i])
                {
                    list = new List<MemberBinding>(n);
                    for (int j = 0; j < i; j++)
                    {
                        list.Add(original[j]);
                    }
                    list.Add(b);
                }
            }
            if (list != null)
                return list;
            return original;
        }

        protected virtual IEnumerable<ElementInit> VisitElementInitializerList(ReadOnlyCollection<ElementInit> original)
        {
            List<ElementInit> list = null;
            for (int i = 0, n = original.Count; i < n; i++)
            {
                ElementInit init = this.VisitElementInitializer(original[i]);
                if (list != null)
                {
                    list.Add(init);
                }
                else if (init != original[i])
                {
                    list = new List<ElementInit>(n);
                    for (int j = 0; j < i; j++)
                    {
                        list.Add(original[j]);
                    }
                    list.Add(init);
                }
            }
            if (list != null)
                return list;
            return original;
        }

        protected virtual Expression VisitLambda(LambdaExpression lambda)
        {
            Expression body = this.Visit(lambda.Body);
            if (body != lambda.Body)
            {
                return Expression.Lambda(lambda.Type, body, lambda.Parameters);
            }
            return lambda;
        }

        protected virtual NewExpression VisitNew(NewExpression nex)
        {
            IEnumerable<Expression> args = this.VisitExpressionList(nex.Arguments);
            if (args != nex.Arguments)
            {
                if (nex.Members != null)
                    return Expression.New(nex.Constructor, args, nex.Members);
                else
                    return Expression.New(nex.Constructor, args);
            }
            return nex;
        }

        protected virtual Expression VisitMemberInit(MemberInitExpression init)
        {
            NewExpression n = this.VisitNew(init.NewExpression);
            IEnumerable<MemberBinding> bindings = this.VisitBindingList(init.Bindings);
            if (n != init.NewExpression || bindings != init.Bindings)
            {
                return Expression.MemberInit(n, bindings);
            }
            return init;
        }

        protected virtual Expression VisitListInit(ListInitExpression init)
        {
            NewExpression n = this.VisitNew(init.NewExpression);
            IEnumerable<ElementInit> initializers = this.VisitElementInitializerList(init.Initializers);
            if (n != init.NewExpression || initializers != init.Initializers)
            {
                return Expression.ListInit(n, initializers);
            }
            return init;
        }

        protected virtual Expression VisitNewArray(NewArrayExpression na)
        {
            IEnumerable<Expression> exprs = this.VisitExpressionList(na.Expressions);
            if (exprs != na.Expressions)
            {
                if (na.NodeType == ExpressionType.NewArrayInit)
                {
                    return Expression.NewArrayInit(na.Type.GetElementType(), exprs);
                }
                else
                {
                    return Expression.NewArrayBounds(na.Type.GetElementType(), exprs);
                }
            }
            return na;
        }

        protected virtual Expression VisitInvocation(InvocationExpression iv)
        {
            IEnumerable<Expression> args = this.VisitExpressionList(iv.Arguments);
            Expression expr = this.Visit(iv.Expression);
            if (args != iv.Arguments || expr != iv.Expression)
            {
                return Expression.Invoke(expr, args);
            }
            return iv;
        }
    }
    internal static class SimpleDBNumericHelper
    {
        public static string PadAndFormatNumber(Int32 num, int precision, int scale)
        {
            return PadAndFormatNumber(num.ToString(CultureInfo.InvariantCulture), precision, scale);
        }

        public static string PadAndFormatNumber(Int64 num, int precision, int scale)
        {
            return PadAndFormatNumber(num.ToString(CultureInfo.InvariantCulture), precision, scale);
        }

        public static string PadAndFormatNumber(UInt32 num, int precision, int scale)
        {
            return PadAndFormatNumber(num.ToString(CultureInfo.InvariantCulture), precision, scale);
        }

        public static string PadAndFormatNumber(UInt64 num, int precision, int scale)
        {
            return PadAndFormatNumber(num.ToString(CultureInfo.InvariantCulture), precision, scale);
        }

        public static string PadAndFormatNumber(double num, int precision, int scale)
        {
            return PadAndFormatNumber(num.ToString(CultureInfo.InvariantCulture), precision, scale);
        }

        public static string PadAndFormatNumber(Decimal num, int precision, int scale)
        {
            return PadAndFormatNumber(num.ToString(CultureInfo.InvariantCulture), precision, scale);
        }

        private static int WholeNumberPaddingCharacters(int precision, int scale)
        {
            return precision - scale;
        }

        private static int FractionalPaddingCharacters(int precision, int scale)
        {
            return scale;
        }

        private static string PadAndFormatNumber(string num, int precision, int scale)
        {
            string result = string.Empty;
            string[] parts = num.Split(Char.Parse(CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator));

            int wholePlaces = WholeNumberPaddingCharacters(precision, scale);
            if (parts[0].Length > wholePlaces)
            {
                throw new Exception("Number had too many whole number places.");
            }
            result = parts[0].PadLeft(wholePlaces, '0');
            result += ".";
            int fractionalPlaces = FractionalPaddingCharacters(precision, scale);
            if (parts.Length > 1)
            {
                if (parts[1].Length < fractionalPlaces)
                {
                    result += parts[1].PadRight(fractionalPlaces, '0');
                }
                else
                {
                    result += parts[1].Substring(0, fractionalPlaces);
                }
            }
            else
            {
                result += string.Empty.PadRight(fractionalPlaces, '0');
            }
            return result;
        }
    }
    public static class Evaluator
    {
        /// <summary>
        /// Performs evaluation & replacement of independent sub-trees
        /// </summary>
        /// <param name="expression">The root of the expression tree.</param>
        /// <param name="fnCanBeEvaluated">A function that decides whether a given expression node can be part of the local function.</param>
        /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
        public static Expression PartialEval(Expression expression, Func<Expression, bool> fnCanBeEvaluated)
        {
            return new SubtreeEvaluator(new Nominator(fnCanBeEvaluated).Nominate(expression)).Eval(expression);
        }

        /// <summary>
        /// Performs evaluation & replacement of independent sub-trees
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
        /// Evaluates & replaces sub-trees when first candidate is reached (top-down)
        /// </summary>
        class SubtreeEvaluator : ExpressionVisitor
        {
            HashSet<Expression> candidates;

            internal SubtreeEvaluator(HashSet<Expression> candidates)
            {
                this.candidates = candidates;
            }

            internal Expression Eval(Expression exp)
            {
                return this.Visit(exp);
            }

            protected override Expression Visit(Expression exp)
            {
                if (exp == null)
                {
                    return null;
                }
                if (this.candidates.Contains(exp))
                {
                    return this.Evaluate(exp);
                }
                return base.Visit(exp);
            }

            private Expression Evaluate(Expression e)
            {
                if (e.NodeType == ExpressionType.Constant)
                {
                    return e;
                }
                LambdaExpression lambda = Expression.Lambda(e);
                Delegate fn = lambda.Compile();
                return Expression.Constant(fn.DynamicInvoke(null), e.Type);
            }
        }

        /// <summary>
        /// Performs bottom-up analysis to determine which nodes can possibly
        /// be part of an evaluated sub-tree.
        /// </summary>
        class Nominator : ExpressionVisitor
        {
            Func<Expression, bool> fnCanBeEvaluated;
            HashSet<Expression> candidates;
            bool cannotBeEvaluated;

            internal Nominator(Func<Expression, bool> fnCanBeEvaluated)
            {
                this.fnCanBeEvaluated = fnCanBeEvaluated;
            }

            internal HashSet<Expression> Nominate(Expression expression)
            {
                this.candidates = new HashSet<Expression>();
                this.Visit(expression);
                return this.candidates;
            }

            protected override Expression Visit(Expression expression)
            {
                if (expression != null)
                {
                    bool saveCannotBeEvaluated = this.cannotBeEvaluated;
                    this.cannotBeEvaluated = false;
                    base.Visit(expression);
                    if (!this.cannotBeEvaluated)
                    {
                        if (this.fnCanBeEvaluated(expression))
                        {
                            this.candidates.Add(expression);
                        }
                        else
                        {
                            this.cannotBeEvaluated = true;
                        }
                    }
                    this.cannotBeEvaluated |= saveCannotBeEvaluated;
                }
                return expression;
            }
        }
    }


}