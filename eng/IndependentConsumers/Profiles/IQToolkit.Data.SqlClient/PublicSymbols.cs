using IQToolkit;
using IQToolkit.Data;
using IQToolkit.Data.Common;
using IQToolkit.Data.SqlClient;
using Microsoft.Data.SqlClient;

namespace IQToolkit.IndependentConsumer;

public static class PublicSymbols
{
    public static DbEntityProvider Create(SqlConnection connection, QueryMapping mapping, QueryPolicy policy)
        => new SqlQueryProvider(connection, mapping, policy);

    public static QueryCache Cache(QueryCache cache) => cache;
}
