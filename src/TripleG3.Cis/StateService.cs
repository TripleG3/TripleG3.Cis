namespace TripleG3.Cis;

/// <summary>
/// Provides serialized state transition behavior for state services.
/// </summary>
/// <typeparam name="T">The type of value held by the state.</typeparam>
public abstract class StateService<T> : IStateService<T>
{
    private readonly SemaphoreSlim semaphoreSlim = new(1, 1);
    private State<T> state = State<T>.Empty;

    /// <summary>
    /// Occurs after the state value, status, or error message changes. Subscriber exceptions are isolated from state transitions.
    /// </summary>
    public event EventHandler<State<T>> StateChanged = delegate { };
    
    /// <summary>
    /// Gets the current state snapshot.
    /// </summary>
    public State<T> State
    {
        get => state;
        private set
        {
            if (state.Equals(value)) return;
            state = value;
            RaiseStateChanged(state);
        }
    }

    private void RaiseStateChanged(State<T> value)
    {
        foreach (EventHandler<State<T>> handler in StateChanged.GetInvocationList().Cast<EventHandler<State<T>>>())
        {
            try
            {
                handler.Invoke(this, value);
            }
            catch
            {
            }
        }
    }

    /// <summary>
    /// Runs the asynchronous value factory one transition at a time and updates the state status.
    /// </summary>
    /// <param name="factory">The factory that produces the next state value. Must be a <see cref="StateValueFactory{T}"/>.</param>
    /// <param name="cancellationToken">A token that cancels waiting for the transition.</param>
    /// <returns>The resulting <see cref="State{T}"/> after the transition completes or fails.</returns>
    public async ValueTask<State<T>> SetAsync(StateValueFactory<T> factory, CancellationToken cancellationToken)
    {
        try
        {
            await semaphoreSlim.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            State = State with { Status = StateStatus.Error, ErrorMessage = "Operation canceled." };
            return State;
        }
        catch (ObjectDisposedException ex) when (ex.ObjectName == nameof(SemaphoreSlim))
        {
            State = State with { Status = StateStatus.Error, ErrorMessage = ex.Message };
            return State;
        }
        
        try
        {
            State = State with { Status = StateStatus.Busy, ErrorMessage = string.Empty };
            var value = await factory(cancellationToken);
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
