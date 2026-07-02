using TripleG3.Cis.Examples;
IExampleService exampleService = new ExampleService();
_ =new ExampleServiceWatcher((ExampleService)exampleService);
await exampleService.SetNextStepAsync(CancellationToken.None);

do 
{
    await exampleService.SetNextStepAsync(CancellationToken.None);
} while (exampleService.State.Value != ExampleServiceSteps.Complete);

Console.ReadLine();