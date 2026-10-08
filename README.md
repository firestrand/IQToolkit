# IQToolkit on .NET 10

IQToolkit is a LINQ query and entity-operation toolkit with a shared relational translator and optional database providers. This fork targets .NET 10; the SDK version and roll-forward policy are recorded in `global.json`. The original project notices and [Microsoft Public License](https://github.com/firestrand/IQToolkit/blob/master/License.txt) are retained.

## Build and verification

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). From the repository root, run the offline verification gate with Bash:

```bash
bash eng/verify.sh
```

The gate performs a locked Release restore, rebuilds with warnings treated as errors and the unused-private-member analyzer, runs MSTest with a TRX report, and checks the existing Northwind SQL translation baselines. Each invocation writes its source snapshot and results to a separate directory under `artifacts/verification/runs/`. `artifacts/verification/latest-success.txt` identifies the most recent fully passing run; failed attempts remain separate. SQL Server integration tests are explicitly skipped when their connection variables are absent; those skips do not certify SQL Server behavior.

Existing dependency versions are centralized in `Directory.Packages.props`. Console/test applications and the metadata-only API reference project have committed restore locks; library dependency ranges remain in their packages. `NuGet.config` maps dependencies to nuget.org and stores the restore cache under ignored `artifacts/nuget/packages`. Restore audits direct and transitive packages and fails on high/critical vulnerability warnings. Update locks deliberately when changing dependencies; a failed locked restore must be investigated before updating them.

The underlying SDK commands are below. Use the full gate to retain snapshots and logs and validate actual test execution; it additionally passes `ContinuousIntegrationBuild=true` when running in GitHub Actions.

```bash
dotnet restore IQToolkit.sln --locked-mode -p:Configuration=Release -warnaserror
dotnet build IQToolkit.sln -c Release --no-restore -t:Rebuild \
  -p:CheckDeadCode=true -p:EnforceCodeStyleInBuild=true \
  -p:AnalysisLevelStyle=10.0-none -p:RunAnalyzersDuringBuild=true -warnaserror
dotnet test IQToolkit.sln -c Release --no-build --no-restore
dotnet run --project Test -c Release --no-build --no-restore
```

The gate validates the actual TRX and translation output before recording success.
Both offline SQLite and native metadata suites must execute; only explicitly
categorized SQL Server integration results may be skipped. Skipped counts come
from individual results rather than the adapter's aggregate counter. The
translation harness rejects an empty selection. Existing reports can be checked
with `bash eng/verify-test-results.sh <trx-report> <translation-output>`; this
builds a framework-only validator and retains source, reports and outcomes under
`artifacts/test-results/`. These checks do not certify unconfigured providers or
replace coverage requirements.

Existing coverage evidence can be compared with
`bash eng/verify-coverage-results.sh <baseline-descriptor.json> <current-descriptor.json>`.
The descriptors identify reviewed native reports, strict execution receipts,
collector/settings, preservation manifests and matching DLL/PDB files. The
validator checks source checksums and coverage leaves, rejects empty reports,
and requires explicit qualification when source, binaries or scope change. It
retains inputs and failures under `artifacts/coverage-results/`; it does not run
the collector or replace required coverage targets. See the
[coverage report](docs/modernization-coverage-hardness.md) for the descriptor
contract and measured limitations.

Collect and compare new coverage on the qualified Linux host with
`bash eng/verify-coverage.sh <baseline-descriptor.json> <passing-strict-run-directory>`.
Run the strict gate first; the supplied run must match the entire current checkout.
The collector verifies the locked adapter, copies the complete test output outside
the checkout, supervises SDK and collector stages, and checks original files again
before publishing success. It retains reports, commands and failures under
`artifacts/coverage/runs/`. The optional third argument is a VSTest filter; its
results must still satisfy the strict suite and baseline. This command currently
requires Linux, GNU coreutils and procps. Other platform adapters, cancellation
qualification and final coverage targets remain open. The baseline is an explicit
reviewed descriptor; the tool never resets it automatically.

Run the current final-verification precursor on the qualified Linux host with
`bash eng/verify-final.sh <retained-1.1.1-api-baseline-packages>`.
It builds fresh packages, checks static API compatibility and symbols, verifies
six independent consumers and the SQLite package consumer, runs the strict gate,
and collects coverage against the explicitly pinned, qualified transition anchor.
The current profile requires the original coverage baseline and retained transition
evidence from this checkout; it is not a portable baseline for other clones.

