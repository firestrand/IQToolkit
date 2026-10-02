using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace IQToolkit.Data.SQLite
{
    using IQToolkit.Data.Common;

    public class SQLiteQueryProvider : DbEntityProvider
    {
        public SQLiteQueryProvider(SqliteConnection connection, QueryMapping mapping, QueryPolicy policy)
            : base(connection, SQLiteLanguage.Default, mapping, policy) { }

        public static string GetConnectionString(string databaseFile)
            => GetConnectionString(databaseFile, null, false);

        public static string GetConnectionString(string databaseFile, string password)
            => GetConnectionString(databaseFile, password, false);

        public static string GetConnectionString(string databaseFile, bool failIfMissing)
            => GetConnectionString(databaseFile, null, failIfMissing);

        public static string GetConnectionString(string databaseFile, string password, bool failIfMissing)
        {
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = databaseFile,
                Mode = failIfMissing ? SqliteOpenMode.ReadWrite : SqliteOpenMode.ReadWriteCreate
            };
            if (!string.IsNullOrEmpty(password))
                builder.Password = password;
            return builder.ToString();
        }

        public override DbEntityProvider New(DbConnection connection, QueryMapping mapping, QueryPolicy policy)
            => new SQLiteQueryProvider((SqliteConnection)connection, mapping, policy);
    }
}
