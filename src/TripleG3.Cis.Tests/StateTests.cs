namespace TripleG3.Cis.Tests;

public class StateTests
{
    [Fact]
    public void Empty_WhenReferenceTypeState_HasNullValue()
    {
        var state = State<string>.Empty;

        Assert.Null(state.Value);
        Assert.Equal(StateStatus.None, state.Status);
        Assert.Equal(string.Empty, state.ErrorMessage);
    }

    [Fact]
    public void Empty_WhenValueTypeState_HasDefaultValue()
    {
        var state = State<int>.Empty;

        Assert.Equal(0, state.Value);
        Assert.Equal(StateStatus.None, state.Status);
        Assert.Equal(string.Empty, state.ErrorMessage);
    }

    [Fact]
    public void Empty_WhenNullableValueTypeState_HasNullValue()
    {
        var state = State<int?>.Empty;

        Assert.Null(state.Value);
        Assert.Equal(StateStatus.None, state.Status);
        Assert.Equal(string.Empty, state.ErrorMessage);
    }
}