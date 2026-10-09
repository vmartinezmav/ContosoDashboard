# Phase 0 Research: Document Upload and Management

## Unknown: Local/Offline-Capable Malware Scanning Mechanism

**Context**: FR-006 requires every uploaded file to be scanned for malware using a
real, local/offline-capable mechanism (no external/cloud scanning service), per the
clarification resolved during `/speckit.specify`. No malware-scanning capability
exists anywhere else in this codebase today.

### Decision

Use the **Windows Defender command-line scanner (`MpCmdRun.exe`)**, invoked as a
local process by `DocumentService` (or a small `IMalwareScanner` helper it depends
on) against the temporary saved file, before the file is treated as final/committed
and before its database record is created.

- `MpCmdRun.exe -Scan -ScanType 3 -File <path>` performs an on-demand scan of a
  single file using signatures already present on the machine — no network call,
  no new installation, no new service/daemon required.
- A non-zero/"threat found" result causes the upload to be rejected (file deleted,
  no document record created, failure logged to the Document Activity Log).
- On non-Windows developer machines (where `MpCmdRun.exe` is unavailable), the
  scan step logs a clear, visible "malware scan skipped — Defender unavailable on
  this platform" warning and allows the upload to proceed. This is a documented,
  training-only limitation consistent with Constitution Principle I
  (Training-First Transparency) rather than a silently-skipped security control.

### Rationale

- **No new install burden**: the project (and this session) already hit real
  friction installing SQL Server Express for an unrelated reason; introducing
  another required local service (like a ClamAV daemon) would repeat that pain
  and add setup steps to every student's machine.
- **Genuinely local/offline**: signature-based scanning against the OS's existing
  antivirus engine performs no network calls, satisfying the offline-first
  constraint (Constitution Principle II) and the explicit "real scan, not a stub"
  choice made during clarification.
- **Matches training platform**: the documented dev prerequisites (Visual Studio
  2022 / VS Code on Windows) make Windows Defender a reasonable default for most
  students, while the non-Windows fallback keeps the app runnable everywhere else
  (consistent with "must remain fully offline/runnable" and not blocking other
  platforms).

### Alternatives Considered

| Alternative | Rejected because |
|---|---|
| ClamAV + an HTTP/socket client (e.g., `nClam`) | Requires installing and running a separate local daemon — repeats the exact kind of extra-install friction already experienced installing SQL Server Express this session; heavier than needed for a training app |
| Cloud-based scanning API (e.g., a hosted AV/threat-intel service) | Directly violates the offline-first, no-external-service constraint (Constitution Principle II) and the clarified requirement that scanning not depend on an external/cloud service |
| Stub/no-op scan that always "passes" | Explicitly rejected by the user during `/speckit.specify` clarification — option (a), a real scan, was chosen over the stub/mock option (b) |
| Async background job (queue + worker) as the *default* mechanism | Would make scan results unavailable until after a background process runs, meaning the document would need to exist in an unscanned "Pending" state before FR-006's reject-before-store guarantee could be honored — this changes spec-level behavior (FR-002/FR-006 currently require rejection *before* storage) and isn't needed to hit the 30s upload budget (SC-002) for a single synchronous local scan call |

### Cloud Migration Path (documented, not built): Async Scanning via Azure Functions

Per Constitution Principle II, infrastructure-facing functionality must be designed
so a cloud implementation can later replace the local default via configuration/DI,
without UI or schema changes. If this application were deployed to production with
a heavier/slower scanning engine (where synchronous in-request scanning is no
longer practical), the documented migration path is:

1. Define scanning behind an abstraction (e.g. `IMalwareScanner.ScanAsync(stream)`)
   exactly as planned — the training implementation calls it synchronously inline
   during upload (`MpCmdRun.exe`, see Decision above).
2. **Production swap**: `DocumentService` instead publishes a message (document
   id + storage path) to an **Azure Queue Storage** queue after the file is saved
   to Azure Blob Storage (itself the already-documented `AzureBlobStorageService`
   swap for `IFileStorageService`).
3. An **Azure Function with a Queue Storage trigger** dequeues the message,
   downloads the blob, runs the production-grade scan (e.g., a managed AV/threat
   scanning service appropriate for the hosting environment), and writes the
   result back (e.g., updating a `ScanStatus` column via a small callback/API, or
   directly against the database if the Function has connectivity).
4. A document would need a `ScanStatus` field (`Pending` / `Clean` / `Rejected`)
   in this production configuration so the UI can reflect "scanning in progress"
   and so other users only see a document once `Clean`. **This field is not part
   of the current data model** (see data-model.md) because the training/offline
   default remains fully synchronous and never produces a `Pending` state — it
   would only be introduced if/when this async production path is actually
   adopted.
5. Swapping steps 2–4 in requires no change to `Pages/`, no change to the
   `IDocumentService` public surface beyond an already-async method signature,
   and no change to the core `Document`/`DocumentShare` schema beyond the added
   status column — consistent with the Offline-First with Cloud Migration Path
   principle.

This section is **informational/forward-looking only**: nothing in this feature's
implementation introduces Azure Functions, Queue Storage, or any queue/worker
infrastructure. Adopting it for real would be a separate future change and would
first require updating FR-002/FR-006 in spec.md, since it changes user-visible
upload behavior (immediate accept with pending scan state vs. synchronous
reject-before-store).

## Other Technical Context Items

No other `NEEDS CLARIFICATION` markers were present in the Technical Context —
language/runtime, storage, project structure, and performance targets were all
directly derivable from the existing codebase conventions and the feature
specification's Success Criteria / Technical Constraints, so no further research
tasks were required.
