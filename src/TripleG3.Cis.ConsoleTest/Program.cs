using TripleG3.Cis.Examples;
var exampleApi = new ExampleApi();
var exampleService = new ExampleService(exampleApi);
_ = new ExampleServiceWatcher(exampleService);

var cancellationTokenSource = new CancellationTokenSource();
do 
{
    await exampleService.SetNextStepAsync(cancellationTokenSource.Token);
} while (exampleService.State.Value != ExampleServiceSteps.Complete);

Console.ReadLine();