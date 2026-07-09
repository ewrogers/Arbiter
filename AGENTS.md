# Arbiter Repository Guide

## Project layout

- `Arbiter.App` contains the Avalonia desktop application, view models, services, models, controls, and XAML views.
- `Arbiter.Net` contains the proxy, protocol, packet, observer, filter, security, and serialization code.
- `Arbiter.Json` contains shared JSON converters.
- `Arbiter.Interop` contains platform interop code.
- `Arbiter.Net.Tests` contains NUnit tests for the network library.
- Keep application concerns in their existing feature folders. Split large view models into partial files named after the concern, such as `.Filtering.cs`, `.Observers.cs`, or `.Selection.cs`.

## C# conventions

- Target .NET 10 and keep nullable reference types enabled.
- Use file-scoped namespaces.
- Follow the existing four-space indentation and Allman brace style.
- Keep `using` directives explicit in `Arbiter.App`. The other projects use implicit global usings where configured.
- Prefer collection expressions such as `[]` and primary language features already used by the surrounding code.
- Use `var` when the assigned type is clear from the expression.
- Keep members ordered consistently with nearby code: constants and static fields, injected fields, state fields, generated properties, public properties and events, constructor, then methods.
- Prefer early returns over deep nesting.
- Keep methods focused. Add a concern-based partial file when a view model gains a distinct responsibility.
- Use `ConfigureAwait(false)` in lower-level network and library async code where the continuation does not need a captured context. Application UI code may resume on the Avalonia context when required.

## Avalonia and MVVM conventions

- Use CommunityToolkit attributes such as `[ObservableProperty]`, `[RelayCommand]`, and notification attributes instead of handwritten command and property boilerplate.
- Keep UI-bound collection mutations and UI-bound property changes on `Dispatcher.UIThread`.
- Do not treat a lock around `ObservableCollection<T>` as UI thread safety. Queue background data and apply it to plain observable collections on the UI thread.
- Prefer batching high-volume updates before dispatching them to the UI.
- Keep code-behind limited to view-specific behavior. Application state and actions belong in view models and services.
- Preserve compiled binding data types and the established control theme resources in XAML.

## Network and concurrency conventions

- Packet observers should parse network data and hand immutable state to the owning service or view model.
- Preserve packet order within a connection and include connection identity when state is scoped to a client.
- Use concurrent collections for background producer state only. Do not expose them directly to Avalonia bindings.
- Unregister observers and event handlers when their connection or owner is no longer active.
- Avoid invoking external callbacks while holding a lock. Capture the result under the lock, then notify after releasing it.

## Tests and validation

- Add NUnit tests under `Arbiter.Net.Tests` for network behavior.
- Name tests with the existing `Should_Describe_Expected_Behavior` convention.
- Use `Assert.That` and `Assert.Multiple` consistently with the current tests.
- Run `dotnet build Arbiter.sln` after application changes and `dotnet test Arbiter.Net.Tests/Arbiter.Net.Tests.csproj` after network changes.
- Do not edit generated `bin` or `obj` contents.
- Keep notable pending changes in the `[Unreleased]` section of `CHANGELOG.md`. Move them into the versioned section when preparing a release.

## Writing style

- Match the existing direct, conversational voice in comments, documentation, logs, and user-facing text.
- Write comments only when they explain intent, protocol behavior, or a non-obvious constraint.
- Keep comments short and natural. Avoid long documentation comments unless a public API genuinely needs them.
- Do not use em dashes or emojis in code comments, documentation, commit messages, or user-facing text.
