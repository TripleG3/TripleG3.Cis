namespace TripleG3.Cis.Examples;

public interface IExampleService : IStateService<ExampleServiceSteps>
{
    ValueTask SetNextStepAsync(CancellationToken cancellationToken);
}
