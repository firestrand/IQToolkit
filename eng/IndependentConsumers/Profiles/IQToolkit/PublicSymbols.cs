using IQToolkit;
using System.Linq.Expressions;

namespace IQToolkit.IndependentConsumer;

public static class PublicSymbols
{
    public static object Execute(QueryCache cache, Expression query) => cache.Execute(query);
}
