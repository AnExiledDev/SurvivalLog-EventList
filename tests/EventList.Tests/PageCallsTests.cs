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

        Assert.Contains(PageCalls.Quote(script), PageCalls.Install(script, ListSettings.From(0, true, true, true), PhoneBadge.None));
    }

    [Fact]
    public void Every_call_hands_the_page_the_configured_height_and_order()
    {
        var settings = ListSettings.From(150, true, false, false);
        var expected = "{maxHeight:150,importantOnTop:true,countdownOnTop:false,soonestFirst:false}";

        Assert.Equal(expected, PageCalls.Settings(settings));
        Assert.Contains(expected, PageCalls.Check(settings, PhoneBadge.None));
        Assert.Contains(expected, PageCalls.Install("var a;", settings, PhoneBadge.None));
    }

    [Fact]
    public void Every_call_hands_the_page_how_many_event_texts_wait_and_whether_one_can_run_out()
    {
        var settings = ListSettings.From(0, true, true, true);
        var badge = PhoneBadge.Of(new[] { new EventText("pending", 0), new EventText("pending", 7200), new EventText("pending", 0) });
        var expected = "{waiting:3,timed:true}";

        Assert.Equal(expected, PageCalls.Badge(badge));
        Assert.Contains(expected, PageCalls.Check(settings, badge));
        Assert.Contains(expected, PageCalls.Install("var a;", settings, badge));
    }

    [Fact]
    public void Nothing_waiting_hands_the_page_an_empty_badge()
    {
        Assert.Equal("{waiting:0,timed:false}", PageCalls.Badge(PhoneBadge.None));
    }

    /// <summary>The messages app lives in the phone's own frame, so the call looks it up beside CoreUI1 in Root.html.</summary>
    [Fact]
    public void Every_call_looks_for_the_phone_frame_to_badge_its_messages_app()
    {
        var settings = ListSettings.From(0, true, true, true);

        Assert.Contains("PhoneSMS\\.html", PageCalls.Check(settings, PhoneBadge.None));
        Assert.Contains("PhoneSMS\\.html", PageCalls.Install("var a;", settings, PhoneBadge.None));
    }
}
