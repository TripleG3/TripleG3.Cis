namespace TripleG3.Cis.Tests;

public class ExampleApiTests
{
    [Fact]
    public async Task GetStepApiAsync_WhenStateIsNone_ReturnsStep1()
    {
        var api = new ExampleApi();

        var result = await api.GetStepApiAsync(State<ExampleServiceSteps>.Empty, CancellationToken.None);

        Assert.Equal(ExampleServiceSteps.Step1, result);
    }
}