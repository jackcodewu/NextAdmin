using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NextAdmin.Application.DTOs.Bases.QueryPages
{
    /// <summary>
    /// Expression tree extension methods, supports expression merging (And/Or)
    /// </summary>
    public static class ExpressionExtensions
    {
        /// <summary>
        /// Merges two expressions, equivalent to expr1 AND expr2
        /// </summary>
        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
        {
            var parameter = Expression.Parameter(typeof(T));
            var body = Expression.AndAlso(
                Expression.Invoke(expr1, parameter),
                Expression.Invoke(expr2, parameter)
            );
            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }
    }
}
