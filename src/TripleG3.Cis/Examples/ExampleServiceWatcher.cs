namespace TripleG3.Cis.Examples;

/// <summary>
/// Writes example service state changes to the console.
/// </summary>
public class ExampleServiceWatcher
{
    private readonly ExampleService exampleService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExampleServiceWatcher"/> class.
    /// </summary>
    /// <param name="exampleService">The example service to observe.</param>
    public ExampleServiceWatcher(ExampleService exampleService)
    {
        this.exampleService = exampleService;
        this.exampleService.StateChanged += OnStateChanged;
    }

    private void OnStateChanged(object? sender, State<ExampleServiceSteps> state)
    {
        Console.WriteLine($"State changed: {state.Status} - {state.Value}");
        Console.WriteLine("-----------------------------------------------------");
        Task.Delay(1000).Wait(); // Simulate some work
    }
}