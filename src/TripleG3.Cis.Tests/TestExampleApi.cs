namespace TripleG3.Cis.Tests;

internal sealed class TestExampleApi : IExampleApi
{
    private readonly Func<State<ExampleServiceSteps>, CancellationToken, Task<ExampleServiceSteps>> getStepAsync;
    private readonly List<State<ExampleServiceSteps>> states = new List<State<ExampleServiceSteps>>();

    public TestExampleApi(Func<State<ExampleServiceSteps>, CancellationToken, Task<ExampleServiceSteps>> getStepAsync)
    {
        this.getStepAsync = getStepAsync;
    }

    public IReadOnlyList<State<ExampleServiceSteps>> States => states;

    public Task<ExampleServiceSteps> GetStepApiAsync(State<ExampleServiceSteps> state, CancellationToken cancellationToken)
    {
        states.Add(state);
        return getStepAsync(state, cancellationToken);
    }
}