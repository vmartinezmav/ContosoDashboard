# Tasks: Document Upload and Management

**Input**: Design documents from `/specs/001-document-upload-management/`
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/document-endpoints.md](./contracts/document-endpoints.md), [quickstart.md](./quickstart.md)

**Tests**: Not included — no test project exists in this repo and tests were not
explicitly requested in the feature specification (see plan.md Technical Context).
Validation is performed manually via quickstart.md.

**Organization**: Tasks are grouped by user story (US1–US7, matching spec.md
priorities P1–P7) so each story can be implemented and verified independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on a sibling task in the same batch)
- **[Story]**: Maps the task to its user story (US1–US7) for traceability
- All file paths are relative to the repository root

---

## Phase 1: Setup

**Purpose**: Baseline configuration needed before any document-feature code is added

- [X] T001 Add a `DocumentStorage` configuration section (storage root path, e.g.
      `App_Data/uploads`; max file size `25 MB`; allowed MIME type whitelist: PDF,
      Word/Excel/PowerPoint Office MIME types, `text/plain`, `image/jpeg`,
      `image/png`) to `ContosoDashboard/appsettings.json` and
      `ContosoDashboard/appsettings.Development.json`
      — *Added to appsettings.json only; appsettings.Development.json has no
      overrides for this section today, consistent with its existing
      logging-only content.*
- [X] T002 [P] Run `dotnet build` in `ContosoDashboard/` to confirm the project
      builds cleanly before adding document-feature code

**Checkpoint**: Configuration in place, baseline build green.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared data model and services that every user story depends on,
consistent with the Clean Separation of Concerns principle (all document
operations go through `IDocumentService`, never directly from Razor pages)

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T003 [P] Create the `Document` entity in `ContosoDashboard/Models/Document.cs`
      with: `DocumentId` (`int`, PK, identity); `Title` (`string`, required, max
      255); `Description` (`string?`, optional, max 2000); `Category` (`string`,
      required, max 100 — stored as text, not an enum); `ProjectId` (`int?`, FK →
      Project); `Tags` (`string?`, max 500, comma-separated); `UploadedByUserId`
      (`int`, FK → User); `UploadDate` (`DateTime`, UTC); `FileSizeBytes`
      (`long`); `FileType` (`string`, max **255** to accommodate long Office MIME
      strings); `FilePath` (`string`, required, unique, max 500, GUID-based,
      never derived from the user-supplied filename); `OriginalFileName`
      (`string`, max 255); `TaskId` (`int?`, FK → TaskItem)
- [X] T004 [P] Create the `DocumentShare` entity in
      `ContosoDashboard/Models/DocumentShare.cs` with: `DocumentShareId` (`int`,
      PK, identity); `DocumentId` (`int`, FK → Document); `SharedByUserId`
      (`int`, FK → User); `SharedWithUserId` (`int?`, FK → User); `SharedWithDepartment`
      (`string?`, max 100); `SharedDate` (`DateTime`, UTC). Enforce the validation
      rule that exactly one of `SharedWithUserId` / `SharedWithDepartment` must be set
- [X] T005 [P] Create the `DocumentActivityLogEntry` entity in
      `ContosoDashboard/Models/DocumentActivityLogEntry.cs` with: `LogId`
      (`int`, PK, identity); `DocumentId` (`int`, stored as a plain value with
      **no enforced FK/navigation**, so the audit trail survives a hard-deleted
      document); `DocumentTitleSnapshot` (`string`, max 255); `ActorUserId`
      (`int`, FK → User); `ActionType` (`enum`: `Upload`, `Download`, `Delete`,
      `Share`); `Timestamp` (`DateTime`, UTC)
- [X] T006 Add `DbSet<Document>`, `DbSet<DocumentShare>`, and
      `DbSet<DocumentActivityLogEntry>` to
      `ContosoDashboard/Data/ApplicationDbContext.cs`, with: `Document` →
      `User` (`Restrict` on delete), `Document` → `Project` (`Restrict`,
      nullable), `Document` → `TaskItem` (`SetNull`, nullable); `DocumentShare` →
      `Document` (`Cascade` on delete); indexes on `Document.UploadedByUserId`,
      `Document.ProjectId`, `Document.Category`, `Document.UploadDate`,
      `DocumentShare.SharedWithUserId`, `DocumentShare.SharedWithDepartment`, and
      `DocumentActivityLogEntry.Timestamp` (depends on T003, T004, T005)
