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

        Assert.False(secondStarted.Task.IsCompleted);
        Assert.False(secondTask.IsCompleted);

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
        var changes = new List<State<int>>();
        service.StateChanged += (_, state) => changes.Add(state);

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
        Assert.Equal("Operation canceled.", secondResult.ErrorMessage);
        Assert.False(firstTask.IsCompleted);
        Assert.Equal(StateStatus.Busy, service.State.Status);
        Assert.Empty(service.State.ErrorMessage);
        Assert.Single(changes);

        var thirdTask = service.SetAsync(_ =>
        {
            thirdStarted.SetResult();
            return new ValueTask<int>(3);
        }, CancellationToken.None).AsTask();

        Assert.False(thirdStarted.Task.IsCompleted);
        Assert.False(thirdTask.IsCompleted);

        firstMayFinish.SetResult();

        Assert.Equal(1, (await firstTask).Value);
        await thirdStarted.Task;
        Assert.Equal(3, (await thirdTask).Value);
        Assert.Equal(3, service.State.Value);
        Assert.Empty(service.State.ErrorMessage);
        Assert.Equal(new[] { StateStatus.Busy, StateStatus.Ready, StateStatus.Busy, StateStatus.Ready },
            changes.Select(state => state.Status));
    }

    [Fact]
    public async Task SetAsync_WhenCancellationTokenIsAlreadyCanceled_ReturnsUnpublishedErrorWithoutInvokingFactory()
    {
        var service = new TestStateService<int>();
        await service.SetAsync(_ => new ValueTask<int>(41), CancellationToken.None);
        var originalState = service.State;
        var changes = new List<State<int>>();
        service.StateChanged += (_, state) => changes.Add(state);
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
        Assert.Equal("Operation canceled.", result.ErrorMessage);
        Assert.Equal(41, result.Value);
        Assert.Same(originalState, service.State);
        Assert.Empty(changes);
        var laterResult = await service.SetAsync(_ => new ValueTask<int>(42), CancellationToken.None);
        Assert.Equal(StateStatus.Ready, laterResult.Status);
        Assert.Equal(42, laterResult.Value);
    }

    [Theory]
    [InlineData("success", StateStatus.Ready, "")]
    [InlineData("failure", StateStatus.Error, "Active failed.")]
    [InlineData("cancellation", StateStatus.Error, "Operation canceled.")]
    public async Task SetAsync_WhenWaiterIsCanceled_OnlyActiveTransitionPublishesOutcome(
        string outcome, StateStatus expectedStatus, string expectedError)
    {
        var service = new TestStateService<int>();
        await service.SetAsync(_ => new ValueTask<int>(41), CancellationToken.None);
        var changes = new List<State<int>>();
        service.StateChanged += (_, state) => changes.Add(state);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var mayFinish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var activeCancellation = new CancellationTokenSource();
        using var queuedCancellation = new CancellationTokenSource();
        var queuedFactoryInvoked = false;

        var activeTask = service.SetAsync(async token =>
        {
            started.SetResult();
            await mayFinish.Task;
            token.ThrowIfCancellationRequested();
            if (outcome == "failure") throw new InvalidOperationException("Active failed.");
            return 42;
        }, activeCancellation.Token).AsTask();
        await started.Task;
        var busyState = service.State;

        var queuedTask = service.SetAsync(_ =>
        {
            queuedFactoryInvoked = true;
            return new ValueTask<int>(99);
        }, queuedCancellation.Token).AsTask();
        Assert.False(queuedTask.IsCompleted);
        queuedCancellation.Cancel();
        var queuedResult = await queuedTask;

        Assert.False(queuedFactoryInvoked);
        Assert.False(activeTask.IsCompleted);
        Assert.Same(busyState, service.State);
        Assert.Equal(StateStatus.Error, queuedResult.Status);
        Assert.Equal("Operation canceled.", queuedResult.ErrorMessage);
        Assert.Equal(41, queuedResult.Value);
        Assert.Single(changes);

        if (outcome == "cancellation") activeCancellation.Cancel();
        mayFinish.SetResult();
        var result = await activeTask;

        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(expectedError, result.ErrorMessage);
        Assert.Equal(outcome == "success" ? 42 : 41, result.Value);
        Assert.Equal(result, service.State);
        Assert.Equal(new[] { StateStatus.Busy, expectedStatus }, changes.Select(state => state.Status));

        var laterResult = await service.SetAsync(_ => new ValueTask<int>(100), CancellationToken.None);
        Assert.Equal(StateStatus.Ready, laterResult.Status);
        Assert.Equal(100, laterResult.Value);
        Assert.Empty(laterResult.ErrorMessage);
    }

    [Fact]
    public async Task SetAsync_WhenSomeWaitersAreCanceled_RemainingCommandsStillExecute()
    {
        var service = new TestStateService<int>();
        var mayFinish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var changes = new List<State<int>>();
        service.StateChanged += (_, state) => changes.Add(state);
        var activeTask = service.SetAsync(async _ =>
        {
            await mayFinish.Task;
            return 42;
        }, CancellationToken.None).AsTask();
        using var firstCancellation = new CancellationTokenSource();
        using var secondCancellation = new CancellationTokenSource();
        var canceledFactoryCalls = 0;
        StateValueFactory<int> canceledFactory = _ =>
        {
            Interlocked.Increment(ref canceledFactoryCalls);
            return new ValueTask<int>(99);
        };
        var firstCanceled = service.SetAsync(canceledFactory, firstCancellation.Token).AsTask();
        var survivingFactoryCalls = 0;
        var survivingTask = service.SetAsync(_ =>
        {
            Interlocked.Increment(ref survivingFactoryCalls);
            return new ValueTask<int>(100);
        }, CancellationToken.None).AsTask();
        var secondCanceled = service.SetAsync(canceledFactory, secondCancellation.Token).AsTask();

        firstCancellation.Cancel();
        secondCancellation.Cancel();
        Assert.Equal(StateStatus.Error, (await firstCanceled).Status);
        Assert.Equal(StateStatus.Error, (await secondCanceled).Status);
        Assert.Equal(0, canceledFactoryCalls);
        Assert.Equal(0, survivingFactoryCalls);
        Assert.False(survivingTask.IsCompleted);
        Assert.Equal(StateStatus.Busy, service.State.Status);
        Assert.Single(changes);

        mayFinish.SetResult();
        Assert.Equal(42, (await activeTask).Value);
        Assert.Equal(100, (await survivingTask).Value);
        Assert.Equal(1, survivingFactoryCalls);
        Assert.Equal(0, canceledFactoryCalls);
        Assert.Equal(new[] { StateStatus.Busy, StateStatus.Ready, StateStatus.Busy, StateStatus.Ready },
            changes.Select(state => state.Status));
    }

    [Fact]
    public async Task SetAsync_WhenObserverCancelsRequestAndSchedulesFollowUp_PreservesTransitions()
    {
        var service = new TestStateService<int>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Task<State<int>>? canceledTask = null;
        Task<State<int>>? followUpTask = null;
        State<int>? factoryState = null;
        var changes = new List<State<int>>();
        var canceledFactoryInvoked = false;
        var scheduled = false;
        service.StateChanged += (_, _) => throw new InvalidOperationException("Observer failed.");
        service.StateChanged += (_, state) =>
        {
            if (state.Status != StateStatus.Busy || scheduled) return;
            scheduled = true;
            canceledTask = service.SetAsync(_ =>
            {
                canceledFactoryInvoked = true;
                return new ValueTask<int>(99);
            }, cancellation.Token).AsTask();
            followUpTask = service.SetAsync(_ => new ValueTask<int>(43), CancellationToken.None).AsTask();
        };
        service.StateChanged += (_, state) => changes.Add(state);

        var result = await service.SetAsync((state, _) =>
        {
            factoryState = state;
            return Task.FromResult(42);
        }, CancellationToken.None);

        Assert.NotNull(canceledTask);
        Assert.NotNull(followUpTask);
        Assert.NotNull(factoryState);
        Assert.Equal(StateStatus.Busy, factoryState.Status);
        Assert.Equal(StateStatus.Error, (await canceledTask).Status);
        Assert.False(canceledFactoryInvoked);
        Assert.Equal(42, result.Value);
        Assert.Equal(43, (await followUpTask).Value);
        Assert.Equal(new[] { StateStatus.Busy, StateStatus.Ready, StateStatus.Busy, StateStatus.Ready },
            changes.Select(state => state.Status));
        Assert.Empty(service.State.ErrorMessage);
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