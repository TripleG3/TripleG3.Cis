# TripleG3.Cis

## Asynchronously Request Change, Read Immutable State, Listen For State Changes - Clean, Simple, Safe

Command Immutable State Pattern: a tiny .NET library for services that need one clear state snapshot, one update pipeline, and simple change notifications.

Think of it as a calm little command center for async work: call a method, the service becomes `Busy`, it produces a value, then it becomes `Ready`. If something goes sideways, the state lands on `Error` with a message.

## Install Or Reference

This repo currently contains the library project directly. Reference it from another project with a normal project reference:

```xml
<ItemGroup>
  <ProjectReference Include="..\TripleG3.Cis\TripleG3.Cis.csproj" />
</ItemGroup>
```

Build everything from the repository root:

```powershell
dotnet build TripleG3.Cis.slnx
```

## The Simple Idea

`TripleG3.Cis` gives you a pattern for command-driven state:

1. A public method represents the command, such as `RefreshAsync` or `SetNextStepAsync`.
2. The command calls `SetAsync(...)` with a `StateValueFactory<T>`.
3. `StateService<T>` serializes updates so one transition runs at a time.
4. Consumers read `State` or subscribe to `StateChanged`.

No mystery ceremony. Just immutable snapshots and predictable transitions.

## API Tour

| Type | What It Does | When To Use It |
| --- | --- | --- |
| `State<T>` | Immutable snapshot with nullable `Value`, `Status`, and `ErrorMessage`. | Return or inspect the latest service state. |
| `StateStatus` | Status enum: `None`, `Busy`, `Ready`, `Error`. | Decide what the UI, caller, or workflow should do next. |
| `StateValueFactory<T>` | Async delegate that creates the next state value. | Wrap the actual work used by `SetAsync`. |
| `IStateService<T>` | Contract for observable state services. | Depend on state behavior without tying callers to a concrete class. |
| `StateService<T>` | Base class that implements state transitions and notifications. | Build your own command-style state service. |
| `IStateService<T>.Empty` | No-op state service that always returns `State<T>.Empty`. | Use as a safe default or placeholder. |

## State Statuses

`StateStatus.None` means nothing has run yet.

`StateStatus.Busy` means a transition is running.

`StateStatus.Ready` means the latest value was produced successfully. `State<T>.Value` is nullable because empty and failed states may not have a reference-type value.

`StateStatus.Error` means the latest transition threw an exception. Check `State<T>.ErrorMessage` for the message.

## Create A State Service

Derive from `StateService<T>` and expose command methods that call `SetAsync`.

```csharp
using TripleG3.Cis;

public sealed record DownloadInfo(string FileName, int PercentComplete);

public sealed class DownloadStateService : StateService<DownloadInfo>
{
    public ValueTask<State<DownloadInfo>> RefreshAsync(CancellationToken cancellationToken)
    {
        return SetAsync(GetDownloadInfoAsync, cancellationToken);
    }

    private static async ValueTask<DownloadInfo> GetDownloadInfoAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(250, cancellationToken);
        return new DownloadInfo("Guide.pdf", 100);
    }
}
```

Use it like this:

```csharp
var service = new DownloadStateService();

service.StateChanged += (_, state) =>
{
    Console.WriteLine($"{state.Status}: {state.Value?.FileName} {state.Value?.PercentComplete}%");
};

State<DownloadInfo> result = await service.RefreshAsync(CancellationToken.None);

if (result is { Status: StateStatus.Ready, Value: { } downloadInfo })
{
    Console.WriteLine($"Done: {downloadInfo.FileName}");
}
```

## Examples Details

The `src\TripleG3.Cis\Examples` path contains a small console-friendly walkthrough. These types are compiled by the console sample and tests, not by the packaged library assembly.

| Example Type | What It Shows | Where It Fits |
| --- | --- | --- |
| `ExampleServiceSteps` | A workflow value that moves from `None` to `Complete`. | The state value type used by the example service. |
| `IExampleApi` | An external step lookup contract. | The dependency `ExampleService` calls to get the next step. |
| `ExampleApi` | A simulated API implementation with a small delay. | The sample external dependency used by the console app. |
| `IExampleService` | A service contract that extends `IStateService<ExampleServiceSteps>`. | The abstraction callers can depend on. |
| `ExampleService` | A concrete service built on `StateService<ExampleServiceSteps>`. | The command implementation that asks `IExampleApi` for the next step. |
| `ExampleServiceWatcher` | A simple console observer for `StateChanged`. | The listener that prints every state transition. |

