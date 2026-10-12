using System.Text.Json;
using EventList.Core;
using Xunit;

namespace EventList.Tests;

public class PageCallsTests
{
    /// <summary>A JSON string literal is a JavaScript one, so a text that survives a JSON read survives the page.</summary>
    [Theory]
    [InlineData("var a = 1;")]
    [InlineData("el.setAttribute(\"data-sl-important\", 'crisis');")]
    [InlineData("var re = /CoreUI1\\.html/i;")]
    [InlineData("two\nlines\r\tand a tab")]
    [InlineData("ends with a backslash\\")]
    [InlineData("line\u2028separator\u2029")]
    public void The_page_script_reaches_the_page_exactly_as_written(string script)
    {
        var literal = PageCalls.Quote(script);

        Assert.Equal(script, JsonSerializer.Deserialize<string>(literal));
        Assert.DoesNotContain('\n', literal);
        Assert.DoesNotContain('\u2028', literal);
        Assert.DoesNotContain('\u2029', literal);
    }

    [Fact]
    public void The_install_call_carries_the_whole_script()
    {
        const string script = "(function(){window.__sleventlist={apply:function(){return \"ok\";}};})();\n// last line";

        Assert.Contains(PageCalls.Quote(script), PageCalls.Install(script, ListSettings.From(0, true, true)));
    }

    [Fact]
    public void Every_call_hands_the_page_the_configured_height_and_order()
    {
        var settings = ListSettings.From(150, true, false);
        var expected = "{maxHeight:150,importantOnTop:true,countdownOnTop:false}";

        Assert.Equal(expected, PageCalls.Settings(settings));
        Assert.Contains(expected, PageCalls.Check(settings));
        Assert.Contains(expected, PageCalls.Install("var a;", settings));
    }
}
