# Copilot Instructions

Use [docs/copilot-context.md](../docs/copilot-context.md) for the application overview, architecture, persistence behavior, and important feature workflows. Treat the current source code as authoritative when documentation and implementation differ.

## General Guidelines
- Keep changes focused on the requested task and follow existing project patterns.
- Prefer minimal, root-cause fixes over workarounds that hide failures.
- Before editing, inspect affected code and relevant callers; validate changes with a build and applicable tests.
- Do not add dependencies unless necessary.
- Do not commit machine-specific paths, credentials, or other secrets.
- When the user asks for repository commits, group changes by logical purpose and stage, review, validate, and commit each group sequentially, preserving unrelated staged and unstaged work.
- For documentation-only requests, use narrowly scoped comment-only edits and verify the diff contains no executable-code changes before finishing.

## Project-Specific Rules
- The project targets .NET 8 for Windows and uses WPF plus Windows Forms integration.
- View models inherit `ViewModelBase`/`NotifyBase`; use existing `SetProperty` helpers where available and follow neighboring notification patterns.
- Keep view models focused on state and commands; put file/database work in existing services and use existing factories/DI.
- Preserve the dark WPF theme resources and custom control templates in `App.xaml`. When changing a template, check the actual content, popup, focus, selection, and disabled states—not only brush resources.
- Database initialization occurs during application startup before the main window is created. Keep schema creation and startup ordering consistent with `DatabaseService.InitializeAsync`.
- During development, when a faulty database schema causes runtime issues, prefer resetting/recreating the database rather than adding schema repair or migration code.
- For WPF menu appearance, prefer a fully custom MenuItem style/template inspired by Visual Studio rather than changing only the default style's border brushes.
- Avoid changing unrelated behavior and preserve existing transfer safety rules, especially source deletion for move operations.
- When changing WPF disabled-button colors, verify the actual rendered button content and template trigger behavior; changing a brush resource alone may not alter the visible text color if template/content presentation overrides it.

## Feature Roadmap Status
The planned sequence has been implemented: retry failed transfers, conflict handling options, rule validation, profile import/export, and dry-run preview. Treat these as existing functionality to maintain, not pending work. See the project context document for behavior notes.
