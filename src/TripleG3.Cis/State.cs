namespace TripleG3.Cis;

/// <summary>
/// Represents an immutable snapshot of a state service value.
/// </summary>
/// <typeparam name="T">The type of value held by the state.</typeparam>
/// <param name="Value">The current state value. Empty states use the default value for <typeparamref name="T" /> until a value is ready.</param>
/// <param name="Status">The current lifecycle status of the state.</param>
/// <param name="ErrorMessage">The error message from the most recent failed transition, or an empty string.</param>
public record State<T>(T Value, StateStatus Status, string ErrorMessage)
{
    /// <summary>
    /// Gets an initial state with no value, no active transition, and no error message.
    /// </summary>
    public static State<T> Empty { get; } = new State<T>(default!, StateStatus.None, string.Empty);
}
