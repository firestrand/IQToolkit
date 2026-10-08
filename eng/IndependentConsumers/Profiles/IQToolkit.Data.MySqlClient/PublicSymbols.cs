using IQToolkit;
using IQToolkit.Data;
using IQToolkit.Data.Common;
using IQToolkit.Data.MySqlClient;
using MySql.Data.MySqlClient;

namespace IQToolkit.IndependentConsumer;

public static class PublicSymbols
{
    public static DbEntityProvider Create(MySqlConnection connection, QueryMapping mapping, QueryPolicy policy)
        => new MySqlQueryProvider(connection, mapping, policy);

    public static QueryCache Cache(QueryCache cache) => cache;
}
