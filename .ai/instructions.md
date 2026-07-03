# Repository Instructions

## 2026-07-02

- Treat `State<T>` as immutable. Use `with` expressions for changes inside library code.
- Build new services by deriving from `StateService<T>` and exposing clear command methods.
- Use `SetAsync(StateValueFactory<T>, CancellationToken)` for state transitions. Do not bypass the base class transition flow.
- Depend on `IStateService<T>` when writing consumers that only need current state and change notifications.
- Use `StateChanged` for observation, not polling loops.
- Keep example code simple, async, and cancellation-token friendly.
- Preserve the namespaces `TripleG3.Cis` and `TripleG3.Cis.Examples` unless the user explicitly asks for a namespace change.
- Validate library changes with `dotnet build TripleG3.Cis.slnx` from the repository root.