- [X] T007 [P] Create the `IFileStorageService` interface
      (`UploadAsync`, `DeleteAsync`, `DownloadAsync`, `Exists`) in
      `ContosoDashboard/Services/IFileStorageService.cs`, per the project's
      documented offline/cloud-migration abstraction pattern
      — *`GetUrlAsync` was dropped: FR-007 requires files only be retrievable
      through an authorization-checked path, never a direct URL.*
- [X] T008 Implement `LocalFileStorageService : IFileStorageService` in
      `ContosoDashboard/Services/LocalFileStorageService.cs` using
      `System.IO` operations, storing files under the configured
      `DocumentStorage` root at `{userId}/{projectId-or-"personal"}/{guid}.{ext}`,
      outside `wwwroot` (depends on T007)
- [X] T009 [P] Create the `IMalwareScanner` interface in
      `ContosoDashboard/Services/IMalwareScanner.cs`
      — *Signature changed to `ScanAsync(string filePath) : Task<ScanResult>`
      (`Clean`/`Flagged`/`ScanFailed`) rather than a `Stream`/`bool`: scanning
      happens against a temp file on disk (required by `MpCmdRun.exe`) and the
      three-way result lets `DocumentService` give a specific rejection
      message per FR-002, decoupled from `IFileStorageService`.*
- [ ] T010 Implement `WindowsDefenderMalwareScanner : IMalwareScanner` in
      `ContosoDashboard/Services/WindowsDefenderMalwareScanner.cs`, invoking
      `MpCmdRun.exe -Scan -ScanType 3 -File <path>` per the decision in
      research.md, with a clearly logged "scan skipped — Defender unavailable"
      warning-and-allow fallback on non-Windows platforms (depends on T009)
- [X] T011 Create the `IDocumentService` interface in
      `ContosoDashboard/Services/IDocumentService.cs`
      — *Scoped down for this MVP pass to only `UploadAsync` and
      `GetMyDocumentsAsync`, to avoid declaring members (download, share,
      project-documents, etc.) that would force speculative/stub
      implementations before their user stories (US3–US7) are built. The
      interface will grow incrementally as each later story is implemented —
      this is called out explicitly so the gap vs. the original task wording
      isn't mistaken for an oversight.*
- [ ] T012 Implement a shared authorization helper method in
      `ContosoDashboard/Services/DocumentService.cs` that resolves whether the
      current user may access a given document as: the uploader, a member of
      the document's project, the Project Manager of the document's project, a
      Team Lead sharing the uploader's Department, an Administrator, or a
      direct/department share recipient (FR-020, FR-023)
      — *Deferred to US3 (T020/T021), the first story that needs cross-user
      access checks; US1's two methods are both trivially owner-scoped. The
      `DocumentService` constructor/DI wiring itself (the other half of the
      original T012) is done.*
- [X] T013 Register `IFileStorageService` → `LocalFileStorageService`,
      `IMalwareScanner` → `WindowsDefenderMalwareScanner`, and
      `IDocumentService` → `DocumentService` in `ContosoDashboard/Program.cs`
      (depends on T007–T012)

**Checkpoint**: Foundation ready — user story implementation can now begin.

---

## Phase 3: User Story 1 - Upload a Document and See It in My Documents (Priority: P1) 🎯 MVP

**Goal**: An employee can upload a file with required metadata and see it in
their "My Documents" list.

**Independent Test**: Upload a single supported file with required metadata and
confirm it appears correctly in the uploader's "My Documents" list.

- [X] T014 [US1] Implement `DocumentService.UploadAsync` in
      `ContosoDashboard/Services/DocumentService.cs`: validate file size (≤ 25
      MB per FR-001/FR-002) and MIME type whitelist, call
      `IMalwareScanner.ScanAsync` and reject the upload before any file is
      persisted if flagged or if the scan cannot complete (FR-006), generate a
      GUID-based `FilePath` server-side before saving (FR-007), save via
      `IFileStorageService.UploadAsync`, create the `Document` record, and log
      an `Upload` activity entry (FR-021) (depends on T012)
