# Copilot Instructions

## General Guidelines
- First general instruction
- Second general instruction

## Code Style
- Use specific formatting rules
- Follow naming conventions

## Project-Specific Rules
- The project uses a `NotifyBase` class with `OnPropertyChanged` but not `SetProperty`. Prefer adding a protected `SetProperty<T>(ref T field, T value, string? propertyName = null)` helper to reduce duplication in view models.
- When changing WPF disabled-button colors, verify the actual rendered button content and template trigger behavior; changing a brush resource alone may not alter the visible text color if template/content presentation overrides it.

## USortThis Feature Roadmap
- Prioritize in the following order: 
  1. Retry failed transfers
  2. Conflict handling options
  3. Rule validation
  4. Profile import/export
  5. Dry-run preview