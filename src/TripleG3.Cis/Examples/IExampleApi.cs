namespace TripleG3.Cis.Examples;

/// <summary>
/// Provides the external step lookup used by the example state service.
/// </summary>
public interface IExampleApi
{
    /// <summary>
    /// Gets the next example workflow step for the supplied state snapshot.
    /// </summary>
    /// <param name="state">The current example workflow state.</param>
    /// <param name="cancellationToken">A token that cancels the simulated API call.</param>
    /// <returns>The next example workflow step.</returns>
    Task<ExampleServiceSteps> GetStepApiAsync(State<ExampleServiceSteps> state, CancellationToken cancellationToken);
}