- [X] T015 [US1] Implement `DocumentService.GetMyDocumentsAsync` in
      `ContosoDashboard/Services/DocumentService.cs`, returning the caller's
      documents with title, category, upload date, file size, and associated
      project (FR-008) (depends on T012)
- [X] T016 [P] [US1] Create `ContosoDashboard/Pages/Documents.razor` with
      `@page "/documents"`, `[Authorize]`, and a "My Documents" table bound to
      `IDocumentService.GetMyDocumentsAsync`
- [X] T017 [US1] Add an upload modal to
      `ContosoDashboard/Pages/Documents.razor`: file picker; required title and
      category (fixed list: Project Documents, Team Resources, Personal Files,
      Reports, Presentations, Other); optional description/project/tags;
      MemoryStream-copy pattern for the selected file per the stakeholder
      Blazor-specific notes; a progress indicator during upload; and a clear
      success/failure message, wired to `DocumentService.UploadAsync` with
      size/type rejection errors surfaced to the user (FR-002, FR-003)
      (depends on T014, T016)

**Checkpoint**: User Story 1 is fully functional and independently testable.

---

## Phase 4: User Story 2 - Search, Filter, and Sort My Documents (Priority: P2)

**Goal**: A user can search, filter, and sort their document list to find a
document quickly.

**Independent Test**: Seed several documents with varying titles, categories,
and dates; verify search, filter, and sort each return the expected results.

- [ ] T018 [US2] Extend `DocumentService.GetMyDocumentsAsync` in
      `ContosoDashboard/Services/DocumentService.cs` with keyword search across
      title/description/tags (FR-010), filters by category/project/date range
      (FR-009), and sort by title/upload date/category/file size (FR-009),
      returning only documents the caller is authorized to see within the
      2-second target (SC-004) (depends on T015)
- [ ] T019 [US2] Add a search box, category/project/date-range filter controls,
      and a sort-order dropdown to `ContosoDashboard/Pages/Documents.razor`,
      wired to the extended query (depends on T018, T016)

**Checkpoint**: User Stories 1–2 both work independently.

---

## Phase 5: User Story 3 - Download, Preview, Edit, and Delete a Document (Priority: P3)

**Goal**: A document owner can download, preview, edit, replace, or delete
their document.

**Independent Test**: As the uploader, download a document and verify it
matches the original; preview a PDF/image; edit its metadata; replace its
file; delete it and confirm it's gone everywhere.

- [ ] T020 Implement `DocumentService.DownloadAsync` and
      `DocumentService.PreviewAsync` in
      `ContosoDashboard/Services/DocumentService.cs`, using the shared
      authorization helper (FR-020) and logging a `Download` activity entry on
      download (FR-021); preview is limited to PDF/image file types (FR-013)
      (depends on T012)
- [ ] T021 Implement `DocumentService.EditMetadataAsync`,
      `DocumentService.ReplaceFileAsync`, and `DocumentService.DeleteAsync` in
      `ContosoDashboard/Services/DocumentService.cs`: edit/replace restricted
      to the document owner (FR-014); delete restricted to the owner or the
      managing Project Manager, permanently removing both the stored file and
      metadata record after confirmation (FR-015) (depends on T012)
- [ ] T022 Create `ContosoDashboard/Endpoints/DocumentEndpoints.cs` with a
      `MapDocumentEndpoints` extension exposing
      `GET /documents/{documentId:int}/download` and
      `GET /documents/{documentId:int}/preview`, requiring authentication and
      returning `200`/`403`/`404`/`415` per
      [contracts/document-endpoints.md](./contracts/document-endpoints.md)
      (depends on T020)
- [ ] T023 Map the document endpoints in `ContosoDashboard/Program.cs`
      (depends on T022)
- [ ] T024 Add a download link, in-browser preview (PDF/image via an
      `<embed>`/`<img>` pointing at the preview endpoint), an edit-metadata
      modal, a file-replace control, and a delete-with-confirmation action to
      `ContosoDashboard/Pages/Documents.razor` (depends on T020, T021, T016)

**Checkpoint**: User Stories 1–3 all work independently.

---

## Phase 6: User Story 4 - View and Manage Project Documents (Priority: P4)