## Use The Example Service

The `Examples` folder contains a tiny workflow that walks through these values:

```text
None -> Step1 -> Step2 -> Step3 -> Complete
```

```csharp
using TripleG3.Cis.Examples;

var exampleApi = new ExampleApi();
var service = new ExampleService(exampleApi);
var watcher = new ExampleServiceWatcher(service);

await service.SetNextStepAsync(CancellationToken.None);
await service.SetNextStepAsync(CancellationToken.None);
await service.SetNextStepAsync(CancellationToken.None);
await service.SetNextStepAsync(CancellationToken.None);

Console.WriteLine(service.State.Status);
Console.WriteLine(service.State.Value);
```

`ExampleService` delegates step calculation to `IExampleApi`, then lets `StateService<ExampleServiceSteps>` handle the `Busy`, `Ready`, and `Error` transitions. `ExampleServiceWatcher` subscribes to `StateChanged` and writes each update to the console. It is intentionally simple so the state pattern is easy to see.

Console output from running `src\TripleG3.Cis.ConsoleTest\TripleG3.Cis.ConsoleTest.csproj`:

```text
State changed: Busy - None
-----------------------------------------------------
State changed: Ready - Step1
-----------------------------------------------------
State changed: Busy - Step1
-----------------------------------------------------
State changed: Ready - Step2
-----------------------------------------------------
State changed: Busy - Step2
-----------------------------------------------------
State changed: Ready - Step3
-----------------------------------------------------
State changed: Busy - Step3
-----------------------------------------------------
State changed: Ready - Complete
-----------------------------------------------------
```

## Use The Empty Service

`IStateService<T>.Empty` is handy when a caller needs an `IStateService<T>` but there is no real implementation yet.

```csharp
IStateService<string> service = IStateService<string>.Empty;

State<string> state = await service.SetAsync(
    cancellationToken => new ValueTask<string>("This value is ignored."),
    CancellationToken.None);

Console.WriteLine(state.Status); // None
Console.WriteLine(state.Value is null); // True
```

The empty service never invokes the factory and always returns `State<T>.Empty`, whose `Value` is `null` for reference-type state values.

## Tips

- Keep state values small and meaningful. Records work nicely.
- Put business actions in command methods such as `LoadAsync`, `SaveAsync`, or `SetNextStepAsync`.
- Let `SetAsync` handle the transition state instead of setting status manually.
- Subscribe to `StateChanged` when a UI, console, or workflow needs to react.
- Inspect `StateStatus.Error` and `ErrorMessage` when a command fails.

## AI Model Instructions

AI assistants working with this repository should follow these rules:

1. Treat `State<T>` as immutable. Use `with` expressions for changes inside library code.
2. Build new services by deriving from `StateService<T>` and exposing clear command methods.
3. Use `SetAsync(StateValueFactory<T>, CancellationToken)` for state transitions. Do not bypass the base class transition flow.
4. Depend on `IStateService<T>` when writing consumers that only need current state and change notifications.
5. Use `StateChanged` for observation, not polling loops.
6. Keep example code simple, async, and cancellation-token friendly.
7. Preserve the namespaces `TripleG3.Cis` and `TripleG3.Cis.Examples` unless the user explicitly asks for a namespace change.
8. Validate library changes with `dotnet build TripleG3.Cis.slnx` from the repository root.

## Project Layout

```text
src/
  TripleG3.Cis/
        Delegates.cs              StateValueFactory<T>
        IStateService.cs          IStateService<T>
        State.cs                  State<T>
        StateService.cs           StateService<T>
        StateStatus.cs            StateStatus
        Examples/
            ExampleApi.cs           ExampleApi
            ExampleService.cs       ExampleService
            ExampleServiceSteps.cs  ExampleServiceSteps
            ExampleServiceWatcher.cs
            IExampleApi.cs          IExampleApi
            IExampleService.cs
```
