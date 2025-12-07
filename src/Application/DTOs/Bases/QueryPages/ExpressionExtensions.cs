using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NextAdmin.Application.DTOs.Bases.QueryPages
{
    /// <summary>
    /// 表达式树扩展方法，支持表达式合并（And/Or）
    /// </summary>
    public static class ExpressionExtensions
    {
        /// <summary>
        /// 合并两个表达式，等价于 expr1 AND expr2
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
