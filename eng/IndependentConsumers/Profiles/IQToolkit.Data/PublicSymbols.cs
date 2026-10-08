using IQToolkit;
using IQToolkit.Data;
using IQToolkit.Data.Common;
using System.Data.Common;

namespace IQToolkit.IndependentConsumer;

public static class PublicSymbols
{
    public static DbEntityProvider Create(DbConnection connection, QueryLanguage language, QueryMapping mapping, QueryPolicy policy)
        => new(connection, language, mapping, policy);

    public static QueryCache Cache(QueryCache cache) => cache;
}
