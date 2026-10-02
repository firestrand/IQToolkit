IQToolkit on .NET 10
===================

This project originated on CodePlex. This fork now targets .NET 10 with
SDK-style projects and NuGet dependencies. The SDK is selected by global.json.

Build and test
--------------
Install the .NET 10 SDK (10.0.401 or a newer patch in that feature band).
Official downloads: https://dotnet.microsoft.com/download/dotnet/10.0

  dotnet restore IQToolkit.sln
  dotnet build IQToolkit.sln -c Release
  dotnet test IQToolkit.sln -c Release
  dotnet run --project Test -c Release

The last command runs the existing Northwind SQL translation suite without a
server, using the checked-in baselines. Failures produce a nonzero exit code.
MSTest runs local SQLite integration tests and core unit tests. SQL Server tests
are reported as skipped unless the connection variables below are set.

On the migration machine (Ubuntu 24.04 ARM64), the SDK is installed in
~/.dotnet and ~/.local/bin/dotnet links to it. If that directory is not on your
shell's PATH, use ~/.dotnet/dotnet or add ~/.local/bin to PATH.

Database providers
------------------
* SQLite: Microsoft.Data.Sqlite; bundled native SQLite supports Linux ARM64.
  Pass Microsoft.Data.Sqlite.SqliteConnection to SQLiteQueryProvider.
  GetConnectionString(..., failIfMissing: true) uses ReadWrite mode, which
  refuses to create missing files. Passwords require a separately configured
  encryption-capable SQLite native library; the bundled SQLite is unencrypted.
* SQL Server: Microsoft.Data.SqlClient replaces System.Data.SqlClient. Pass
  Microsoft.Data.SqlClient.SqlConnection to SqlQueryProvider. Use a reachable
  SQL Server and an appropriate connection string; local Windows instances and
  Windows integrated authentication are not portable defaults. The modern
  driver enables encryption by default; configure a trusted server certificate.
* MySQL: updated MySql.Data dependency. A MySQL server is required for execution.
* Access/OLE DB: builds with System.Data.OleDb and is explicitly marked Windows
  only. Execution requires Windows and a compatible installed Access provider.
* SQL Server Compact: legacy source/project is preserved in
  IQToolkit.Data.SqlServerCe but excluded from the modern solution. Its old
  Windows/.NET Framework driver cannot run on modern .NET or Linux ARM64.
  Use SQLite for a local cross-platform database. Compact files require a data
  migration; changing the provider does not convert an .sdf file.

The constructor parameter types for SQLite and SQL Server changed with their
new drivers. Provider namespace names used by DbEntityProvider.From remain the
same. Provider assemblies are now resolved through .NET assembly loading, so
consumers should add project/package references instead of copying DLLs into
the current directory. The old Test/CopyProvider*.cmd scripts are not used.

SQL Server integration tests (optional)
---------------------------------------
Use disposable test databases: these suites insert/update/delete test data.
Provision Northwind with its original fixture data and the extension tables
used by NorthwindExtensionTableCUDTests. CreateIQToolkitTestDb.sql supplies the
separate IQToolkitTest schema. Set connection strings in your environment:

  IQTOOLKIT_NORTHWIND_CONNECTION   connection to Northwind
  IQTOOLKIT_TEST_CONNECTION        connection to IQToolkitTest

Then run dotnet test. To run the legacy console execution/CUD suites:

  dotnet run --project Test -- --execute

Live SQL Server, MySQL and Access execution requires those external services
or platforms; the local build and SQLite tests do not provision them.
