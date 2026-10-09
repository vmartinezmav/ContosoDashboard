<!--
Sync Impact Report
==================
Version change: [unset/template] → 1.0.0
Rationale: Initial ratification. No prior filled-in constitution existed (file was
still the unfilled template scaffold), so this is treated as the first adoption
rather than an amendment.

Modified principles: N/A (initial draft)

Added sections:
- Core Principles (I–V)
- Technology & Architecture Constraints
- Development Workflow
- Governance

Removed sections: N/A

Templates requiring updates (not modified by this command — informational only):
- .specify/templates/plan-template.md ⚠ pending manual review for alignment with
  the principles below (Constitution Check gates)
- .specify/templates/spec-template.md ⚠ pending manual review
- .specify/templates/tasks-template.md ⚠ pending manual review
- .specify/templates/checklist-template.md ⚠ pending manual review

Deferred / follow-up items:
- TODO(RATIFICATION_DATE): Original adoption date is unknown (this is the first
  time the constitution placeholders were filled in). Replace with the actual
  date the team formally ratifies this document.

This report is scratch material for human review and should be removed before
the amended constitution is committed, once the Sync Impact has been reviewed.
-->

# ContosoDashboard Constitution

## Core Principles

### I. Training-First Transparency (NON-NEGOTIABLE)
ContosoDashboard is a training artifact for teaching Spec-Driven Development (SDD)
with the GitHub Spec Kit, not a production system. Every feature, spec, and
implementation MUST preserve this framing:
- Known limitations and mock behaviors (e.g., mock authentication) MUST be
  documented in-repo, not hidden or silently "fixed" into a false sense of
  production-readiness.
- Changes MUST NOT introduce real external/cloud service dependencies by
  default; the application MUST remain runnable fully offline.
- Where a shortcut is taken for training simplicity (e.g., skipping password
  hashing), the spec or code comment MUST say so explicitly.
Rationale: Learners and instructors rely on the repository accurately
representing what is a teaching simplification versus a real security or
architectural recommendation. Silent drift erodes the training value and risks
the code being mistaken for a production template.

### II. Offline-First with Cloud Migration Path
All infrastructure-facing functionality (storage, file handling, identity,
notifications, etc.) MUST be built behind an interface abstraction with a
local/offline implementation as the default, and MUST be designed so a cloud
implementation (e.g., Azure Blob Storage, Microsoft Entra ID) can be substituted
via dependency injection without changes to business logic, UI, or database
schema.
Rationale: This is the repository's established architecture pattern (see
`IFileStorageService` precedent) and is itself a teaching objective — students
learn proper abstraction and DI by seeing the local-to-cloud swap path kept
viable.

### III. Security by Design, Scoped to Training Context
Even though ContosoDashboard is not production software, implemented features
MUST still demonstrate real secure-coding discipline so students learn correct
habits:
- Authorization MUST be enforced at every layer touched by a feature (page/
  route attributes AND service-level checks) — defense in depth, not a single
  gate.
- Any feature exposing user-referenced resources (documents, tasks, profiles,
  etc.) MUST include IDOR protection: ownership/role checks before returning or
  mutating data.
- File- or user-supplied input MUST be validated (type, size, format) before
  use; user-supplied values MUST NOT be used directly to construct file paths
  or storage keys — generate unique, non-guessable identifiers instead.
- Known gaps relative to production security (no real password hashing, no
  MFA, no external identity provider) MUST stay documented rather than
  partially/inconsistently patched.
Rationale: Teaching secure-by-design habits is a stated purpose of the
training; demonstrating sloppy security would teach the wrong lesson even in a
non-production app.

### IV. Clean Separation of Concerns
Code MUST follow the established layering: `Models/` (EF Core entities),
`Data/` (DbContext), `Services/` (business/service logic behind interfaces),
`Pages/`/`Shared/` (Blazor UI). New features MUST NOT place business logic
directly in Razor components/code-behind when a service abstraction is the
established pattern for that concern.
Rationale: Consistent layering is both a maintainability requirement and a
teaching example of clean architecture for students following the codebase.

### V. Simplicity Appropriate to a Training Codebase
Implementations MUST favor the simplest approach that correctly satisfies the
spec and the principles above (YAGNI). Added complexity (new abstractions,
frameworks, patterns) MUST be justified against a concrete, current
requirement — not speculative future needs — since added complexity directly
increases the cognitive load for learners.
Rationale: The codebase doubles as teaching material; unnecessary complexity
obscures the concepts being taught.

## Technology & Architecture Constraints

- **Framework/runtime**: ASP.NET Core with Blazor Server, targeting the .NET
  version configured in `ContosoDashboard.csproj`. Upgrades to the target
  framework are allowed but MUST keep the app building and running offline.
- **Database**: SQL Server (LocalDB or a local SQL Server Express instance)
  accessed via Entity Framework Core. Production-grade migration tooling
  (versioned EF Core migrations) SHOULD be adopted before any feature that
  requires repeatable, reversible schema changes; `EnsureCreated()` MAY remain
  for quick local training setup until that need arises.
- **Authentication/Authorization**: Cookie-based mock authentication with
  claims-based roles stays the default for training. Any change to the auth
  model MUST preserve the documented role hierarchy (Employee, TeamLead,
  ProjectManager, Administrator) and MUST NOT introduce a real external
  identity dependency as a requirement to run the app locally.
- **Styling/UI**: Bootstrap 5.x with Bootstrap Icons remains the default UI
  toolkit unless a spec explicitly calls for a change.
- **No required external/cloud services**: Features MUST work fully offline
  by default; optional cloud implementations (e.g., Azure Blob Storage) MAY be
  added as alternate, swappable implementations only.

## Development Workflow

- Non-trivial features MUST go through the Spec Kit flow in this repo
  (`/speckit.specify` → `/speckit.clarify` as needed → `/speckit.plan` →
  `/speckit.tasks` → `/speckit.implement`), so the spec/plan/tasks artifacts
  stay the source of truth for what was built and why.
- Every plan produced via `/speckit.plan` MUST include an explicit Constitution
  Check against the principles above before implementation begins; violations
  MUST be justified in the plan's Complexity Tracking section or the design
  MUST be simplified to comply.
- Code changes MUST keep the project buildable (`dotnet build`) and, where
  tests exist for the touched area, passing, before being considered complete.
- Stakeholder requirement documents under `StakeholderDocs/` are treated as
  upstream input to specs, not as a substitute for them — a feature described
  there still needs a corresponding spec before implementation.

## Governance

This constitution supersedes other informal conventions for anything it
explicitly addresses. Where it is silent, existing repository patterns (e.g.,
the `IFileStorageService` abstraction precedent) govern by example.

**Amendment procedure**: Propose changes via `/speckit.constitution` with the
specific principle or section to change and why. Amendments are recorded via
the Sync Impact Report convention at the top of this file and by incrementing
the version per the policy below.

**Versioning policy** (semantic versioning for governance text):
- MAJOR: Backward-incompatible governance changes — removing or redefining a
  principle in a way that invalidates prior compliant work.
- MINOR: Adding a new principle or materially expanding guidance in an
  existing section.
- PATCH: Clarifications, wording fixes, typo corrections, non-semantic
  refinements.

**Compliance review**: Any `/speckit.plan` run MUST re-check its design
against this constitution's Core Principles before tasks are generated. Pull
requests or reviews for this repo SHOULD note explicitly if a change touches
an area governed by a Core Principle above.

**Version**: 1.0.0 | **Ratified**: TODO(RATIFICATION_DATE): original adoption date unknown | **Last Amended**: 2026-10-09
