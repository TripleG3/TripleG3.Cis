namespace TripleG3.Cis;

/// <summary>
/// Represents an immutable snapshot of a state service value.
/// </summary>
/// <typeparam name="T">The type of value held by the state.</typeparam>
/// <param name="Value">The current state value, or <see langword="null" /> when no value is available.</param>
/// <param name="Status">The current lifecycle status of the state.</param>
/// <param name="ErrorMessage">The error message from the most recent failed transition, or an empty string.</param>
public record State<T>(T? Value, StateStatus Status, string ErrorMessage)
{
    /// <summary>
    /// Gets the current state value, or <see langword="null" /> when no value is available.
    /// </summary>
    public T? Value { get; init; } = Value;

    /// <summary>
    /// Gets the current lifecycle status of the state.
    /// </summary>
    public StateStatus Status { get; init; } = Status;

    /// <summary>
    /// Gets the error message from the most recent failed transition, or an empty string.
    /// </summary>
    public string ErrorMessage { get; init; } = ErrorMessage;

    /// <summary>
    /// Gets an initial state with no value, no active transition, and no error message.
    /// </summary>
    public static State<T> Empty { get; } = new State<T>(default, StateStatus.None, string.Empty);
}