**Goal**: Project team members see project documents; Project Managers and
Team Leads get elevated management access.

**Independent Test**: Upload a document associated with a project; verify
project members can view/download it; verify the Project Manager can manage
any document in the project; verify a Team Lead can manage documents uploaded
by anyone in their own Department.

- [ ] T025 Implement `DocumentService.GetProjectDocumentsAsync` in
      `ContosoDashboard/Services/DocumentService.cs` (FR-011), and extend the
      shared authorization helper so a Project Manager can edit/replace/delete
      any document in a project they manage regardless of uploader (FR-012),
      and a Team Lead can view/edit/delete any document uploaded by a user in
      their own Department regardless of project (FR-023) (depends on T012,
      T021)
- [ ] T026 Add a "Project Documents" section to
      `ContosoDashboard/Pages/ProjectDetails.razor` listing documents
      associated with the project, downloadable by all project members, with
      upload/manage actions shown for the Project Manager (depends on T025)

**Checkpoint**: User Stories 1–4 all work independently.

---

## Phase 7: User Story 5 - Share Documents and Get Notified (Priority: P5)

**Goal**: A document owner shares a document with a user or Department;
recipients are notified and can find it under "Shared with Me."

**Independent Test**: Share a document with a specific user; verify the
recipient is notified and sees it under "Shared with Me"; verify a
non-recipient does not.

- [ ] T027 Implement `DocumentService.ShareAsync` (target: a specific user or
      an entire Department, FR-016) and `DocumentService.GetSharedWithMeAsync`
      in `ContosoDashboard/Services/DocumentService.cs`, calling
      `INotificationService.CreateNotificationAsync` for each recipient and
      logging a `Share` activity entry (FR-021) (depends on T012)
- [ ] T028 Add a "Share" action/modal (choose a specific colleague or a
      Department) and a "Shared with Me" view to
      `ContosoDashboard/Pages/Documents.razor` (depends on T027, T016)

**Checkpoint**: User Stories 1–5 all work independently.

---

## Phase 8: User Story 6 - Attach Documents to Tasks and See Them on the Dashboard (Priority: P6)

**Goal**: Users attach documents from a task's detail view and see recent
documents/count on their dashboard.

**Independent Test**: Upload a document from a task's detail view and verify
it's associated with the task's project; verify the dashboard shows the 5 most
recent documents and an accurate count.

- [ ] T029 Implement `DocumentService.AttachToTaskAsync` in
      `ContosoDashboard/Services/DocumentService.cs`, setting `TaskId` and
      inheriting the task's `ProjectId` automatically (FR-017) (depends on
      T014)
- [ ] T030 Implement `DocumentService.GetRecentDocumentsAsync` (5 most recent
      for the caller) and `DocumentService.GetDocumentCountAsync` in
      `ContosoDashboard/Services/DocumentService.cs` (FR-018) (depends on T015)
- [ ] T031 [P] Add a document upload/attach control to the task detail view in
      `ContosoDashboard/Pages/Tasks.razor`, calling `AttachToTaskAsync`
      (depends on T029)
- [ ] T032 [P] Add a "Recent Documents" widget and a document-count summary
      card to `ContosoDashboard/Pages/Index.razor` (depends on T030)

**Checkpoint**: User Stories 1–6 all work independently.

---

## Phase 9: User Story 7 - Administrator Audit and Reporting (Priority: P7)

**Goal**: Administrators can review the document activity log and usage
reports.

**Independent Test**: Perform a mix of document actions as various users; as
an Administrator, verify the activity log and usage reports reflect them
accurately; verify a non-Administrator is denied access.

- [ ] T033 Implement `DocumentService.GetActivityLogAsync` and
      `DocumentService.GetUsageReportAsync` (most-uploaded document types,
      most active uploaders, document access patterns) in
      `ContosoDashboard/Services/DocumentService.cs`, restricted to
      Administrators (FR-022) (depends on T012)
- [ ] T034 Create `ContosoDashboard/Pages/Admin/DocumentReports.razor` with
      `@attribute [Authorize(Policy = "Administrator")]`, showing the activity
      log and usage report (depends on T033)

**Checkpoint**: All 7 user stories are independently functional.

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and cleanup across all stories

