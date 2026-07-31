namespace TripleG3.Cis.Tests;

public class StateServiceTests
{
    [Fact]
    public void Constructor_WhenDefaultFactoryIsProvided_InitializesValueAndPreservesNoneStatus()
    {
        var service = new TestStateService<string?>(() => "Initial value");

        Assert.Equal("Initial value", service.State.Value);
        Assert.Equal(StateStatus.None, service.State.Status);
        Assert.Empty(service.State.ErrorMessage);
    }

    [Fact]
    public void Constructor_WhenDefaultFactoryIsProvided_InvokesFactoryOnce()
    {
        var factoryCalls = 0;

        _ = new TestStateService<string?>(() =>
        {
            factoryCalls++;
            return "Initial value";
        });

        Assert.Equal(1, factoryCalls);
    }

    [Fact]
    public async Task Constructor_WhenDefaultValueIsProvided_AllowsNormalTransitionsToReplaceIt()
    {
        var service = new TestStateService<string?>(() => "Initial value");

        var result = await service.SetAsync(_ => new ValueTask<string?>("Loaded value"), CancellationToken.None);

        Assert.Equal("Loaded value", result.Value);
        Assert.Equal(StateStatus.Ready, result.Status);
    }

    [Fact]
    public void State_WhenGenericArgumentControlsValueNullability_ExposesMatchingValueType()
    {
        var notNullIntService = new TestStateService<int>();
        var nullIntService = new TestStateService<int?>();

        int notNullValue = notNullIntService.State.Value;
        int? nullValue = nullIntService.State.Value;

        Assert.Equal(0, notNullValue);
        Assert.Null(nullValue);
    }

    [Fact]
    public async Task SetAsync_WhenFactorySucceeds_RaisesBusyThenReady()
    {
        var service = new TestStateService<int>();
        var changes = new List<State<int>>();

        service.StateChanged += (_, state) => changes.Add(state);

        var result = await service.SetAsync(async _ =>
        {
            await Task.Yield();
            return 42;
        }, CancellationToken.None);

        Assert.Equal(StateStatus.Ready, result.Status);
        Assert.Equal(42, result.Value);
        Assert.Equal(result, service.State);
        Assert.Collection(
            changes,
            state => Assert.Equal(StateStatus.Busy, state.Status),
            state =>
            {
                Assert.Equal(StateStatus.Ready, state.Status);
                Assert.Equal(42, state.Value);
            });
    }

