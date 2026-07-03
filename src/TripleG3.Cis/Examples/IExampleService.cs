namespace TripleG3.Cis.Examples;

/// <summary>
/// Provides example workflow state transitions.
/// </summary>
public interface IExampleService : IStateService<ExampleServiceSteps>
{
    /// <summary>
    /// Advances the example workflow to the next step.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the step transition.</param>
    /// <returns>The state after the step transition has finished.</returns>
    ValueTask<State<ExampleServiceSteps>> SetNextStepAsync(CancellationToken cancellationToken);
}
