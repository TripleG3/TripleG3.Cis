namespace TripleG3.Cis.Examples;

/// <summary>
/// Demonstrates a state service that advances through sample workflow steps.
/// </summary>
public class ExampleService : StateService<ExampleServiceSteps>, IExampleService
{
    /// <summary>
    /// Advances the example workflow to the next step.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the step transition.</param>
    /// <returns>A value task that completes when the step transition has finished.</returns>
    public async ValueTask SetNextStepAsync(CancellationToken cancellationToken)
    {
        await SetAsync(SimulateGetStepApiAsync, cancellationToken);
    }
    private async ValueTask<ExampleServiceSteps> SimulateGetStepApiAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(1000, cancellationToken); // Simulate some work
        return State.Value == ExampleServiceSteps.None ? ExampleServiceSteps.Step1 :
                              State.Value == ExampleServiceSteps.Step1 ? ExampleServiceSteps.Step2 :
                              State.Value == ExampleServiceSteps.Step2 ? ExampleServiceSteps.Step3 :
                              State.Value == ExampleServiceSteps.Step3 ? ExampleServiceSteps.Complete :
                              throw new InvalidOperationException($"Unknown step: {State.Value}");
    }
}
