# Implementation Plan: Document Upload and Management

**Branch**: `001-document-upload-management` | **Date**: 2026-10-09 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-document-upload-management/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Add centralized document upload, organization, sharing, and audit capabilities to
ContosoDashboard. Employees upload files (PDF/Office/text/images, ≤25 MB) with
required title/category metadata; documents are searchable/sortable/filterable,
scoped to the uploader, their projects, their Department (for Team Leads), and
anyone they're explicitly shared with. The feature integrates with existing Tasks
and Dashboard pages and adds an Administrator audit/reporting view. Technical
approach: extend the existing single ASP.NET Core Blazor Server project using the
same layered conventions already in the codebase (`Models` → `Data` → `Services` →
`Pages`), add a local-filesystem-backed `IFileStorageService` (mirroring the
project's documented offline-first/cloud-migration abstraction pattern), and add
two small authenticated minimal-API endpoints for file download/preview streaming
(Blazor Server circuits can't stream raw file bytes directly, same reason Login/
Logout already use separate Razor Pages instead of Blazor components).

## Technical Context

**Language/Version**: C# (implicit usings, nullable enabled) on .NET 10 — matches
`ContosoDashboard.csproj` (`net10.0`)
**Primary Dependencies**: ASP.NET Core Blazor Server, EF Core 10
(`Microsoft.EntityFrameworkCore.SqlServer`), cookie-based auth (existing), Bootstrap
5.3 + Bootstrap Icons (existing UI toolkit); NEW: a local/offline malware-scan
mechanism (resolved in Phase 0 research below — synchronous local scan for this
feature; an Azure Functions + Queue Storage async production migration path is
documented, but intentionally not built, in [research.md](./research.md#cloud-migration-path-documented-not-built-async-scanning-via-azure-functions))
**Storage**: SQL Server (LocalDB or local SQL Server Express instance) via the
existing `ApplicationDbContext` (EF Core, currently using `EnsureCreated()`); local
filesystem for document file bytes, stored outside `wwwroot` (e.g.
`App_Data/uploads/{userId}/{projectId-or-"personal"}/{guid}.{ext}`)
**Testing**: No automated test project exists in this repo yet; this feature will
be validated manually against the scenarios in `quickstart.md` (consistent with
current repo practice — the constitution only requires tests to pass where they
already exist for the touched area, not that new test infrastructure be
introduced as a prerequisite)
**Target Platform**: Local developer machine running Kestrel (ASP.NET Core),
fully offline — training environment, not a hosted deployment
**Project Type**: Single project (Blazor Server monolith) — extends the existing
`ContosoDashboard/` project; no new projects/solutions created
**Performance Goals**: Upload completes ≤30s for files up to 25 MB; document list
pages load ≤2s for up to 500 documents; search returns ≤2s; preview loads ≤3s (per
spec Success Criteria SC-002–SC-005)
**Constraints**: Must remain fully offline/no required external or cloud service
(Constitution Principle II); `DocumentId` MUST be `int` (not GUID), `Category`
MUST be stored as text (not an integer enum), `FileType` column MUST accommodate
255 characters — all per stakeholder technical constraints; stored filenames MUST
be GUID-based (never user-supplied) and stored outside any web-accessible
directory (Constitution Principle III)
**Scale/Scope**: Training-app scale — a handful of concurrent users (4 seeded
accounts today), designed to the documented 500-documents-per-user performance
target; 7 user stories (P1–P7) covering upload, browse/search, lifecycle
management, project visibility, sharing, task/dashboard integration, and
Administrator audit

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|---|---|---|
| I. Training-First Transparency | Feature stays fully offline by default; the one new real external-ish dependency (malware scan) must be documented as a training-scoped choice, not hidden | **PASS** — Phase 0 research resolves this with a locally-available, no-new-install mechanism; documented in research.md |
| II. Offline-First with Cloud Migration Path | File storage MUST sit behind an interface abstraction with local default | **PASS** — `IFileStorageService` / `LocalFileStorageService` planned, mirroring the project's existing documented pattern; no cloud SDK introduced |
| III. Security by Design | Defense in depth, IDOR protection, safe file-path handling | **PASS** — plan requires `[Authorize]` page attributes + service-layer ownership/role checks on every operation, GUID-based filenames, files outside `wwwroot`, auth-checked download/preview endpoints |
| IV. Clean Separation of Concerns | New code follows Models/Data/Services/Pages layering | **PASS** — see Project Structure below; no business logic placed directly in Razor code-behind |
| V. Simplicity | No speculative abstractions | **PASS** — reuses existing per-request `ApplicationDbContext` injection pattern (no repository layer added); Tags stored as a simple delimited string rather than a new join table, consistent with training-scope simplicity |

No violations requiring justification — Complexity Tracking table is not needed.

## Project Structure

### Documentation (this feature)

```text
specs/001-document-upload-management/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   └── document-endpoints.md
├── checklists/
│   └── requirements.md  # Already created by /speckit.specify + /speckit.clarify
└── tasks.md              # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
ContosoDashboard/
├── Models/
│   ├── Document.cs                    # NEW — document metadata entity
│   ├── DocumentShare.cs               # NEW — user/department share record
│   └── DocumentActivityLogEntry.cs    # NEW — audit log entry
├── Data/
│   └── ApplicationDbContext.cs        # MODIFIED — add 3 new DbSets + relationships/indexes
├── Services/
│   ├── IFileStorageService.cs         # NEW — storage abstraction (per Constitution II)
│   ├── LocalFileStorageService.cs     # NEW — offline/local implementation
│   ├── IDocumentService.cs            # NEW — upload/search/share/authorize/audit logic
│   ├── DocumentService.cs             # NEW
│   └── NotificationService.cs         # UNCHANGED interface — DocumentService calls its
│                                       # existing CreateNotificationAsync for share/new-doc alerts
├── Pages/
│   ├── Documents.razor                # NEW — "My Documents" list, search/filter/sort, upload
│   ├── ProjectDetails.razor           # MODIFIED — add Project Documents section/tab
│   ├── Tasks.razor                    # MODIFIED — attach/view documents from task detail view
│   ├── Index.razor                    # MODIFIED — "Recent Documents" widget + doc count card
│   └── Admin/DocumentReports.razor    # NEW — Administrator audit log + usage reports
├── Endpoints/
│   └── DocumentEndpoints.cs           # NEW — minimal-API extension method (download/preview),
│                                       # mapped from Program.cs, matching existing lightweight
│                                       # style (no new MVC Controllers folder/infrastructure)
└── Program.cs                         # MODIFIED — register new services + map document endpoints
```

**Structure Decision**: Single-project extension of the existing Blazor Server app
(Option 1 from the template, adapted to this repo's real layout). No
frontend/backend split and no additional projects are introduced — this matches
Constitution Principle IV (established layering) and Principle V (no unjustified
complexity).

## Complexity Tracking

> No Constitution Check violations were identified; this section is intentionally
> left empty.

