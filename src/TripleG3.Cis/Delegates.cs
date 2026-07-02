namespace TripleG3.Cis;

public delegate ValueTask<T> StateValueFactory<T>(CancellationToken cancellationToken);