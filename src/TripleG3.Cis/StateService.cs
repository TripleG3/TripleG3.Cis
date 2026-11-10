namespace TripleG3.Cis;

/// <summary>
/// Provides serialized state transition behavior for state services and can be used directly or as a base class.
/// </summary>
/// <typeparam name="T">The type of value held by the state.</typeparam>
public abstract class StateService<T> : IStateService<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StateService{T}"/> class with the default empty state.
    /// </summary>
    protected StateService() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="StateService{T}"/> class with the specified default value factory.
    /// </summary>
    /// <param name="defaultValueFactory">A function that produces the default value for the state.</param>
    protected StateService(Func<T> defaultValueFactory) 
    { 
        State = State with { Value = defaultValueFactory() };
    }

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
        foreach (EventHandler<State<T>> handler in StateChanged.GetInvocationList().Cast<EventHandler<State<T>>>() )
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
    /// Runs an asynchronous value factory that receives the current state snapshot.
    /// </summary>
    /// <param name="factory">The factory that receives the current state snapshot and produces the next state value.</param>
    /// <param name="cancellationToken">A token that cancels waiting for the transition or creating the next state value.</param>
    /// <returns>The resulting <see cref="State{T}"/> after the transition completes or fails.</returns>
    /// <remarks>Cancellation before acquisition returns an unpublished error snapshot without changing shared state or raising <see cref="StateChanged"/>.</remarks>
    public ValueTask<State<T>> SetAsync(Func<State<T>, CancellationToken, Task<T>> factory, CancellationToken cancellationToken)
    {
        return SetAsync(ct => new ValueTask<T>(factory(State, ct)), cancellationToken);
    }

    /// <summary>
    /// Runs the asynchronous value factory one transition at a time and updates the state status.
    /// </summary>
    /// <param name="factory">The factory that produces the next state value. Must be a <see cref="StateValueFactory{T}"/>.</param>
    /// <param name="cancellationToken">A token that cancels waiting for the transition or creating the next state value.</param>
    /// <returns>The resulting <see cref="State{T}"/> after the transition completes or fails.</returns>
    /// <remarks>
    /// Only the transition that acquires the update pipeline changes shared state.
    /// Cancellation before acquisition returns an unpublished error snapshot without invoking the factory or raising <see cref="StateChanged"/>.
    /// Cancellation thrown by an acquired factory is published as an error transition.
    /// </remarks>
    public async ValueTask<State<T>> SetAsync(StateValueFactory<T> factory, CancellationToken cancellationToken)
    {
        try
        {
            await semaphoreSlim.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            return State with { Status = StateStatus.Error, ErrorMessage = "Operation canceled." };
        }
        catch (ObjectDisposedException ex) when (ex.ObjectName == nameof(SemaphoreSlim))
        {
            return State with { Status = StateStatus.Error, ErrorMessage = ex.Message };
        }
        
        try
        {
            State = State with { Status = StateStatus.Busy, ErrorMessage = string.Empty };
            var value = await factory(cancellationToken);
            State = State with { Value = value, Status = StateStatus.Ready, ErrorMessage = string.Empty };
            return State;
        }
        catch (OperationCanceledException)
        {
            State = State with { Status = StateStatus.Error, ErrorMessage = "Operation canceled." };
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
