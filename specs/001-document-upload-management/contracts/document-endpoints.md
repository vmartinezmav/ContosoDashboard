# Contracts: Document Download & Preview Endpoints

ContosoDashboard is a Blazor Server app — most document operations (upload, list,
search, edit, delete, share) happen through direct component → `IDocumentService`
calls over the existing SignalR circuit and need no HTTP contract. The two
operations that must cross an HTTP boundary (because Blazor Server circuits can't
stream arbitrary file bytes back to the browser — the same reason this app already
uses separate Razor Pages for Login/Logout instead of Blazor components) are
**download** and **preview**. These are exposed as small authenticated minimal-API
endpoints, mapped from `Program.cs` via `Endpoints/DocumentEndpoints.cs`.

## `GET /documents/{documentId:int}/download`

**Authorization**: Requires the existing cookie authentication
(`RequireAuthorization()`); then `IDocumentService` performs the same
ownership/share/project/department authorization check used everywhere else
(FR-020).

| Response | When |
|---|---|
| `200 OK`, file stream, `Content-Disposition: attachment; filename="<OriginalFileName>"`, correct `Content-Type` | Document exists and caller is authorized |
| `403 Forbidden` | Document exists but caller is not authorized (owner / shared-with user / shared-with department / project member / project's Project Manager / uploader's Team Lead / Administrator) |
| `404 Not Found` | Document does not exist (also returned instead of 403 when the caller has no basis to know the document exists, to avoid confirming IDs to unauthorized users) |

**Side effect**: records a `DocumentActivityLogEntry` with `ActionType = Download`.

## `GET /documents/{documentId:int}/preview`

Same authorization rules as download.

| Response | When |
|---|---|
| `200 OK`, file stream, `Content-Disposition: inline`, correct `Content-Type` | Document is a PDF or image (JPEG/PNG) type and caller is authorized (FR-013) |
| `415 Unsupported Media Type` | Document exists, caller is authorized, but its file type isn't previewable in-browser (Office/text documents — caller should use download instead) |
| `403 Forbidden` / `404 Not Found` | Same rules as download |

**Side effect**: none logged (preview is not one of the FR-021 audited action
types; only upload/download/delete/share are).

## Notes

- Both endpoints read the file via `IFileStorageService.DownloadAsync(filePath)`
  — never by trusting a path supplied in the request. The route parameter is
  always a `Document.DocumentId`, which the service resolves to the server-known
  `FilePath` internally (prevents path traversal regardless of input).
- No other REST/API contracts are introduced by this feature; all other
  interactions are internal Blazor Server component-to-service calls, which are
  implementation detail rather than a public contract.
