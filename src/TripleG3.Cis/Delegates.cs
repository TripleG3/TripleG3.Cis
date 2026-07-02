namespace TripleG3.Cis;

/// <summary>
/// Creates a state value asynchronously for a state transition.
/// </summary>
/// <typeparam name="T">The type of value held by the state.</typeparam>
/// <param name="cancellationToken">A token that cancels value creation.</param>
/// <returns>The value to store in the state.</returns>
public delegate ValueTask<T> StateValueFactory<T>(CancellationToken cancellationToken);