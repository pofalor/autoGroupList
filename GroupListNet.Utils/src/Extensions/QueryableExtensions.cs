using System.Linq.Expressions;

namespace GroupListNet.Utils.src.Extensions
{
    public static class QueryableExtensions
    {
        public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool condition, Expression<Func<T, bool>> predicate)
        {
            ArgumentChecker.NotNull(predicate, nameof(predicate));
            ArgumentChecker.NotNull(query, nameof(query));
            if (condition)
            {
                return query.Where(predicate);
            }

            return query;
        }
    }
}
