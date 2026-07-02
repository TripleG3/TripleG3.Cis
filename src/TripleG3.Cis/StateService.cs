namespace TripleG3.Cis;

public abstract class StateService<T> : IStateService<T>
{
    private readonly SemaphoreSlim semaphoreSlim = new(1, 1);
    private State<T> state = State<T>.Empty;
    public event EventHandler<State<T>> StateChanged = delegate { };
    public State<T> State
    {
        get => state;
        private set
        {
            if (state.Equals(value)) return;
            state = value;
            StateChanged.Invoke(this, state);
        }
    }
    public async ValueTask<State<T>> SetAsync(StateValueFactory<T> factory, CancellationToken cancellationToken)
    {
        try
        {
            await semaphoreSlim.WaitAsync(cancellationToken);            
            var factoryTask = factory(cancellationToken);
            State = State with { Status = StateStatus.Busy };
            var value = await factoryTask;
            State = State with { Value = value, Status = StateStatus.Ready };
            return State;
        }
        catch (Exception ex)
        {
            State = State with { Status = StateStatus.Error, ErrorMessage = ex.Message };
            return State;
        }
        finally
        {
            semaphoreSlim.Release();
        }
    }
}
