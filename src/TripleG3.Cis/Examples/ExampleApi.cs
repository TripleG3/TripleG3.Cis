namespace TripleG3.Cis.Examples;

/// <summary>
/// Simulates an external API that calculates the next example workflow step.
/// </summary>
public sealed class ExampleApi : IExampleApi
{
    /// <summary>
    /// Gets the next example workflow step for the supplied state snapshot.
    /// </summary>
    /// <param name="state">The current example workflow state.</param>
    /// <param name="cancellationToken">A token that cancels the simulated API call.</param>
    /// <returns>The next example workflow step.</returns>
    public async Task<ExampleServiceSteps> GetStepApiAsync(State<ExampleServiceSteps> state, CancellationToken cancellationToken)
    {
        await Task.Delay(1000, cancellationToken); // Simulate some work
        return state.Value == ExampleServiceSteps.None ? ExampleServiceSteps.Step1 :
                              state.Value == ExampleServiceSteps.Step1 ? ExampleServiceSteps.Step2 :
                              state.Value == ExampleServiceSteps.Step2 ? ExampleServiceSteps.Step3 :
                              state.Value == ExampleServiceSteps.Step3 ? ExampleServiceSteps.Complete :
                              throw new InvalidOperationException($"Unknown step: {state.Value}");
    }
}
