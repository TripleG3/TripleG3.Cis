namespace TripleG3.Cis.Examples;

public class ExampleService : StateService<ExampleServiceSteps>, IExampleService
{
    public async ValueTask SetNextStepAsync(CancellationToken cancellationToken)
    {
        await SetAsync(SimulateGetStepApiAsync, cancellationToken);
        Console.WriteLine("-----------------------------------------------------");
        await Task.Delay(5000, cancellationToken); // Simulate some work
    }
    private async ValueTask<ExampleServiceSteps> SimulateGetStepApiAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(5000, cancellationToken); // Simulate some work
        return State.Value == ExampleServiceSteps.None ? ExampleServiceSteps.Step1 :
                              State.Value == ExampleServiceSteps.Step1 ? ExampleServiceSteps.Step2 :
                              State.Value == ExampleServiceSteps.Step2 ? ExampleServiceSteps.Step3 :
                              State.Value == ExampleServiceSteps.Step3 ? ExampleServiceSteps.Complete :
                              throw new InvalidOperationException($"Unknown step: {State.Value}");
    }
}
