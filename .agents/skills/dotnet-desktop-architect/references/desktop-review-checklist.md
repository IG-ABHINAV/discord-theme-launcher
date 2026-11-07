# Desktop Review Checklist

Use this checklist for broad architecture reviews, migration planning, and release readiness checks for C# Windows desktop applications.

## Project And Runtime

- Confirm target frameworks and runtime support: `net8.0-windows`, `net9.0-windows`, or project-specific target.
- Check `UseWPF`, `UseWindowsForms`, nullable reference types, implicit usings, analyzers, and warning level.
- Identify framework dependencies: WPF, WinForms, Windows App SDK, COM interop, native libraries, registry, services, shell integration.
- Verify bitness assumptions: AnyCPU, x86, x64, ARM64, native DLL loading, COM registration, and installer architecture.

## Architecture

- Keep UI, presentation, domain, application, and infrastructure concerns separate.
- Avoid business logic in XAML code-behind, WinForms event handlers, generated designer files, and global application events.
- Use DTOs or domain models that do not depend on UI controls.
- Isolate OS-specific services behind interfaces when they affect testability or portability.
- Keep navigation/window creation in a UI factory, coordinator, shell service, or composition root.

## WPF MVVM

- Bind view state to view models through properties, observable collections, commands, and validation.
- Ensure `INotifyPropertyChanged` and collection updates are correct and thread-safe.
- Avoid heavy work in property getters and converters.
- Prefer commands over direct click handlers for business actions.
- Keep resources, templates, and styles reusable without hiding behavior in XAML.

## WinForms MVP

- Keep designer partial classes generated and avoid manual edits.
- Move workflow decisions from event handlers into presenters or application services.
- Use view interfaces for presenter tests when the form is complex or business-critical.
- Avoid cross-thread control access; marshal updates through the UI layer.

## Dependency Injection

- Check one composition root at startup.
- Use constructor injection for view models, presenters, services, and infrastructure clients.
- Avoid resolving services throughout business logic.
- Register lifetimes intentionally: singleton for stateless services, scoped/transient for unit-of-work and disposable resources.
- Dispose hosted services, database contexts, file watchers, timers, and cancellation token sources.

## Async And Responsiveness

- Flag `.Result`, `.Wait()`, `Thread.Sleep`, synchronous file/network/database work on UI paths.
- Use `async Task`; keep `async void` limited to event handlers.
- Propagate `CancellationToken` for long-running operations, imports, exports, searches, downloads, and shutdown.
- Report progress without flooding the UI thread.
- Handle exceptions from commands and event handlers consistently.

## Errors And Logging

- Use structured logging through the project standard, commonly `Microsoft.Extensions.Logging`.
- Avoid logging secrets, tokens, passwords, raw connection strings, personal data, or full local paths unless justified.
- Show user-friendly messages and log diagnostic detail separately.
- Add timeouts and retries only for transient operations.
- Verify global exception handling does not hide bugs during tests.

## Data And Security

- Validate file paths and user input at boundaries.
- Prevent directory traversal when opening, saving, extracting, or importing files.
- Use parameterized queries and avoid string-built SQL.
- Store secrets with Windows Credential Manager, DPAPI, enterprise secret providers, or documented secure storage.
- Check elevation, UAC prompts, registry writes, startup tasks, firewall rules, and external process execution.

## Tests

- Unit test domain logic, application services, view models, presenters, validation, and command behavior.
- Mock infrastructure boundaries rather than UI controls.
- Add integration tests for persistence, file formats, serialization, and external services when risk warrants.
- Use UI automation sparingly for critical flows: startup, login, import/export, update, and packaging smoke tests.
- Verify tests run in CI without an interactive desktop unless explicitly marked.

## Packaging

- For MSIX, check identity, publisher, capabilities, app manifest, file type associations, update settings, signing, dependencies, and install location assumptions.
- For ClickOnce, check publish profile, update mode, prerequisites, signing, application files, and rollback behavior.
- Ensure installers do not require unnecessary admin elevation.
- Verify clean install, upgrade, downgrade or rollback behavior, uninstall cleanup, and first-run state.

## Windows 11 Compatibility

- Check DPI awareness and layout behavior on mixed scaling monitors.
- Verify keyboard navigation, screen reader labels, high contrast, focus visuals, and accessible names.
- Check light/dark theme behavior if supported.
- Avoid relying on deprecated shell behavior or hardcoded system paths.
- Validate window sizing, snap layouts, multi-monitor restore, taskbar integration, notifications, and file associations.
- Test x64 and ARM64 expectations when distribution requires them.
