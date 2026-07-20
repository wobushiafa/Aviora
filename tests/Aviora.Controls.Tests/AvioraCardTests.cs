namespace Aviora.Controls.Tests;

public class AvioraCardTests
{
    [Fact]
    public void Content_can_be_assigned()
    {
        var card = new AvioraCard { Content = "Aviora" };

        Assert.Equal("Aviora", card.Content);
    }
}
