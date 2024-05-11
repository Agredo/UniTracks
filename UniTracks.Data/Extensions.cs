using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace UniTracks.Data;

public static class Extensions
{
    public static IQueryable<T> IncludeMultiple<T>(this IQueryable<T> query, params Expression<Func<T, object>>[] includes)
    where T : class
    {
        if (includes != null && includes.Length > 0)
        {
            query = includes.Aggregate(query,
                      (current, include) => current.Include(include));
        }

        return query;
    }
}
