# Phase 1 Data Model: Document Upload and Management

Derived from [spec.md](./spec.md) Key Entities and Functional Requirements, and
from the stakeholder document's explicit technical constraints (integer keys,
text-based category, 255-char file type).

## Document

Represents one uploaded file and its metadata.

| Field | Type | Rules |
|---|---|---|
| `DocumentId` | `int` (PK, identity) | Integer key, consistent with existing `User`/`Project` keys (stakeholder constraint) |
| `Title` | `string`, required, max 255 | FR-004 |
| `Description` | `string?`, optional, max 2000 | FR-004 |
| `Category` | `string`, required, max 100 | Stored as text, not an enum (stakeholder constraint). Allowed values enforced at the application layer: Project Documents, Team Resources, Personal Files, Reports, Presentations, Other |
| `ProjectId` | `int?` (FK → Project) | Optional project association (FR-004, FR-011); `Restrict` on delete — a project is never hard-deleted by this feature |
| `Tags` | `string?`, max 500 | Simple comma-separated free-form tags (Simplicity principle — no separate tags table for this scope) |
| `UploadedByUserId` | `int` (FK → User) | Required; `Restrict` on delete |
| `UploadDate` | `DateTime` (UTC) | Set automatically at upload (FR-005) |
| `FileSizeBytes` | `long` | Captured automatically (FR-005); validated ≤ 25 MB at upload (FR-001/FR-002) |
| `FileType` | `string`, max **255** | MIME type; 255 chars to accommodate long Office MIME strings (stakeholder constraint) |
| `FilePath` | `string`, required, unique, max 500 | GUID-based relative path, e.g. `{userId}/{projectId-or-"personal"}/{guid}.{ext}` (FR-007); never derived from user-supplied filename |
| `OriginalFileName` | `string`, max 255 | The user's original filename, stored for display/download purposes only — never used to build `FilePath` |

**Validation rules**:
- `Category` MUST be one of the fixed allowed values (FR-004).
- `FileType` MUST be one of the supported MIME types for PDF/Office/text/image
  types (FR-001).
- `FileSizeBytes` MUST be ≤ 25 MB (FR-001/FR-002).
- `FilePath` MUST be unique and generated server-side before the file is saved to
  disk (FR-007; matches the stakeholder doc's "generate unique path → save file →
  save metadata" sequencing to avoid orphaned records/duplicate-key errors).

**Relationships**: one `User` (uploader) → many `Document`; one `Project` (optional)
→ many `Document`; one `Document` → many `DocumentShare`; one `Document` → zero or
more `TaskItem` attachments (see Task Attachment below).

## DocumentShare

Represents one document having been shared with a specific user or an entire
Department.

| Field | Type | Rules |
|---|---|---|
| `DocumentShareId` | `int` (PK, identity) | |
| `DocumentId` | `int` (FK → Document) | `Cascade` on delete — shares are meaningless once the document is gone (FR-015 hard delete) |
| `SharedByUserId` | `int` (FK → User) | The document owner who performed the share |
| `SharedWithUserId` | `int?` (FK → User) | Set when sharing with one specific user |
| `SharedWithDepartment` | `string?`, max 100 | Set when sharing with an entire Department (FR-016) |
| `SharedDate` | `DateTime` (UTC) | |

**Validation rule**: exactly one of `SharedWithUserId` / `SharedWithDepartment` MUST
be set per record (a share targets either one user or one department, not both/
neither).

**Relationships**: one `Document` → many `DocumentShare`; one `User` (sharer) and
optionally one `User` (direct recipient) per record.

## DocumentActivityLogEntry

An immutable audit record of one document-related action, for Administrator
reporting (FR-021, FR-022).

| Field | Type | Rules |
|---|---|---|
| `LogId` | `int` (PK, identity) | |
| `DocumentId` | `int` | **Not** an enforced EF Core navigation/FK relationship — stored as a plain value so the audit trail survives after a `Document` row is hard-deleted (FR-015 allows permanent deletion; audit history must not be lost or block deletion) |
| `DocumentTitleSnapshot` | `string`, max 255 | Copy of the document's title at the time of the action, so the log remains meaningful after deletion |
| `ActorUserId` | `int` (FK → User) | Who performed the action |
| `ActionType` | `enum` (`Upload`, `Download`, `Delete`, `Share`) | FR-021 |
| `Timestamp` | `DateTime` (UTC) | |

**Relationships**: one `User` (actor) → many `DocumentActivityLogEntry`; logically
references a `Document` by id/title snapshot only (no enforced FK, see above).

## Task Attachment (uses existing `TaskItem`)

No new entity is introduced for task attachment. A document "attached" to a task
(FR-017) is represented by adding a nullable `TaskId` (`int?`, FK → `TaskItem`,
`SetNull` on delete) to `Document`. A document attached to a task is automatically
given that task's `ProjectId` at creation time (per FR-017), so task-document
visibility reuses the existing `ProjectId` association — no separate join table
needed (Simplicity principle).

## Indexes (for the performance targets in Success Criteria)

- `Document(UploadedByUserId)` — for "My Documents" list (SC-003)
- `Document(ProjectId)` — for Project Documents view
- `Document(Category)` — for category filter
- `Document(UploadDate)` — for sort/recent-documents widget
- `DocumentShare(SharedWithUserId)`, `DocumentShare(SharedWithDepartment)` — for
  "Shared with Me" lookups
- `DocumentActivityLogEntry(Timestamp)` — for Administrator report queries

## State / Lifecycle

`Document` has no explicit status field — its lifecycle is simply
created → (optionally edited/file-replaced any number of times) → hard-deleted.
There is no soft-delete/trash state (explicitly out of scope per the spec's
Assumptions section).
