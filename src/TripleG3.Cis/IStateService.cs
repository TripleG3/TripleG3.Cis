namespace TripleG3.Cis;

public interface IStateService<T>
{
    event EventHandler<State<T>> StateChanged;
    State<T> State { get; }
    ValueTask<State<T>> SetAsync(StateValueFactory<T> valueFactory, CancellationToken cancellationToken);
    public static IStateService<T> Empty { get; } = new EmptyStateService();
    private class EmptyStateService : IStateService<T>
    {
        public event EventHandler<State<T>> StateChanged = delegate { };
        public State<T> State => State<T>.Empty;
        public ValueTask<State<T>> SetAsync(StateValueFactory<T> valueFactory, CancellationToken cancellationToken) => new(State<T>.Empty);
    }
}
