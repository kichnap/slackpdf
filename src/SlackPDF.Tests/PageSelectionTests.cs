using SlackPDF.Core.Models;
using Xunit;

namespace SlackPDF.Tests;

public class PageSelectionTests
{
    [Fact]
    public void Parse_Empty_ReturnsAll()
    {
        var sel = PageSelection.Parse("");
        Assert.True(sel.SelectAll);
    }

    [Fact]
    public void Parse_SimpleRange_ReturnsCorrectPages()
    {
        var sel = PageSelection.Parse("1-3, 5, 7-");
        var pages = sel.Resolve(10).ToList();
        Assert.Equal([1, 2, 3, 5, 7, 8, 9, 10], pages);
    }

    [Fact]
    public void Parse_SinglePage_ContainsOnlyThatPage()
    {
        var sel = PageSelection.Parse("5");
        Assert.True(sel.Contains(5));
        Assert.False(sel.Contains(4));
        Assert.False(sel.Contains(6));
    }

    [Fact]
    public void Parse_OpenEndRange_ExtendsToEnd()
    {
        var sel = PageSelection.Parse("8-");
        var pages = sel.Resolve(10).ToList();
        Assert.Equal([8, 9, 10], pages);
    }

    [Fact]
    public void All_ContainsAllPages()
    {
        var sel = PageSelection.All;
        Assert.True(sel.SelectAll);
        Assert.True(sel.Contains(1));
        Assert.True(sel.Contains(999));
    }

    [Fact]
    public void Resolve_All_ReturnsFullRange()
    {
        var pages = PageSelection.All.Resolve(5).ToList();
        Assert.Equal([1, 2, 3, 4, 5], pages);
    }
}
