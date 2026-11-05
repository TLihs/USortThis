# USortThis — Copilot Project Context

## Purpose

USortThis is a Windows desktop application for organizing files with user-defined profiles. A profile contains folder bridges; each bridge watches or receives files from a source and evaluates its routes. Matching files are copied to route destinations and may be removed from the source only when the bridge is configured to move files and the transfer safety checks verify all applicable destinations.

This document is a session handoff. Source code is authoritative when implementation and documentation differ.

## Technology and entry points

- .NET 8 Windows desktop (`net8.0-windows`), WPF UI, and Windows Forms integration for system tray and folder dialogs.
- SQLite persistence through `Microsoft.Data.Sqlite`.
- Dependency injection is configured in `USortThis/App.xaml.cs`; `App.ServiceProvider` is the app service provider.
- Startup awaits `IDatabaseService.InitializeAsync()` before creating the main window. Database initialization loads profile/device data and ensures the schema exists.
- Shared dark-theme brushes and implicit control styles are in `USortThis/App.xaml`.
- Main window: `USortThis/UI/Views/MainWindow.xaml`; profile management: `USortThis/UI/Views/Contols/ProfileListControl.xaml`; profile creation/editing: `USortThis/UI/Views/ProfileEditorWindow.xaml`.

## Architecture

- `Core/Models` contains observable domain models, including `ProfileItemModel`, `FolderBridgeModel`, `FilterRuleModel`, criteria models, and transfer history details.
- `Core/ViewModels` owns presentation state and commands. View models should delegate file and database work to services and use existing factories/DI.
- `Core/Services` contains database, filesystem, configured-location, sorting, and profile import/export logic.
- `Core/Interfaces` defines service and factory boundaries.
- `Core/Factories` constructs models that need generated IDs or runtime dependencies.
- WPF views are in `UI/Views` and `UI/Views/Contols` (the misspelling is an existing folder name; preserve it unless a requested change requires otherwise).

## Persistence and startup

- SQLite database is named `USortThis.db` under the current user's local application data `USortThis` directory; do not hardcode a developer machine path.
- `DatabaseService.InitializeAsync()` creates the app schema before view models query it. Keep this ordering intact.
- The schema includes profiles, bridges, folders, routes/rule criteria, devices, configured locations, copy queue/history, and transfer history. Profile route settings and criteria are serialized in the database service.
- Profiles retain conflict-handling mode. Transfer history is associated with a bridge and source/destination paths.
- During development, for a faulty schema causing runtime failures, the project preference is to reset/recreate the development database rather than add ad hoc schema repair/migration code. Any intentional schema migration requirement should be assessed against this explicit preference and existing initialization behavior.
- Configured-location credentials are managed separately through the Windows credential store; do not export secrets.

## Profile and transfer behavior

- A profile contains one or more folder bridges. A bridge has a source folder (or dropped paths), routes, transfer history, and a `MoveSourceAfterCopy` setting.
- `FilterRuleModel.Matches` uses AND semantics across its criteria. Empty criteria do not match. Individual criteria may have match-all behavior for empty values; inspect each criterion implementation before changing validation or matching.
- Route destination paths support preserving relative folders and date-folder modes.
- `SortingService` enumerates files, matches routes, builds destinations, persists progress/status in transfer history, and performs retries for transient I/O errors.
- Move safety is important: source deletion must remain conditional on successful copies to all relevant destinations and destination verification. A targeted retry must not bypass these safeguards.
- Per-profile conflict modes are Skip, Overwrite, KeepBoth, and AskEachTime. AskEachTime is surfaced through the UI; KeepBoth uses a unique alternate filename. Preserve the established behavior when modifying destination handling.
- Retry failed transfer is exposed in the main transfer-history panel. It updates the selected history record; targeted retry must not delete its source.

## Implemented profile features

- **Rule validation:** Profile editor reports errors and warnings; invalid configurations block saving, while advisories are non-blocking. Validation is intentionally limited to statically checkable conditions rather than claiming arbitrary rule-overlap detection.
- **Profile import/export:** `ProfileImportExportService` uses a versioned JSON DTO format and regenerates IDs on import. Exports include configuration such as profile settings, bridges, routes, and criteria, but not transfer history or credentials.
- **Dry-run preview:** `SortingService.PreviewAsync` enumerates and calculates matching destinations without copying files or writing transfer history. Preview shows planned conflict actions and enumeration warnings. Keep it read-only and avoid creating destination directories/files.

## UI and styling practices

- Preserve the app's dark WPF design and shared theme resources.
- A brush-only change may not alter visuals when a control template paints its own content. Inspect and update the template as needed, including popup surfaces, headers, content presenters, focus, selection, hover, and disabled states.
- ComboBox templates must keep the selection content presenter separate from the narrow dropdown arrow control, and preserve `DisplayMemberPath`/item template behavior.
- For menu appearance, use the existing custom MenuItem template approach rather than only changing default border brushes.
- View models inherit `ViewModelBase`/`NotifyBase`; follow nearby property notification patterns and use existing `SetProperty` helpers where available.

## Validation and testing

- Build the solution after code/XAML changes using the available Visual Studio build workflow.
- Discover and run relevant tests when test projects/tests exist. If none are available, report that rather than implying test coverage.
- For transfer-related changes, verify no unintended source deletion, overwrite, or history mutation occurs.
- For database failures, inspect startup ordering and the exact database path used by `DatabaseService` before changing initialization behavior.

## Feature roadmap status

Implemented features include:

- Retry failed transfers.
- Conflict handling options.
- Rule validation.
- Profile import/export.
- Dry-run preview.
- Rule testing against selected sample files.
- Transfer history cards showing paths, status, progress, and failure details.

Pending feature candidates:

- Scheduled or continuous sorting, with clear pause/resume controls and run status.
- Duplicate detection using file content hashes, with user-controlled conflict handling.
- Transfer notifications and completion/failure summaries linked to transfer history.
- Safe undo or restore workflows for eligible transfers, with explicit safeguards for moves and overwritten files.
- Backup and restore for profiles and application settings, without exporting credentials.
- Implement build and test tools.
- Implement proper localization.

Treat implemented features as existing behavior to maintain. Pending candidates are suggestions, not committed requirements; confirm scope and safety implications before implementation.
