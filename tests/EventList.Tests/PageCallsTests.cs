using EventList.Core;
using ExiledToolkit.Web;
using Xunit;

namespace EventList.Tests;

public class PageCallsTests
{
    [Fact]
    public void The_install_call_carries_the_whole_script()
    {
        const string script = "(function(){window.__sleventlist={apply:function(){return \"ok\";}};})();\n// last line";

        Assert.Contains(JsText.Literal(script), PageCalls.Install(script, ListSettings.From(0, true, true, true), PhoneBadge.None));
    }

    [Fact]
    public void Every_call_hands_the_page_the_configured_height_and_order()
    {
        var settings = ListSettings.From(150, true, false, false);
        var expected = "{maxHeight:150,importantOnTop:true,countdownOnTop:false,soonestFirst:false,layout:'groups',zh:false}";

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

    [Fact]
    public void The_lanes_layout_and_the_game_s_language_reach_the_page()
    {
        var settings = ListSettings.From(150, true, true, true, ListLayout.Lanes, true);

        Assert.Equal("{maxHeight:150,importantOnTop:true,countdownOnTop:true,soonestFirst:true,layout:'lanes',zh:true}", PageCalls.Settings(settings));
    }

    [Fact]
    public void Every_call_hands_the_lanes_what_the_plugin_read_about_the_running_events()
    {
        var settings = ListSettings.From(0, true, true, true, ListLayout.Lanes, false);
        const string facts = "{\"7\":{total:600,needs:[]}}";

        Assert.Contains(",phone," + facts + "))", PageCalls.Check(settings, PhoneBadge.None, facts));
        Assert.Contains(",phone," + facts + "))", PageCalls.Install("var a;", settings, PhoneBadge.None, facts));
        Assert.Contains(",phone," + LaneFacts.None + "))", PageCalls.Check(settings, PhoneBadge.None));
    }

    [Fact]
    public void The_check_call_is_built_again_when_the_lanes_facts_change()
    {
        var check = new CheckCall();
        var settings = ListSettings.From(0, true, true, true, ListLayout.Lanes, false);
        var first = check.For(settings, PhoneBadge.None, "{\"7\":{total:600,needs:[]}}");

        Assert.Same(first, check.For(settings, PhoneBadge.None, "{\"7\":{total:600,needs:[]}}"));
        Assert.Equal(PageCalls.Check(settings, PhoneBadge.None, LaneFacts.None), check.For(settings, PhoneBadge.None, LaneFacts.None));
    }

    /// <summary>The check call goes every couple of seconds; it is built only when what it carries changed.</summary>
    [Fact]
    public void The_check_call_is_built_again_only_when_the_settings_or_the_badge_change()
    {
        var check = new CheckCall();
        var settings = ListSettings.From(0, true, true, true);
        var waiting = PhoneBadge.Of(new[] { new EventText(PhoneBadge.WaitingState, 0) });
        var first = check.For(settings, waiting);

        Assert.Equal(PageCalls.Check(settings, waiting), first);
        Assert.Same(first, check.For(ListSettings.From(0, true, true, true), PhoneBadge.Of(new[] { new EventText(PhoneBadge.WaitingState, 0) })));

        var timed = PhoneBadge.Of(new[] { new EventText(PhoneBadge.WaitingState, 500) });

        Assert.Equal(PageCalls.Check(settings, timed), check.For(settings, timed));

        var other = ListSettings.From(0, false, true, true);

        Assert.Equal(PageCalls.Check(other, timed), check.For(other, timed));
    }
}