- [ ] T035 [P] Walk through every scenario in
      [quickstart.md](./quickstart.md) (US1–US7) end-to-end and record results
- [ ] T036 [P] Review all new `Services/` and `Pages/` files added by this
      feature for nullable-reference warnings and resolve any that this
      feature introduces (pre-existing warnings in `TaskService.cs` are out of
      scope)
- [ ] T037 Run `dotnet build` in `ContosoDashboard/` and confirm zero new
      errors or warnings
- [ ] T038 [P] Update the "Features" section of `README.md` to list document
      upload/management as an implemented feature

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories (T003–T013)
- **User Stories (Phase 3–9)**: All depend on Foundational completion
  - US1 (Phase 3) has no dependency on other stories — the MVP
  - US2 (Phase 4) depends on US1's `GetMyDocumentsAsync` (T015) and
    `Documents.razor` (T016) existing
  - US3 (Phase 5) depends on Foundational only for its service methods, but its
    UI task (T024) extends `Documents.razor` from US1 (T016)
  - US4 (Phase 6) depends on US3's authorization extension point (T021)
  - US5 (Phase 7) depends on US1's `Documents.razor` (T016) for its UI
  - US6 (Phase 8) depends on US1's `UploadAsync`/`GetMyDocumentsAsync` (T014, T015)
  - US7 (Phase 9) depends on Foundational only (T012)
- **Polish (Phase 10)**: Depends on all desired user stories being complete

### Within Each User Story

- Service-layer methods before the UI/endpoint tasks that call them
- Story complete and checkpointed before moving to the next priority (if
  working sequentially)

### Parallel Opportunities

- T003, T004, T005 (new, independent model files) in parallel
- T007 and T009 (new, independent interface files) in parallel
- T016 (new `Documents.razor`) in parallel with the Foundational service tasks
  it will eventually call
- T031 and T032 (different files, each depending on a different earlier task)
  in parallel
- T035, T036, T038 in the Polish phase (different concerns/files) in parallel
- Tasks that modify `ContosoDashboard/Services/DocumentService.cs` are
  intentionally **not** marked `[P]` relative to each other, even when
  logically independent, to avoid concurrent edits to the same file

---

## Parallel Example: Foundational Phase

```bash
# Launch independent new-file tasks together:
Task: "Create the Document entity in ContosoDashboard/Models/Document.cs"
Task: "Create the DocumentShare entity in ContosoDashboard/Models/DocumentShare.cs"
Task: "Create the DocumentActivityLogEntry entity in ContosoDashboard/Models/DocumentActivityLogEntry.cs"
Task: "Create the IFileStorageService interface in ContosoDashboard/Services/IFileStorageService.cs"
Task: "Create the IMalwareScanner interface in ContosoDashboard/Services/IMalwareScanner.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE** against quickstart.md's US1 section
5. Demo: upload a document, see it in "My Documents"

### Incremental Delivery

1. Setup + Foundational → foundation ready
2. US1 → validate → demo (MVP!)
3. US2 → validate → demo (search/filter/sort)
4. US3 → validate → demo (full document lifecycle)
5. US4 → validate → demo (project visibility + Team Lead/PM management)
6. US5 → validate → demo (sharing + notifications)
7. US6 → validate → demo (task + dashboard integration)
8. US7 → validate → demo (admin audit/reporting)
9. Polish

### Parallel Team Strategy

With multiple developers, after Foundational is done:

- Developer A: US1 → US2 → US3 (core document lifecycle, sequential — each
  builds on `Documents.razor`)
- Developer B: US7 (Administrator reporting — only depends on Foundational)
- Once US3 lands: Developer A or C picks up US4; Developer B picks up US5 or
  US6 in parallel (different files: `ProjectDetails.razor` vs `Tasks.razor`/`Index.razor`)

---

## Notes

- `[P]` tasks touch different files and have no dependency on a sibling task in
  the same batch
- `[Story]` label maps each task to its user story for traceability
- All `DocumentService.cs` edits are sequenced (not parallelized) to avoid
  concurrent-edit conflicts in a single shared file
- No test tasks are included — see the Tests note at the top of this file
- Commit after each task or logical group; stop at any checkpoint to validate
  a story independently using quickstart.md
