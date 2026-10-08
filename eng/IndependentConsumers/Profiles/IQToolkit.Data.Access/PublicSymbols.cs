using IQToolkit;
using IQToolkit.Data;
using IQToolkit.Data.Common;
using IQToolkit.Data.Access;
using System.Data.OleDb;
using System.Runtime.Versioning;

namespace IQToolkit.IndependentConsumer;

[SupportedOSPlatform("windows")]
public static class PublicSymbols
{
    public static DbEntityProvider Create(OleDbConnection connection, QueryMapping mapping, QueryPolicy policy)
        => new AccessQueryProvider(connection, mapping, policy);

    public static QueryCache Cache(QueryCache cache) => cache;
}
