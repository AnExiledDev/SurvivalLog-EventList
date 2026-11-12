using EventList.Core;
using ExiledToolkit.Web;
using Xunit;

namespace EventList.Tests;

/// <summary>What the plugin tells the lanes about each running event, as the literal the page reads.</summary>
public class LaneFactsTests
{
    private const string Water = "Icon/Item/water.png";

    [Fact]
    public void No_running_events_send_no_facts()
    {
        var facts = new LaneFacts();

        facts.Begin();

        Assert.Equal(LaneFacts.None, facts.End());
    }

    [Fact]
    public void Each_event_sends_its_countdown_length_and_the_items_it_asks_for()
    {
        var facts = new LaneFacts();

        facts.Begin();
        facts.Event(202, 21600);
        facts.Need(Water, 2, 1);
        facts.Need("", 1, 0);
        facts.Event(203, 43200);

        var expected = "{\"202\":{total:21600,needs:[[" + JsText.Literal(Water) + ",2,1],[" + JsText.Literal("") + ",1,0]]},\"203\":{total:43200,needs:[]}}";

        Assert.Equal(expected, facts.End());
    }

    /// <summary>The page reads the literal with the same rules the lanes draw from, so the two cannot drift apart.</summary>
    [Fact]
    public void The_page_reads_the_facts_the_plugin_sends()
    {
        var facts = new LaneFacts();
        var page = PageScript.Alone();

        facts.Begin();
        facts.Event(202, 21600);
        facts.Need(Water, 2, 1);

        var literal = facts.End();

        Assert.Equal(360.0, page.Number($"window.__sleventlistLanes.rules.factOf({literal}, 202).totalMinutes"));
        Assert.Equal(Water, page.Text($"window.__sleventlistLanes.rules.factOf({literal}, 202).needs[0].icon"));
        Assert.Equal(2.0, page.Number($"window.__sleventlistLanes.rules.factOf({literal}, 202).needs[0].need"));
        Assert.Equal(1.0, page.Number($"window.__sleventlistLanes.rules.factOf({literal}, 202).needs[0].have"));
    }

    /// <summary>The plugin sends the facts every couple of seconds; the literal is built again only when they change.</summary>
    [Fact]
    public void The_same_facts_give_back_the_same_string_without_building_it_again()
    {
        var facts = new LaneFacts();
        var first = Fill(facts, have: 1);

        Assert.Same(first, Fill(facts, have: 1));
        Assert.NotEqual(first, Fill(facts, have: 2));
    }

    [Fact]
    public void Facts_go_back_to_none_once_every_event_ended()
    {
        var facts = new LaneFacts();

        Fill(facts, have: 1);
        facts.Begin();

        Assert.Equal(LaneFacts.None, facts.End());
    }

    [Theory]
    [InlineData(5, 2)]
    [InlineData(-3, 0)]
    public void How_many_the_player_has_is_held_between_none_and_what_the_event_asks_for(int have, int sent)
    {
        var facts = new LaneFacts();

        facts.Begin();
        facts.Event(7, 600);
        facts.Need(Water, 2, have);

        Assert.Contains($",2,{sent}]", facts.End());
    }

    [Fact]
    public void An_item_asked_for_zero_times_or_before_any_event_is_left_out()
    {
        var facts = new LaneFacts();

        facts.Begin();
        facts.Need(Water, 2, 1);
        facts.Event(7, -60);
        facts.Need(Water, 0, 0);

        Assert.Equal("{\"7\":{total:0,needs:[]}}", facts.End());
    }

    private static string Fill(LaneFacts facts, int have)
    {
        facts.Begin();
        facts.Event(202, 21600);
        facts.Need(Water, 2, have);

        return facts.End();
    }
}
