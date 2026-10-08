---
title: Package symbol and Source Link provenance hardness
version: 0.1
last_updated: 2026-10-03
status: Scoped verification; integrated Oracle CLEAR
owner: IQToolkit project owner
tags: [modernization, packaging, symbols, verification]
---

# ATLAS hardness report: package symbol provenance

V1s strengthens the roadmap's existing source-link/symbol package requirement. [verify-symbols.sh](../eng/verify-symbols.sh) builds the existing framework-only [verifier](../eng/IndependentConsumers/Verifier/Program.cs) outside the checkout and checks the actual six package/symbol archive pairs. No library, dependency, template, reviewed lock or public API changes.

## Contract and observed evidence

Acquisition `artifacts/package-symbols/acquisition-runs/run.zv1h2_qi` captures the six actual nuspec records, DLL/PDB hashes, portable CodeView/PDB identities, checksums and Source Link records. Source/feed hashes are unchanged and the acquisition project/source are retained under nondiscoverable names. Acquisition Oracle CLEAR precedes implementation.

The gate requires GUID plus stamp equality, then SHA256 of the portable PDB with its actual twenty-byte ID zeroed in an in-memory copy. Original bytes remain unchanged. This follows the primary [PE/PDB specification](https://github.com/dotnet/runtime/blob/main/docs/design/specs/PE-COFF.md#portable-pdb-checksum). Exactly one portable CodeView record/Age1 and one SHA256 checksum are supported in the current SDK profile.

Both archive manifests must match the exact package ID, central version, Git repository and captured HEAD commit. A single module-level Source Link record must contain only the `documents` singleton `/_/*`, whose URL exactly names this project and the nuspec commit. Duplicate fields, alternative shapes or URLs reject. The record identity comes from the primary [portable-PDB specification](https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md#source-link-c-and-vb-compilers). Document names must use the normalized profile. Reports inventory names/checksums/embedding presence without printing source contents.

## Edge cases tested

- Qualified `artifacts/package-symbols/runs/run.t8FcjI18`: all six genuine pairs pass with zero-warning tooling build, seven valid JSONL records, unchanged checkout/source/feed inputs and exact copied archives. All six commit-bound Source Link records validate.
- Actual core nupkg paired with actual Data snupkg: exits1 with `Portable PDB GUID/stamp does not match DLL.` Identity is compared before package-name rejection, so this genuinely exercises the required identity check. No artifact is altered or fabricated.
- First wrapper `run.9pgaouUd`: six metadata checks pass, but aggregate fails because EXIT recapture runs outside Git. Source/environment/failure retained. Narrow correction explicitly anchors capture to the checkout and propagates hash errors; a new qualified run validates the correction. This is orchestration failure, not semantic RED of a production library.
- Malformed JSON, forged commits, mutated checksums, unsupported algorithms, cancelled processes and I/O failures have not been injected or qualified. Their rejection/cleanup code is reviewed but not claimed covered by the cross-pair case.

## Tool/service failure handling

Strict pipe failure propagation stops restore/build/metadata failure. No unlocked retry or package repair. The exit handler preserves failure status, checks original source/feed/revision, renames the owned project to `.csproj.snapshot`, and retains its environment. All SDK work is root-serialized. Only a successful retained run updates the success pointer.

## Concurrency and load

Unique temporary environments, cold framework-only caches and run directories isolate invocations. Parallel wrapper execution and hosted matrix timing remain unmeasured. Archives/PDBs are read into memory; the bounded scope is the six current project packages, not a large arbitrary artifact service. No provider runtime/concurrency guarantee follows from this gate.

## Security and compatibility

Git hashing avoids adding a GNU hashing utility requirement to the existing matrix; framework APIs compute archive/assembly/symbol cryptographic hashes. No new package feeds/dependencies or application data. Source Link URL/commit binding is exact, not substring matching. No embedded-source contents, credentials or endpoint values are printed. [CI](../.github/workflows/ci.yml) invokes the check after package inspection on every configured runner; only local LinuxARM64 execution is observed.

Current maps name release commit60ddef60 while the implementation tree is dirty. Matching metadata does not prove source retrieval, source-byte correctness, artifact-disclosure approval or official release certification. DOTNET-REPO-004's clean-checkout retrieval at a different absolute path and disclosure review remain separate requirements. This gate does not approve or publish artifacts.

## Verification and remaining work

`bash -n eng/verify-symbols.sh` passes. `bash eng/verify-symbols.sh artifacts/ci-consumers-input/packages` passes the six pairs after the recorded correction. `dotnet <qualified-verifier-dll> symbol-pair artifacts/ci-consumers-input/packages/IQToolkit.1.1.2-preview.1.nupkg artifacts/ci-consumers-input/packages/IQToolkit.Data.1.1.2-preview.1.snupkg IQToolkit.Data` returns the expected GUID/stamp rejection.

Fresh seven-consumer run.XMRmhZOP passes all seven hash-only lock reviews, six cold locked symbol compiles and the native SQLite consumer with current verifier/workflow inputs. Ordinary run.uVAtG07u passes77tests/325unconfigured SQLServer skips/247translations with0warnings and261stable in-run inputs. Integrated inspection independently matches six pair reports to acquisition, all136document names/checksums (embedding observed separately in the qualified output),138library/config inputs,12fresh/retained archive payloads and336current/V1a-identical binaries. Final integrated Oracle review is CLEAR for this checkpoint, including the six pair and 136 document comparisons, genuine mismatch, protecting gates and unchanged input/payload/binary identities. Earlier V1q consumer source-input qualification is historical after the deliberate verifier/workflow changes. Runtime coverage collection excludes this engineering tool; full coverage targets and existing owner/data/policy decisions remain open. Full M0/roadmap completion is not claimed.
