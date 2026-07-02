namespace TripleG3.Cis;

public record State<T>(T Value, StateStatus Status, string ErrorMessage)
{
    public static State<T> Empty { get; } = new State<T>(default!, StateStatus.None, string.Empty);
}
