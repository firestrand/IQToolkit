using System;
using IQToolkit.Data;

namespace Test
{
    class Program
    {
        static int Main(string[] args)
        {
            bool execute = args.Length == 1 && args[0] == "--execute";
            if (args.Length > 0 && !execute)
            {
                Console.Error.WriteLine("Usage: dotnet run --project Test [-- --execute]");
                return 2;
            }
            string connection = Environment.GetEnvironmentVariable("IQTOOLKIT_NORTHWIND_CONNECTION");
            if (execute && string.IsNullOrWhiteSpace(connection))
            {
                Console.Error.WriteLine("Set IQTOOLKIT_NORTHWIND_CONNECTION to a SQL Server Northwind connection string.");
                return 2;
            }
            var provider = DbEntityProvider.From("IQToolkit.Data.SqlClient",
                connection ?? "Server=localhost;Database=Northwind", "Test.NorthwindWithAttributes");
            using (provider.Connection)
            {
                var db = new Northwind(provider);
                if (execute)
                {
                    provider.Connection.Open();
                    NorthwindExecutionTests.Run(db);
                    NorthwindCUDTests.Run(db);
                }
                else
                {
                    NorthwindTranslationTests.Run(db, false);
                }
            }
            return Environment.ExitCode;
        }
    }
}
