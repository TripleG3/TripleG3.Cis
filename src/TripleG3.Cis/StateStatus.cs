namespace TripleG3.Cis;

/// <summary>
/// Identifies the lifecycle status of a state transition.
/// </summary>
public enum StateStatus
{
    /// <summary>
    /// No transition has started and no value is ready.
    /// </summary>
    None,

    /// <summary>
    /// A state value is currently being produced.
    /// </summary>
    Busy,

    /// <summary>
    /// The state contains a successfully produced value.
    /// </summary>
    Ready,

    /// <summary>
    /// The most recent transition failed.
    /// </summary>
    Error
}
