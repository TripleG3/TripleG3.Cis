namespace TripleG3.Cis;

/// <summary>
/// Exposes observable state and a controlled way to update it.
/// </summary>
/// <typeparam name="T">The type of value held by the state.</typeparam>
public interface IStateService<T>
{
    /// <summary>
    /// Occurs after the state value, status, or error message changes.
    /// </summary>
    event EventHandler<State<T>> StateChanged;

    /// <summary>
    /// Gets the current state snapshot.
    /// </summary>
    State<T> State { get; }

    /// <summary>
    /// Runs the value factory and updates the state around the transition.
    /// </summary>
    /// <param name="valueFactory">The factory that produces the next state value.</param>
    /// <param name="cancellationToken">A token that cancels the transition.</param>
    /// <returns>The resulting state after the transition completes or fails.</returns>
    ValueTask<State<T>> SetAsync(StateValueFactory<T> valueFactory, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a no-op state service that always returns an empty state.
    /// </summary>
    public static IStateService<T> Empty { get; } = new EmptyStateService();

    private class EmptyStateService : IStateService<T>
    {
        /// <summary>
        /// Occurs after the state changes; this implementation does not raise the event.
        /// </summary>
        public event EventHandler<State<T>> StateChanged = delegate { };

        /// <summary>
        /// Gets the empty state snapshot.
        /// </summary>
        public State<T> State => State<T>.Empty;

        /// <summary>
        /// Returns the empty state without invoking the supplied factory.
        /// </summary>
        /// <param name="valueFactory">The factory that would produce a state value.</param>
        /// <param name="cancellationToken">A token that would cancel value creation.</param>
        /// <returns>The empty state snapshot.</returns>
        public ValueTask<State<T>> SetAsync(StateValueFactory<T> valueFactory, CancellationToken cancellationToken) => new(State<T>.Empty);
    }
}
