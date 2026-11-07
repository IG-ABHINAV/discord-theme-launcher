---
name: dotnet-desktop-architect
description: Review, design, or refactor Windows desktop applications written in C# with WPF, WinForms, Windows App SDK, or modern .NET. Use when Codex needs to assess desktop architecture, MVVM or MVP separation, dependency injection, async/await correctness, UI thread safety, error handling, logging, automated tests, MSIX or ClickOnce packaging, deployment, and Windows 11 compatibility.
---

# .NET Desktop Architect

Use this skill to evaluate or design production Windows desktop apps in C# across WPF, WinForms, and modern .NET. Prefer the existing project style, but raise architecture risks when the code mixes UI, business rules, infrastructure, and deployment concerns.

## Workflow

1. Identify the application type and target framework from project files:
   - WPF: `UseWPF`, `.xaml`, `Application`, `Window`, `UserControl`.
   - WinForms: `UseWindowsForms`, `Form`, `UserControl`, designer partials.
   - Windows App SDK or WinUI interop: package references, `app.manifest`, MSIX config.
   - Packaging: `.wapproj`, `.pubxml`, `.appinstaller`, `.msix`, ClickOnce properties.
2. Map the solution boundaries before editing:
   - UI layer: views, forms, controls, XAML, designer files.
   - Presentation layer: view models, presenters, commands, binding adapters.
   - Domain layer: business rules independent from UI framework.
   - Application layer: workflows, use cases, validation, orchestration.
   - Infrastructure layer: file system, database, network, OS APIs, logging sinks.
3. Inspect the highest-risk paths first:
   - App startup and dependency composition.
   - Main windows/forms and long-running event handlers.
   - Shared services that touch UI thread, file system, network, registry, credentials, or process execution.
   - Packaging and deployment configuration.
4. Produce concrete findings with file/line references. Include impact, recommended change, and verification.
5. When implementation is requested, make small, locally consistent changes and add or update tests at the nearest feasible boundary.

Load `references/desktop-review-checklist.md` when doing a broad review, architecture assessment, migration plan, or packaging readiness check.

## Architecture Rules

Keep UI framework objects out of domain and application services. Do not pass `Window`, `Form`, `Control`, `Dispatcher`, `SynchronizationContext`, or `IServiceProvider` through business logic unless the surrounding architecture already has an explicit adapter pattern.

For WPF, prefer MVVM:
 - Use bindable view models, commands, observable state, and validation interfaces.
 - Keep code-behind limited to view-only behavior that is hard to express declaratively.
 - Avoid direct service calls from XAML code-behind unless the app is intentionally small.

For WinForms, prefer MVP or a pragmatic presenter/service split:
 - Keep designer files generated and untouched.
 - Move business decisions out of event handlers.
 - Use presenters or application services to make workflows testable without opening forms.

Use dependency injection deliberately:
 - Put composition at application startup.
 - Prefer constructor injection for services and view models/presenters.
 - Avoid service locator patterns except at UI factories, navigation, or plugin boundaries.
 - Dispose scopes and long-lived resources correctly.

## Async And UI Threading

Treat UI responsiveness as an architectural requirement.

Use `async Task` for asynchronous workflows. Reserve `async void` for UI event handlers only, and catch/report exceptions inside those handlers or delegate to a tested command/service boundary.

Never block the UI thread with `.Result`, `.Wait()`, long CPU loops, synchronous I/O, or network calls. Use cancellation tokens for user-cancelable operations and app shutdown.

Update UI-bound state only on the UI thread. In WPF, use dispatcher-aware adapters sparingly; in WinForms, use `Control.Invoke`/`BeginInvoke` wrappers or synchronization abstractions where needed.

## Errors, Logging, And Diagnostics

Separate user-facing messages from diagnostic detail. Do not expose stack traces, file paths, secrets, connection strings, or raw exception messages in UI dialogs.

Use structured logging through `Microsoft.Extensions.Logging` or the project standard. Include operation context and correlation details when useful, but redact secrets and personal data.

Define a consistent error boundary:
 - Global unhandled exception handling for last-resort reporting.
 - Local error handling around user actions, file import/export, network calls, and persistence.
 - Retry only for transient operations and only with cancellation/timeouts.

## Tests

Prefer tests that do not require a desktop session for domain, application, view model, and presenter logic. Use mocks or fakes at infrastructure boundaries.

For WPF MVVM, test commands, property notifications, validation, and state transitions. For WinForms MVP, test presenters against view interfaces. Add integration or UI automation tests only for critical workflows or regressions that cannot be covered below the UI.

Do not overfit tests to implementation details such as exact logging text, control layout, generated designer code, or XAML formatting.

## Packaging And Windows 11

Check packaging against the app's distribution target:
 - MSIX for Store, enterprise deployment, clean install/uninstall, app identity, app installer updates, and declared capabilities.
 - ClickOnce for lightweight internal deployment where the project already uses it and MSIX is not required.
 - Plain installer only when the project has a deliberate packaging toolchain.

For Windows 11 compatibility, review target framework, supported OS settings, app manifest, DPI awareness, accessibility, high contrast, light/dark theme behavior, file associations, elevation requirements, long path assumptions, and ARM64/x64 expectations.

When exact SDK, Store, ClickOnce, MSIX, or Windows 11 requirements matter, verify against current Microsoft Learn documentation before making claims.

## Output Format

For reviews, lead with findings ordered by severity. Include:
 - Severity and short title.
 - File/line reference.
 - Why it matters.
 - Concrete fix or design direction.
 - Test or manual verification.

For implementation plans, keep the plan scoped to:
 - Boundaries to change.
 - Public interfaces to introduce or preserve.
 - Risky migrations and compatibility checks.
 - Tests and packaging validation.
