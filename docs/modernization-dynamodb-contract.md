---
title: DynamoDB keyed-operation contract proposal
version: 0.1
last_updated: 2026-10-03
status: Proposal for owner review
owner: IQToolkit project owner
tags: [modernization, dynamodb, keyed-operations, contracts]
---

# DynamoDB keyed-operation contract proposal

This D3 design packet makes `LOCAL-RM-M3` in the [implementation plan](modernization-implementation-plan.md) concrete against the complete [roadmap](modernization-roadmap.md). It preserves the selected `LOCAL-RM-MAPPING` and `LOCAL-RM-QUERY` work and the DynamoDB follow-ons in `LOCAL-RM-LATER`. No proposed interface, package, mapping, budget default or provider support claim is implemented or accepted by this document.

Production M3 still requires M1 and an owner-reviewed keyed-operation contract. The [D1 execution proposal](modernization-execution-contract.md), generated-data authorization, coverage targets and AWS environment decisions remain separate unanswered gates. Read-only design preparation does not resolve them. No fixtures, SDK installation, service calls or AWS resources were needed for this document.

## Standards and evidence boundary

The owner-selected Cross-Platform C#/.NET Engineering Standard, `ENG-STD-DOTNET-XPLAT` version `1.1.0-draft.1`, governs this work. Its [repository snapshot](standards/source/Cross-Platform-DotNet-Engineering-Standard-1.1.0-draft.1.md) matches the selected source SHA256 `8c56930ec1cd26e8b58a273d47cb0b9713f55e6d4961b6d29a636ffaf4f0ebe2`. Applicable rules include project dependency boundaries (`DOTNET-REPO-003`), strict new-code/analyzer controls (`DOTNET-REPO-004`), central/locked/mapped/audited dependencies (`DOTNET-DEP-001/002/003/004`), public compatibility and versioning (`DOTNET-API-001/003`), test levels and coverage (`DOTNET-TEST-001`), security and disclosure (`DOTNET-SEC-001/002`), structured diagnostics (`DOTNET-OBS-001/002`) and bounded legacy adoption (`DOTNET-GOV-005`). This is not a whole-project compliance verdict.

The Development Plan Creation Guide 2.7, dated 2026-09-27, supplies requirement traceability, decision/data gates and future Test-before-Implement packets. The library's agent-tooling and Technical Markdown Style Guide supply tool routing and document conventions. Source inspection used filesystem reads and `rg`; no language-server activation or SDK operation was required.

Evidence labels in this proposal distinguish three things:

- **Confirmed source:** a repository shape, published package record or documented AWS behavior inspected on 2026-10-03.
- **Proposed contract:** IQToolkit semantics and names offered for owner review.
- **Unverified:** actual restore/runtime behavior, a selected workload, approved inputs, AWS certification or owner acceptance.

## Existing repository seams

| Confirmed source | Constraint on the proposal |
| :--- | :--- |
| [IEntityProvider and IEntityTable](../IQToolkit/IEntityProvider.cs) | Existing `IQueryable` and synchronous entity contracts remain unchanged. New abstract members would break external implementations. |
| [QueryExecutor](../IQToolkit.Data/Common/QueryExecutor.cs) and [DbEntityProvider](../IQToolkit.Data/DbEntityProvider.cs) | SQL commands, ADO.NET readers and connection/transaction ownership are not DynamoDB HTTP execution contracts. |
| [QueryMapping](../IQToolkit.Data/Common/Mapping/QueryMapping.cs) and [ColumnAttribute](../IQToolkit.Data/Mapping/ColumnAttribute.cs) | Existing primary-key metadata lacks partition/sort/index roles and document-presence semantics. |
| [BasicMapping.GetPrimaryKeyQuery](../IQToolkit.Data/Common/Mapping/BasicMapping.cs) | A relational primary-key predicate is not a partition-query or index contract. |
| [TypeHelper.Convert](../IQToolkit/TypeHelper.cs) | Existing null conversion returns CLR defaults. Reusing it would collapse missing attributes or explicit document nulls into defaults. |
| [Directory.Packages.props](../Directory.Packages.props) | No AWS SDK dependency is currently selected. |

