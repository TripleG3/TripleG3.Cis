namespace TripleG3.Cis.Tests;

public class ExampleServiceTests
{
    [Fact]
    public async Task SetNextStepAsync_WhenApiReturnsStep_UpdatesStateWithApiResult()
    {
        var api = new TestExampleApi((_, _) => Task.FromResult(ExampleServiceSteps.Step1));
        var service = new ExampleService(api);
        var changes = new List<State<ExampleServiceSteps>>();

        service.StateChanged += (_, state) => changes.Add(state);

        await service.SetNextStepAsync(CancellationToken.None);

        Assert.Collection(
            api.States,
            state =>
            {
                Assert.Equal(StateStatus.Busy, state.Status);
                Assert.Equal(ExampleServiceSteps.None, state.Value);
            });
        Assert.Equal(StateStatus.Ready, service.State.Status);
        Assert.Equal(ExampleServiceSteps.Step1, service.State.Value);
        Assert.Collection(
            changes,
            state => Assert.Equal(StateStatus.Busy, state.Status),
            state =>
            {
                Assert.Equal(StateStatus.Ready, state.Status);
                Assert.Equal(ExampleServiceSteps.Step1, state.Value);
            });
    }

    [Fact]
    public async Task SetNextStepAsync_WhenApiThrows_UpdatesStateWithError()
    {
        var api = new TestExampleApi((_, _) => throw new InvalidOperationException("API down."));
        var service = new ExampleService(api);

        await service.SetNextStepAsync(CancellationToken.None);

        Assert.Equal(StateStatus.Error, service.State.Status);
        Assert.Equal("API down.", service.State.ErrorMessage);
    }
}