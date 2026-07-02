namespace TripleG3.Cis.Examples;

public class ExampleServiceWatcher
{
    private readonly ExampleService exampleService;
    public ExampleServiceWatcher(ExampleService exampleService)
    {
        this.exampleService = exampleService;
        this.exampleService.StateChanged += OnStateChanged;
    }

    private void OnStateChanged(object? sender, State<ExampleServiceSteps> state)
    {
        Console.WriteLine($"State changed: {state.Status} - {state.Value}");
        switch (state.Status)
        {
            case StateStatus.Busy:
                switch (state.Value)
                {
                    case ExampleServiceSteps.None:
                        Console.WriteLine("Step1 step has started.");
                        break;
                    case ExampleServiceSteps.Step1:
                        Console.WriteLine("Step2 has started.");
                        break;
                    case ExampleServiceSteps.Step2:
                        Console.WriteLine("Step3 has started.");
                        break;
                    case ExampleServiceSteps.Step3:
                        Console.WriteLine("Step4 has started.");
                        break;
                    case ExampleServiceSteps.Complete:
                        Console.WriteLine("Service has completed all steps.");
                        break;
                }
                break;
            case StateStatus.Error:
                Console.WriteLine($"Service encountered an error: {state.ErrorMessage}");
                break;
            case StateStatus.Ready:
                switch (state.Value)
                {
                    case ExampleServiceSteps.Step1:
                        Console.WriteLine("Service has ended Step1.");
                        break;
                    case ExampleServiceSteps.Step2:
                        Console.WriteLine("Service has ended Step2.");
                        break;
                    case ExampleServiceSteps.Step3:
                        Console.WriteLine("Service has ended Step3.");
                        break;
                    case ExampleServiceSteps.Complete:
                        Console.WriteLine("Service has completed all steps.");
                        break;
                }
                break;
        }
    }
}