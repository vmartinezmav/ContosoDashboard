# Quickstart: Validating Document Upload and Management

Prerequisites: .NET 10 SDK, a running local SQL Server instance (LocalDB or SQL
Server Express — see repo setup notes), the app building successfully
(`dotnet build` from `ContosoDashboard/`).

This guide maps each user story in [spec.md](./spec.md) to a quick manual
validation pass. Run `dotnet run` from `ContosoDashboard/` and log in via the mock
login screen (`/login`) as the indicated seeded user for each scenario.

## US1 — Upload a Document (P1)

1. Log in as any user (e.g., Ni Kang, Employee).
2. Navigate to **Documents** → **Upload**.
3. Select a PDF under 25 MB, enter a title, choose a category, submit.
4. **Expect**: progress indicator during upload, success message, document
   appears in "My Documents" with correct title/category/date/size.
5. Retry with a file over 25 MB, then with an unsupported type (e.g. `.exe`).
   **Expect**: both rejected with a clear, specific error before any file is
   stored.

## US2 — Search, Filter, and Sort (P2)

1. Upload 3–4 documents with varying titles/categories/dates/projects.
2. In "My Documents", search by a keyword in one document's title.
   **Expect**: only matching documents returned, within ~2 seconds.
3. Filter by category, then by project, then by date range.
   **Expect**: list narrows correctly each time.
4. Sort by title, date, category, and size.
   **Expect**: order changes correctly for each.

## US3 — Download, Preview, Edit, Delete (P3)

1. Download a document you own. **Expect**: retrieved file matches the original.
2. Preview a PDF or image document in-browser. **Expect**: renders without a
   download prompt, within ~3 seconds.
3. Edit a document's title/description/category/tags. **Expect**: changes persist.
4. Replace its file with a new one. **Expect**: same document record, updated
   file content.
5. Delete it, confirm. **Expect**: removed from every list; file no longer
   downloadable (404).
6. As a *different*, unrelated user, attempt to open that document's
   download/preview/edit URL directly by ID. **Expect**: denied (403/404).

## US4 — Project Documents & Team Lead Management (P4)

1. As a Project Manager, upload a document associated with a project you manage.
2. Log in as another member of that project. **Expect**: the document is visible
   and downloadable from the Project Documents view.
3. As the Project Manager, edit/delete a document in that project that a
   *different* user uploaded. **Expect**: succeeds.
4. As a Team Lead (e.g., Floris Kregel, Engineering), verify you can view/edit/
   delete a document uploaded by anyone else in the Engineering department —
   even one not associated with any project you belong to.
5. As a Team Lead in a *different* department than the document's uploader,
   attempt the same. **Expect**: denied, unless you have another valid access
   basis (project membership or direct share).

## US5 — Share Documents & Notifications (P5)

1. As a document owner, share it with one specific colleague.
   **Expect**: recipient gets an in-app notification and sees it under
   "Shared with Me."
2. Share a document with an entire Department. **Expect**: all users in that
   department see it under "Shared with Me."
3. Log in as a user the document was *not* shared with. **Expect**: it does not
   appear in their "Shared with Me."

## US6 — Task & Dashboard Integration (P6)

1. Open an existing task's detail view; attach/upload a new document from there.
   **Expect**: document is created and automatically associated with the task's
   project; visible from the task.
2. Go to the dashboard home page. **Expect**: "Recent Documents" widget shows the
   5 most recently uploaded documents for the logged-in user, and a summary card
   shows their total document count.

## US7 — Administrator Audit & Reporting (P7)

1. Perform a mix of upload/download/delete/share actions as a couple of
   different test users.
2. Log in as the Administrator. Open the document audit/reporting view.
   **Expect**: every action appears in the activity log with who/what/when, and
   summary reports show most-uploaded types, most active uploaders, and access
   patterns.
3. Log in as a non-Administrator and attempt to reach the same audit view.
   **Expect**: access denied.

## Cross-cutting checks

- Confirm no uploaded file ever becomes reachable at a direct `wwwroot` URL —
  only via the authenticated `/documents/{id}/download` and `/preview` endpoints
  (see [contracts/document-endpoints.md](./contracts/document-endpoints.md)).
- Confirm an interrupted upload (e.g., kill the browser tab mid-upload) leaves no
  orphaned document record pointing at a missing file.
- Confirm uploading an unscannable/flagged file is rejected per the malware-scan
  decision in [research.md](./research.md).