The command currently returns nonzero even when all component checks pass.
Its retained readiness assessment lists missing coverage scope, unmet targets,
unbound mandatory facts, unmet touched-file targets and optimized cache sensitivity still unqualified.
The six fixed native cache challenges now have [diagnostic-profile evidence](docs/modernization-cache-sensitivity-hardness.md);
that evidence is not yet bound into this command and does not certify optimized artifacts.
The final verifier authenticates the original/V1y/V1z compilation chain and the six cache challenges under their diagnostic profile. The fact and touched-file classifiers retain explicit scoped results. It publishes no final-success
pointer and does not establish M0 completion. Logs, input snapshots and failures
remain under `artifacts/final-verification/runs/`. Each ordinary stage is bounded to
120 seconds; the compound coverage stage is bounded to 300 seconds. Newly owned
scratch/evidence is bounded to 2 GiB and each stage log to 16 MiB. See the
[implementation plan](docs/modernization-implementation-plan.md) for remaining gates.

## Packages and providers

The solution produces these six library packages. Choose a provider package and its declared dependencies rather than copying provider DLLs into the current directory.

| Package | Purpose | Execution requirements |
| :--- | :--- | :--- |
| `IQToolkit` | Expression evaluation, query compilation, and caching. | .NET 10. |
| `IQToolkit.Data` | Relational translation, mapping, policies, and ADO.NET execution. | A compatible provider. |
| `IQToolkit.Data.SQLite` | SQLite via `Microsoft.Data.Sqlite`. | SQLite native runtime; existing tests execute locally on Linux ARM64. |
| `IQToolkit.Data.SqlClient` | SQL Server via `Microsoft.Data.SqlClient`. | Reachable SQL Server and suitable authentication/certificate configuration. |
| `IQToolkit.Data.MySqlClient` | MySQL via `MySql.Data`. | Reachable MySQL server; local SQLite tests do not certify MySQL execution. |
| `IQToolkit.Data.Access` | Microsoft Access via `System.Data.OleDb`. | Windows and a compatible installed Access provider. |

SQLite and SQL Server constructors accept the modern driver's connection types. The namespace names used by `DbEntityProvider.From` remain unchanged. SQLite's default native library is unencrypted; passwords require an independently configured encryption-capable library. SQL Server driver encryption requires appropriate server certificate configuration.

SQL Server Compact remains preserved legacy source outside the modern solution. Its .NET Framework driver is not a supported modern .NET provider. Moving a Compact database to SQLite requires a data migration.

Produce local packages after the Release build:

```bash
dotnet pack IQToolkit.sln -c Release --no-build --no-restore -warnaserror -o artifacts/packages
dotnet msbuild eng/VerifyPackages.proj -nologo -p:PackageDirectory=artifacts/packages
```

