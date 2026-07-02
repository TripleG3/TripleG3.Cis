namespace TripleG3.Cis.Examples;

/// <summary>
/// Demonstrates a state service that advances through sample workflow steps.
/// </summary>
public sealed class ExampleService(IExampleApi exampleApi) : StateService<ExampleServiceSteps>, IExampleService
{
    /// <summary>
    /// Advances the example workflow to the next step.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the step transition.</param>
    /// <returns>A value task that completes when the step transition has finished.</returns>
    public async ValueTask SetNextStepAsync(CancellationToken cancellationToken)
    {
        await SetAsync(async ct => await exampleApi.GetStepApiAsync(State, ct), cancellationToken);
    }
}
