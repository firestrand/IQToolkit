using IQToolkit;
using IQToolkit.Data;
using IQToolkit.Data.Common;
using IQToolkit.Data.SQLite;
using Microsoft.Data.Sqlite;

namespace IQToolkit.IndependentConsumer;

public static class PublicSymbols
{
    public static DbEntityProvider Create(SqliteConnection connection, QueryMapping mapping, QueryPolicy policy)
        => new SQLiteQueryProvider(connection, mapping, policy);

    public static QueryCache Cache(QueryCache cache) => cache;
}