Each library package includes this README, the original license, repository metadata, and a separate `.snupkg` containing portable debug symbols. The .NET SDK supplies Source Link; no extra Source Link dependency is required. This command does not publish packages. Git tags and locally created packages do not establish NuGet.org availability. [NuGet packaging properties](https://learn.microsoft.com/en-us/nuget/reference/msbuild-targets), [SDK Source Link behavior](https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/8.0/source-link)

For static API compatibility checks on Linux, rebuild the pinned v1.1.1 release and compare its library packages with fresh current packages:

```bash
bash eng/prepare-api-baseline.sh
api_baseline_run="$(cat artifacts/api-baselines/latest-success.txt)"
bash eng/verify-api.sh "$(cat "$api_baseline_run/packages-directory.txt")"
```

The baseline commit must exist locally; shallow clones may need additional history. Preparation builds the original release in an isolated artifact directory without changing the checkout. The API gate rebuilds current sources, retains the exact compared packages, and rejects unresolved reference assemblies. Each run records revision, dirty-source hashes, SDK and reference provenance, and its result under `artifacts/api-baselines/` or `artifacts/api-compatibility/`. The baseline release has no dependency lock, so rebuilding it does not promise byte-identical historical packages. Static compatibility does not establish runtime consumer behavior or database certification. Windows execution of these engineering scripts remains unverified.

Verify the packaged SQLite executor in an isolated consumer using the exact packages produced by the API gate:

```bash
api_current_run="$(cat artifacts/api-compatibility/latest-success.txt)"
bash eng/prepare-consumer-lock.sh "$api_current_run/packages"
# Review the lock delta before verification.
git diff -- eng/PackageConsumer/packages.lock.json
bash eng/verify-consumer.sh "$api_current_run/packages"
```

The consumer references only `IQToolkit.Data.SQLite`, exposing its declared core/Data/driver dependencies. Explicit preparation records the resolved lock for the exact local archive bytes; even fresh archives with identical extracted contents can have different hashes. Verification restores with that reviewed lock into a fresh package cache outside repository build-policy inheritance, maps toolkit IDs to the tested local feed, and compares public-executor metadata results with direct native SQLite results. Per-run evidence under `artifacts/package-consumer/` records restored package hashes/sources and loaded assembly locations; the owned consumer/cache move into that evidence directory after execution. This checks SQLite executor package runtime on the executing host. Other independent package restores, LINQ translation and provider/OS qualification remain separate checks. The verification runner never retries a failed locked restore unlocked.

## Independent package checks

Check all six packages independently against the same exact API feed:

```bash
bash eng/prepare-independent-consumers.sh "$api_current_run/packages"
# Review each resolved lock and its binding to the selected archive bytes.
git diff -- eng/IndependentConsumers/Profiles eng/IndependentConsumers/prepared-feed.sha256
bash eng/verify-independent-consumers.sh "$api_current_run/packages"
```

Each profile has one exact direct package reference, its own outside cold cache and locked graph, and strict compilation of named public toolkit/driver symbols. Access compilation declares its Windows boundary; it does not run the provider. Evidence under `artifacts/independent-consumers/` records per-profile restore, hashes, source provenance and compiled assembly references. Success requires all six profiles. These checks establish package contracts on the executing SDK/host, not backend runtime certification.

Standalone projects are stored as `.csproj.template` and reconstructed only outside the repository. Evidence projects become byte-identical `.csproj.snapshot` files before relocation, preventing ambient workspace restores from changing recorded locks. Filename/hash mappings retain replay provenance.

## Optional SQL Server integration

Use disposable databases because the execution suites insert, update, and delete rows. Provision the original Northwind fixture and required extension tables, and use `CreateIQToolkitTestDb.sql` for the separate IQToolkitTest schema. Supply connection strings through these environment variables:

| Variable | Target |
| :--- | :--- |
| `IQTOOLKIT_NORTHWIND_CONNECTION` | SQL Server Northwind database. |
| `IQTOOLKIT_TEST_CONNECTION` | SQL Server IQToolkitTest database. |

Run `dotnet test` with the variables configured. For the legacy console execution and CUD suites:

```bash
dotnet run --project Test -- --execute
```

The build gate does not provision SQL Server, MySQL, or Access. Source history and migration details remain in [the original README](https://github.com/firestrand/IQToolkit/blob/master/README.txt).

## Continuous integration and roadmap

The checked-in workflow declares Linux x64, Linux ARM64, Windows x64, and macOS ARM64 qualification jobs. Each job runs the offline gate and packs/inspects the six libraries and symbol packages. CI commands explicitly set `ContinuousIntegrationBuild=true`; build/pack/publish reject `LocalWarningRelaxation=true` or `TreatWarningsAsErrors=false`. Linux jobs also rebuild the pinned release and compare static APIs. Logs and compared packages are retained as workflow artifacts. It uses release commit pins, the SDK selected by `global.json`, and read-only repository permissions. A declared matrix is not evidence of a successful hosted run or live Access execution. [GitHub runner labels](https://docs.github.com/en/actions/reference/runners/github-hosted-runners), [setup-dotnet configuration](https://github.com/actions/setup-dotnet)

The unreleased development package version is `1.1.2-preview.1`. `Directory.Build.props` is the version source; the SDK generates version attributes while preserving legacy non-version metadata. AssemblyVersion stays at `1.1.1.0` for binary identity compatibility; FileVersion follows the numeric development version. Local artifacts from a dirty checkout do not certify commit-specific source retrieval. Release qualification requires a clean committed source revision and the declared platform/provider checks.

Modernization and additional database families are described in [the roadmap](https://github.com/firestrand/IQToolkit/blob/master/docs/modernization-roadmap.md). They are planned capabilities, not features supplied by the current packages.