The recommended boundary is an optional, proposed `IQToolkit.Data.DynamoDB` package with provider-local SDK-free keyed signatures. Extract a shared abstraction only when an approved consumer demonstrates a useful common contract. Native `IAmazonDynamoDB`, request/response and `AttributeValue` types remain available at an explicit adapter boundary; they do not become mandatory core dependencies. The adapter adds validated access patterns, mapping, budgets, policies and diagnostics rather than merely renaming SDK calls. AWS distinguishes low-level, document and object-persistence programming models; this proposal favors the low-level boundary to retain explicit service semantics. [AWS programming models](https://docs.aws.amazon.com/sdk-for-net/v4/developer-guide/dynamodb-intro.html)

## Proposed public surface

All names in this section are proposed IQToolkit types, not AWS SDK types or existing interfaces. One-shot operations use `Task`; traversal exposes pages through `IAsyncEnumerable`. A narrower first implementation can select a subset without removing the remaining required M3 inventory.

```csharp
public interface IKeyedStore<T>
{
    Task<KeyedRead<T>> GetAsync(
        KeyedGetRequest request, CancellationToken cancellationToken);
    Task<KeyedWriteResult> PutAsync(
        KeyedPutRequest<T> request, CancellationToken cancellationToken);
    Task<KeyedWriteResult> UpdateAsync(
        KeyedUpdateRequest request, CancellationToken cancellationToken);
    Task<KeyedWriteResult> DeleteAsync(
        KeyedDeleteRequest request, CancellationToken cancellationToken);
    Task<KeyedPage<T>> QueryPageAsync(
        PartitionQuery request, CancellationToken cancellationToken);
    IAsyncEnumerable<KeyedPage<T>> QueryPagesAsync(
        PartitionTraversal request, CancellationToken cancellationToken);
    Task<KeyedPage<T>> ScanPageAsync(
        ExplicitScan request, CancellationToken cancellationToken);
}
```

| Proposed contract element | Required meaning |
| :--- | :--- |
| Mapping | Explicit configured table and optional named index; key attribute names/types/roles, index kind/projections and converter identity. No arbitrary table selection from an untrusted continuation. |
| Item key | Typed String, Number or Binary partition value, plus the complete required sort value for item operations. Attribute names come from the mapping. |
| Read options | Explicit consistency and optional consumed-capacity reporting. Full entity reads and declared projection mappings remain distinct. |
| `KeyedRead<T>` | Found/not found is distinct from attribute presence and CLR null. It does not return a default entity to represent absence. |
| Write request | Explicit replace/create/update/delete behavior and optional supported condition. A proposed version condition carries an expected value without creating a hidden read-modify-write loop. |
| `KeyedWriteResult` | Confirmed application or confirmed condition failure. No affected-row count fabricated from a service success response. |
| `KeyedPage<T>` | Non-null items, opaque continuation, returned/evaluated counts, optional observed capacity, cumulative traversal progress and stop reason. |
| Operation context | Cancellation/deadline, bounded settings, diagnostics and declared policy scope; align with D1 only after its contract is accepted. |
| `ExplicitScan` | Separate affirmative scan opt-in, projection/filter and finite request/evaluation/deadline bounds. |

Caller-supplied requests are validated and snapshotted before dispatch. Mutable documents/binary values must not be modified concurrently unless a selected converter explicitly provides copying semantics. A shallow collection snapshot is not a deep-immutability guarantee.

## Key conditions, filters and index capabilities

The proposed first slice supports classic schemas with one partition attribute and optional one sort attribute. Require partition equality. Permit sort `=`, `<`, `<=`, `>`, `>=`, inclusive `BETWEEN` and string/binary `begins_with`; numeric prefixes reject. Compile attribute names and values through placeholders. Unsupported expressions reject before dispatch rather than becoming scans or hidden client evaluation. [AWS key conditions](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Query.KeyConditionExpressions.html), [Query API](https://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_Query.html)

Query filters are explicit post-read predicates that cannot contain the partition/sort key attributes of the selected table or index; those belong in that Query's key condition. This restriction does not apply to Scan: its filter may include key attributes, within the separately approved bounded filter grammar. The proposed first grammar includes comparisons, existence/type tests and conjunction; additional operators require selection and evidence. Filters do not reduce the capacity consumed by reading candidate items. A projection is not a promise of cheaper table reads. Query previews expose operator/mapping shapes and parameter slots, never values. [Query filter expressions](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Query.FilterExpression.html), [Scan API](https://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_Scan.html)

Reject strong reads on global secondary indexes without downgrading. Tables and local secondary indexes support strong reads, but a traversal does not thereby obtain a multi-page snapshot. Sort order follows the service's numeric, UTF-8 string or unsigned-binary rules, not CLR culture. [Read consistency](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/HowItWorks.ReadConsistency.html), [Query ordering](https://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_Query.html)

GSI queries use the index's own keys and projected attributes. They cannot fetch unprojected attributes from the table implicitly; reject an unavailable projection or select a separately visible, budgeted hydration operation later. GSI key values can be nonunique, so tied index-key rows do not have a promised deterministic secondary order. Missing index-key attributes can exclude an item from the index. Item reads/writes address the base table; `GetItem` is not an index lookup. [GSI behavior](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/GSI.html)

Current AWS documentation also describes multi-attribute GSI keys: up to four partition attributes and four sort attributes, all partition equalities, and left-to-right sort constraints. The classic first-slice boundary is an IQToolkit proposal, not a universal service restriction. Validate configured/approved schema metadata, reject unsupported multi-attribute index shapes, and record support as a possible later selection. Newer vector-index features, DAX and global-table replication guarantees are likewise outside this keyed first slice. [Multi-attribute GSI keys](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/GSI.html#GSI.MultiAttributeKeys)

## Mapping and document presence

Preserve absent attribute, explicit `NULL` and value as distinct states. Proposed presence-aware fields can express all three; ordinary CLR null cannot. Missing required constructor/member values and incompatible nulls produce mapping errors rather than default objects. Unselected projection fields are not evidence that stored attributes are missing. Typed partial reads use a declared projection mapping; reject construction of a complete entity whose required fields were not selected.

Select converters explicitly for strings, Boolean, bounded CLR integers/decimal, binary, ordered lists, unordered sets and maps. Reject overflow/precision loss, invalid keys and empty sets. Preserve empty non-key strings/binary and empty lists/maps. DynamoDB numbers support up to 38 digits, so a supported CLR numeric subset is not complete service-number support. Date/time needs an explicit UTC-string or numeric-epoch wire contract; no native date type exists. Unknown attributes, constructor selection, schema evolution, floating-point support and round-trip guarantees require recorded mapping decisions. [Service types](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/HowItWorks.NamingRulesDataTypes.html)

AWS SDK V4 collections default to null and request/response value properties are nullable. Handle those states locally, including absent capacity information and empty/absent continuation maps. Do not change global `AWSConfigs.InitializeCollections` to conceal the distinction. Typed item reads should request identity fields when projecting so response normalization cannot confuse an existing projected item with absence. [V4 migration](https://docs.aws.amazon.com/sdk-for-net/v4/developer-guide/net-dg-v4.html), [GetItem](https://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_GetItem.html)

The M1q mapping gate selects record/immutable constructor support, nullable metadata, explicit converters, modern date/time, JSON/document and array/list/set slices for the consuming workload, or records a reason for each deferral. Tenant and soft-delete policies remain explicit selections. A filter is not a substitute for a tenant key condition or authorization boundary. Validate the selected index and continuation against the policy scope; any bypass operation must be separately named and tested before support is claimed.

The M1q query gate likewise records selection or deferral for cursor pagination, parameterized native escape hatches, query previews/capability diagnostics, optimistic concurrency and partial-failure batching/bulk operations. The explicit native SDK boundary is the proposed escape hatch; PartiQL or a new raw-expression language is not implied. DynamoDB does not inherit relational keyset, aggregate, join or transaction guarantees merely because those features appear elsewhere in the shared inventory.

## Conditional writes, retries and uncertain outcomes

Put replaces the complete existing item. Expose create-only and conditional replacement distinctly. The recommended bounded update surface initially supports explicit `SET`/`REMOVE`, with an existence condition for update-only behavior. Removing an attribute differs from setting `NULL`; allowing update to create a missing item requires an explicit option. Arithmetic, list append and set mutations are later selected update slices. [PutItem](https://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_PutItem.html), [UpdateItem](https://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_UpdateItem.html), [Update expressions](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Expressions.UpdateExpressions.html)

Confirmed condition failure becomes an explicit conflict result and is not automatically retried by IQToolkit. A successful delete alone does not prove that an item existed. Transport failure, deadline or caller cancellation after dispatch does not prove that a write failed or rolled back. Preserve an uncertain outcome on the typed failure boundary rather than returning conflict, success or not-found without evidence. Conditional writes alone are not a universal idempotency token: retrying a successfully committed but unacknowledged conditional write can produce a later condition failure. [DeleteItem](https://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_DeleteItem.html), [Write uncertainty and retries](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Programming.Errors.html)

The AWS SDK is the sole owner of transport retries in the proposed initial adapter. IQToolkit does not add an outer replay loop. Explicit finite retry configuration is required; the current .NET guide documents ten DynamoDB retries by default, including Standard mode. For writes without a reviewed replay/idempotency strategy, recommend a no-retry write-client configuration. Exact settings and the classification of post-retry conflicts remain owner-reviewed contract choices, then exact-version probe obligations. Never silently accept an injected client's unknown retry policy as satisfying a bounded-attempt claim. [SDK retries and timeouts](https://docs.aws.amazon.com/sdk-for-net/v4/developer-guide/retries-timeouts.html)

Expose validation, unsupported operation, condition conflict, authentication/authorization, throttling, caller cancellation, deadline, mapping failure, transport failure and uncertain write outcome separately. Preserve useful native status/code/request identity behind the explicit native boundary. Default logs use sanitized exception objects and safe structured fields; arbitrary SDK exception text or returned condition-failure documents are not presumed safe to export.

## Pages, continuation and budgets

`QueryPageAsync` performs one logical SDK invocation, which may contain SDK transport retries. Preserve an empty filtered page and its continuation. Traversal continues until continuation is absent/empty, the requested item target is reached, or a declared budget stops it. A nonempty continuation does not prove more matching items remain. Do not treat an empty item list as completion. [Query pagination](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Query.Pagination.html)

The service's `Limit` counts evaluated items, not returned matches. Before each traversal invocation, cap it by the remaining evaluation allowance, remaining returned-item target and selected per-call allowance. Never discard already-returned items while keeping a service cursor that has advanced past them. Returning whole bounded pages avoids that data-loss trap. Fetch lazily, without prefetch; cancellation and early disposal prevent later calls. [Query limits](https://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_Query.html)

| Proposed bound | Meaning and limitation |
| :--- | :--- |
| Logical SDK calls | Finite maximum invocations by this traversal. Separate from transport attempts. SDK retries must also be finite and disclosed. |
| Evaluated items | Finite allowance consumed by returned service `ScannedCount` values, enforced before the next call with `Limit`. Repeated evaluation by transport retries is not hidden inside a claimed hard total. |
| Returned-item target | Maximum emitted items; reaching it is distinct from service exhaustion. Retain continuation when present. |
| Deadline | Linked cancellation for the complete traversal and its calls. Distinguish caller cancellation from deadline expiration. |
| Observed capacity | Optional threshold checked after responses. Require capacity reporting when using it. It can overshoot on the last response and cannot establish a hard monetary cap across retries/failures. |
| Scan allowance | Explicit opt-in plus finite call/evaluation/deadline bounds. No implicit parallel scan; no snapshot guarantee. |

Pages report progress and whether traversal continues or stops because of service exhaustion, item target or a specific budget. A budget stop with continuation is not exhaustion. Cancellation/deadline throws a distinct failure rather than returning a normal empty page. Missing required accounting fields fail closed for the accounting claim; a missing capacity value is not zero. External AWS account/resource cost controls remain necessary for paid certification.

Continuation retains the complete typed `LastEvaluatedKey`, including base/index keys where returned. Bind token version, mapping/index, query/filter/projection, direction, consistency and tenant scope. Reject mismatched or malformed continuations before dispatch. Bound token size and parsing work. Revalidate policy on every resume; a token is not an authorization grant. A changed table/index mapping invalidates the continuation.

Choose a protected portable cursor or an explicitly process-local opaque handle. Base64 alone provides no confidentiality. Portable cursors require an approved BCL/standard protection mechanism, key ownership/rotation and expiration policy; do not invent cryptography. Continuation values, fingerprints containing key values and cursor-protection keys never appear in logs. Resumption observes current service state, not a stable cross-page snapshot.

## Follow-on scope retained

These remain `LOCAL-RM-LATER` work, not automatic additions to the first adapter.

| Slice | Required separate contract |
| :--- | :--- |
| Batch get | Up to 100 keys/16 MB per request; partial responses and `UnprocessedKeys`; unordered results; missing items distinct from unprocessed keys. Include identity keys in projections. Retry only unprocessed work with bounded delay/attempt accounting. [BatchGetItem](https://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_BatchGetItem.html) |
| Batch write | Up to 25 puts/deletes/16 MB; no update or per-item conditions; the batch is not atomic. Expose processed, unprocessed and uncertain outcomes. Duplicate targets and size violations reject; explicit unprocessed resubmission must not multiply SDK retry policies. [BatchWriteItem](https://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_BatchWriteItem.html) |
| Atomic transactions | Separate API, up to 100 actions/4 MB, same account/region; no duplicate targeted item in a write transaction. Preserve cancellation/failure reasons. `ClientRequestToken` has a ten-minute window; changed parameters with the same token reject. Do not emulate these guarantees through batches or relational transaction scopes. [TransactWriteItems](https://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_TransactWriteItems.html), [TransactGetItems](https://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_TransactGetItems.html) |
| TTL metadata | Explicit conversion and asynchronous-expiration/visibility semantics, separately selected and verified. TTL does not itself supply a soft-delete isolation policy. |
| Change streams and synchronization | Selected checkpoint/replay/idempotency, deletion propagation, lag and rebuild contracts. No cross-store distributed-transaction promise. |
| Extended model support | Multi-attribute GSI keys, richer filters/updates and additional converters require demand, exact SDK/API evidence and their own bounded packets. |

## Credentials, diagnostics and certification

Use the AWS SDK credential chain or an explicitly injected client; prefer short-lived workload identity. Configure region, allowed endpoint and owned-versus-borrowed client lifetime explicitly. SDK credentials do not belong in mappings. An injected client remains caller-owned; do not dispose it after an operation or iterator. Adapter-owned clients receive a separately documented lifetime. Shared-client safety does not permit shared mutable requests, cursors or operation budgets. [Credential resolution](https://docs.aws.amazon.com/sdk-for-net/v4/developer-guide/creds-assign.html)

Structured diagnostics may expose operation family, a bounded configured resource alias, duration, returned/evaluated counts, observed capacity and error category. Exclude keys, documents, condition values, continuations, credential/profile contents, authorization headers and raw payload-bearing errors. Request IDs belong only in restricted diagnostics, not metrics labels. D1's approved activity/metric/error conventions should be reused later without importing a hosting or vendor telemetry dependency into the core.

DynamoDB Local is development evidence, not AWS certification. It ignores provisioned throughput, often appears strongly consistent, calculates index page sizes differently and does not throw transaction-conflict exceptions. Its documentation's mocking recommendation does not authorize fabricated inputs under this repository's data policy. A protocol-compatible alternative requires its own named support evidence. [DynamoDB Local differences](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/DynamoDBLocal.UsageNotes.html)

Future AWS certification requires a declared account/region/resource namespace, identity and least privilege, approved data/captures, request/cost limits, time bound and cleanup proof before execution. No service operations are authorized by saving this proposal. Record local/core checks separately from AWS-specific checks and report unavailable checks as unverified or skipped, never passed. Cancellation, consistency, throttling/retry/cost behavior and conflicts require appropriate live evidence; SDK metadata cannot prove them.

## SDK metadata candidate

As inspected on 2026-10-03, published 2026-09-30 metadata identifies `AWSSDK.DynamoDBv2` 4.0.107 and `AWSSDK.Core` 4.0.102.8. They are candidates for a future root-owned probe, not adopted dependencies. Both declare Apache-2.0. The DynamoDB package requires Core `[4.0.102.8,5.0.0)`; Core's `net8.0` dependency group is empty. NuGet lists net8 assets rather than explicit net10 assets. Actual net10 restore/assets/API/runtime qualification remains unverified. Catalog repository commits are absent; no exact package source commit is claimed. [DynamoDB catalog](https://api.nuget.org/v3/catalog0/data/2026.09.30.19.33.26/awssdk.dynamodbv2.4.0.107.json), [Core catalog](https://api.nuget.org/v3/catalog0/data/2026.09.30.19.35.25/awssdk.core.4.0.102.8.json), [Published framework assets](https://www.nuget.org/packages/AWSSDK.DynamoDBv2/4.0.107)

The exact official catalog JSON bodies were fetched in memory, without package binaries or SDK execution. These identities let root retain and verify the source records independently; they are not a claim that this document-only worker created an evidence file. Both catalogs' `packageEntries` declare `lib/net8.0` DLL/XML paths, which establishes advertised asset presence rather than runtime success.

| Catalog body | Bytes | Raw-body SHA256 |
| :--- | ---: | :--- |
| `awssdk.dynamodbv2.4.0.107.json` | 8052 | `a66547c7a5ddaa74105d39b51a788b6d2f96c396ec32c2e50253ddc546bc0305` |
| `awssdk.core.4.0.102.8.json` | 9241 | `91d96a0393608cd5042d92b9b34cf04332a478602615306110737398a78aa145` |

Published catalog SHA512 values, Base64, are metadata declarations rather than downloaded-archive verification:

```text
AWSSDK.DynamoDBv2/4.0.107
gFhFlu3jbmO38fIGK78H+O576hAmOIL1sigjfKR9gjkDYMQORN7B9ABzcAjKd4r3sO8hM76jGYCBICLWledsAw==

AWSSDK.Core/4.0.102.8
GywBolaWAVaQmdy2CIyvs6WVDWcxI6ZWbX9CZ5gAz5jALYhnB4yhf53fcHln50fLQnLfJmuHLNBOWHGKQjX2Mw==
```

Before any future restore, root records approved exact pins, the full target-profile graph, feed mapping, vulnerabilities/licenses and source/archive identity expectations. Prepare/review locks explicitly outside locked mode; verify an unchanged cold locked restore and strict no-restore build in the same profile. No unlocked recovery after a locked failure. An isolated probe must not alter production dependencies or retained evidence. Exact-version source and live behavior are required before genuine async/cancellation support is asserted; the documented async API alone is insufficient. [QueryAsync API](https://docs.aws.amazon.com/sdkfornet/v4/apidocs/items/DynamoDBv2/MDynamoDBQueryAsyncQueryRequestCancellationToken.html)

## Decisions and execution gates

Record each owner decision once in the integrated plan; do not repeat an unanswered user question or infer an answer from this proposal.

| Decision | Proposed starting point | Still required |
| :--- | :--- | :--- |
| Workload and public/package boundary | Optional provider-local SDK-free keyed API; preserve 1.x SQL contracts. | Actual consuming access patterns, public names and package placement. |
| Initial schema scope | Classic keys; explicit table/LSI/GSI mappings. | Accepted limits and multi-attribute/index deferrals. |
| Mapping | Presence-aware documents and explicit converters/projection construction. | Number/date/schema-evolution policy and each M1q selection/deferral. |
| Policies | Explicit tenant/soft-delete scope and named bypass if selected. | Workload isolation rules and authorized evidence. |
| Pagination and budgets | Whole service pages, finite logical-call/evaluation/deadline bounds. | Numeric defaults, exhaustion/error semantics, cursor protection/expiry and resumption budget policy. |
| Writes and retries | Explicit conflicts; SDK-owned finite retries; uncertain outcomes preserved. | Replay/idempotency rules and exact client retry configuration. |
| Data, local and AWS environments | Approved real data/captures; separate local versus AWS evidence. | Data authorization, local version/platform, AWS resource/cost/cleanup packet. |
| Coverage and support | Protect existing measurements; experimental status until accepted evidence. | Coverage targets, maintainer, supported SDK/OS matrix, promotion and runnable examples. |

The current dispatch horizon is D3 document/source review only. Future packets proceed in dependency order: contract/decision/data gates, root-owned SDK/schema probe, keyed read/query, conditional writes, bounded explicit scan, then selected follow-ons. All initial M3 capabilities remain in the inventory even when split across packets. Provider slices select or explicitly defer each M1q mapping/query item; batches, transactions, TTL and streams are retained follow-ons. Other roadmap milestones remain unchanged.

Before behavioral dispatch, expand exact source ownership, operation facts, Test-before-Implement dependencies, approved inputs, protecting/API/package checks, literal commands, evidence identities and rollback notes. No future command is represented as runnable against a nonexistent project. The existing integrated gates remain root-owned and are necessary protection, not DynamoDB certification.

## Design completion criteria

D3 is ready for integration when the saved document is read back, local links and fences are checked, repository and official-source assertions are reviewed, all pending decisions remain explicit and root obtains independent clarity/source review against protected input identities. Document readiness is not M3 implementation or owner acceptance.

Future M3 closure requires direct evidence for keyed access, supported key/filter/projection semantics, missing/null/converters, conditional conflicts, index consistency rejection, empty-page continuation, budgets, cancellation/lifetime, no implicit scans, default redaction and independent package consumption. AWS-specific evidence uses the declared authorized environment; platform/support exclusions and unavailable suites remain explicit. Selected mapping/query slices and follow-on outcomes retain their own facts and acceptance evidence.

## GOTCHA spec: D3 design packet

### Goals

Produce a reviewable DynamoDB contract that makes the required M3 behaviors executable after their actual gates resolve.

### Objectives

- Ground repository seams and service constraints in inspected sources.
- Specify concrete proposed operations, mapping, error, cursor, budget and ownership semantics.
- Preserve the complete M3 inventory, M1q selections/deferrals and DynamoDB follow-ons without claiming approval or runtime support.

### Tasks

- Trigger: independent source/design groundwork authorized while production and owner/data/environment gates remain open.
- Termination: saved/read-back proposal, scope/link/fence checks, root integration and independent source/clarity review. Runtime capability closure is outside D3.

### Capabilities

- Tools: filesystem source reads, `rg`, official documentation/catalog metadata and the exclusive new-document write scope.
- Off-limits: SDK/build/restore/test, implementation or public API changes, dependency installs, generated fixtures, binary downloads, credentials, services, paid requests and resource creation.

### Health

- Time/token/cost budget: none supplied; no paid service work.
- Retry limit: the integrated plan's three evidence-driven corrections; retain a source-access gap rather than inventing evidence.
- Monitoring: actual changed path, source references, saved-file checks and explicit unverified outcomes.

### Attributes

- Always distinguish confirmed source, proposed contract and unverified runtime/owner decisions.
- Never infer scans, cost guarantees, rollback, idempotency, async support or AWS certification from an API name or successful local response.

### Constraints

- Only this new document is agent-owned; root owns the integrated plan, state, SDK and later packets.
- M1 and accepted keyed contracts gate production M3; no generated-data or coverage permission is implied.
- Provider authors and consuming .NET applications need explicit cost/consistency and compatible existing SQL contracts.

### Users

Project owner, provider implementers, reviewers and consumers selecting a DynamoDB access-pattern workload.

### Runtime

- Working state: read-only repository/standard/source metadata inspection.
- Persisted state: this proposal; root-owned plan/evidence records are updated separately.
- Decision loop: correct concrete source/clarity errors, replan on incompatible actual schemas/APIs, and keep unresolved authority/data/environment gates explicit.

### Beliefs and intentions

The repository has relational synchronous seams but no adopted DynamoDB SDK or keyed API. AWS documents useful keyed operations and significant consistency/paging/retry constraints. This packet prepares a bounded proposal; it does not convert those observations into implemented capability.