    [Fact]
    public async Task SetAsync_WhenFactoryUsesCurrentState_PassesBusySnapshotWithPreviousValue()
    {
        var service = new TestStateService<int>();
        State<int>? observedState = null;

        await service.SetAsync(_ => new ValueTask<int>(41), CancellationToken.None);

        var result = await service.SetAsync((state, _) =>
        {
            observedState = state;
            return Task.FromResult(state.Value + 1);
        }, CancellationToken.None);

        Assert.NotNull(observedState);
        Assert.Equal(StateStatus.Busy, observedState.Status);
        Assert.Equal(41, observedState.Value);
        Assert.Equal(StateStatus.Ready, result.Status);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task SetAsync_WhenFactoryThrows_ReturnsErrorState()
    {
        var service = new TestStateService<int>();

        var result = await service.SetAsync(_ => throw new InvalidOperationException("Boom."), CancellationToken.None);

        Assert.Equal(StateStatus.Error, result.Status);
        Assert.Equal("Boom.", result.ErrorMessage);
        Assert.Equal(result, service.State);
    }

    [Fact]
    public async Task SetAsync_WhenFactoryIsCanceled_ReturnsOperationCanceledErrorState()
    {
        var service = new TestStateService<string>();
        using var cancellationTokenSource = new CancellationTokenSource();

        var result = await service.SetAsync(async cancellationToken =>
        {
            cancellationTokenSource.Cancel();
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            return "Finished";
        }, cancellationTokenSource.Token);

        Assert.Equal(StateStatus.Error, result.Status);
        Assert.Null(result.Value);
        Assert.Equal("Operation canceled.", result.ErrorMessage);
        Assert.Equal(result, service.State);
    }

    [Fact]
    public async Task SetAsync_WhenStateChangedSubscriberThrows_DoesNotFailTransitionOrSkipOtherSubscribers()
    {
        var service = new TestStateService<int>();
        var factoryInvoked = false;
        var observedStates = new List<State<int>>();

        service.StateChanged += (_, _) => throw new InvalidOperationException("Observer failed.");
        service.StateChanged += (_, state) => observedStates.Add(state);

        var result = await service.SetAsync(_ =>
        {
            factoryInvoked = true;
            return new ValueTask<int>(42);
        }, CancellationToken.None);

        Assert.True(factoryInvoked);
        Assert.Equal(StateStatus.Ready, result.Status);
        Assert.Equal(42, result.Value);
        Assert.Equal(result, service.State);
        Assert.Collection(
            observedStates,
            state => Assert.Equal(StateStatus.Busy, state.Status),
            state =>
            {
                Assert.Equal(StateStatus.Ready, state.Status);
                Assert.Equal(42, state.Value);
            });
    }

    [Fact]
    public async Task SetAsync_WhenTransitionsOverlap_RunsOneFactoryAtATime()
    {
        var service = new TestStateService<int>();
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstMayFinish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var firstTask = service.SetAsync(async _ =>
        {
            firstStarted.SetResult();
            await firstMayFinish.Task;
            return 1;
        }, CancellationToken.None).AsTask();

        await firstStarted.Task;

        var secondTask = service.SetAsync(_ =>
        {
            secondStarted.SetResult();
            return new ValueTask<int>(2);
        }, CancellationToken.None).AsTask();

        var secondRanBeforeFirstFinished = await Task.WhenAny(secondStarted.Task, Task.Delay(100)) == secondStarted.Task;
        Assert.False(secondRanBeforeFirstFinished);

        firstMayFinish.SetResult();

        Assert.Equal(1, (await firstTask).Value);
        await secondStarted.Task;
        Assert.Equal(2, (await secondTask).Value);
        Assert.Equal(2, service.State.Value);
    }

    [Fact]
    public async Task SetAsync_WhenWaitingTransitionIsCanceled_KeepsFollowingTransitionSerialized()
    {
        var service = new TestStateService<int>();
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstMayFinish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thirdStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondFactoryInvoked = false;

        var firstTask = service.SetAsync(async _ =>
        {
            firstStarted.SetResult();
            await firstMayFinish.Task;
            return 1;
        }, CancellationToken.None).AsTask();

        await firstStarted.Task;

        using var cancellationTokenSource = new CancellationTokenSource();
        var secondTask = service.SetAsync(_ =>
        {
            secondFactoryInvoked = true;
            return new ValueTask<int>(2);
        }, cancellationTokenSource.Token).AsTask();

        cancellationTokenSource.Cancel();

        var secondResult = await secondTask;

        Assert.False(secondFactoryInvoked);
        Assert.Equal(StateStatus.Error, secondResult.Status);

        var thirdTask = service.SetAsync(_ =>
        {
            thirdStarted.SetResult();
            return new ValueTask<int>(3);
        }, CancellationToken.None).AsTask();

        var thirdRanBeforeFirstFinished = await Task.WhenAny(thirdStarted.Task, Task.Delay(100)) == thirdStarted.Task;
        Assert.False(thirdRanBeforeFirstFinished);

        firstMayFinish.SetResult();

        Assert.Equal(1, (await firstTask).Value);
        await thirdStarted.Task;
        Assert.Equal(3, (await thirdTask).Value);
        Assert.Equal(3, service.State.Value);
    }

    [Fact]
    public async Task SetAsync_WhenCancellationTokenIsAlreadyCanceled_ReturnsErrorStateWithoutInvokingFactory()
    {
        var service = new TestStateService<int>();
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        var factoryInvoked = false;

        var result = await service.SetAsync(_ =>
        {
            factoryInvoked = true;
            return new ValueTask<int>(42);
        }, cancellationTokenSource.Token);

        Assert.False(factoryInvoked);
        Assert.Equal(StateStatus.Error, result.Status);
        Assert.Equal(result, service.State);
    }

    [Fact]
    public async Task Empty_WhenSetAsyncIsCalled_ReturnsEmptyStateWithoutInvokingFactory()
    {
        var service = IStateService<int>.Empty;
        var factoryInvoked = false;

        var result = await service.SetAsync(_ =>
        {
            factoryInvoked = true;
            return new ValueTask<int>(42);
        }, CancellationToken.None);

        Assert.False(factoryInvoked);
        Assert.Equal(State<int>.Empty, result);
        Assert.Equal(State<int>.Empty, service.State);
    }

    [Fact]
    public async Task SetAsync_WhenNullableValueTypeFactoryReturnsNull_AllowsNullValue()
    {
        var service = new TestStateService<int?>();
        State<int?> initialState = service.State;

        var result = await service.SetAsync(_ => new ValueTask<int?>((int?)null), CancellationToken.None);

        Assert.Null(initialState.Value);
        Assert.Equal(StateStatus.Ready, result.Status);
        Assert.Null(result.Value);
        Assert.Equal(result, service.State);
    }

    [Fact]
    public async Task Empty_WhenNullableValueTypeSetAsyncIsCalled_ReturnsNullableEmptyStateWithoutInvokingFactory()
    {
        var service = IStateService<int?>.Empty;
        var factoryInvoked = false;

        var result = await service.SetAsync(_ =>
        {
            factoryInvoked = true;
            return new ValueTask<int?>(42);
        }, CancellationToken.None);

        Assert.False(factoryInvoked);
        Assert.Equal(State<int?>.Empty, result);
        Assert.Equal(State<int?>.Empty, service.State);
    }

    private sealed class TestStateService<T> : StateService<T>
    {
        public TestStateService()
        {
        }

        public TestStateService(Func<T> defaultValueFactory)
            : base(defaultValueFactory)
        {
        }
    }
}