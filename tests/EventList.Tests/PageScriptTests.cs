using EventList.Core;
using Xunit;

namespace EventList.Tests;

/// <summary>eventlist.js hooked into a fake events list and driven the way the plugin drives it, through apply.</summary>
public class PageScriptTests
{
    /// <summary>A Main group with one event and a Normal group with two, too tall for the box. Nothing is important yet.</summary>
    private const string QuietList = "{groups: [[{eventConfigId: 101, text: 'Repair the gate'}], [{eventConfigId: 201, text: 'Scout the ruins'}, {eventConfigId: 202, text: 'Trade caravan'}]], isOverflowing: true}";

    /// <summary>The same list while the game shows 201 as an emergency and 202 counts down.</summary>
    private const string BusyList = "{groups: [[{eventConfigId: 101, text: 'Repair the gate'}], [{eventConfigId: 201, text: 'Scout the ruins'}, {eventConfigId: 202, text: 'Trade caravan', timeActive: true, remainSeconds: 45}]], chips: [{eventConfigId: 201}], isOverflowing: true}";

    private const string BusyListInScrollMode = "{groups: [[{eventConfigId: 101, text: 'Repair the gate'}], [{eventConfigId: 201, text: 'Scout the ruins'}, {eventConfigId: 202, text: 'Trade caravan', timeActive: true, remainSeconds: 45}]], chips: [{eventConfigId: 201}], isOverflowing: true, scrollMode: true}";

    /// <summary>
    /// A Main group counting down 200 seconds and a Normal group with countdowns of 300 and 45 seconds, an event with no
    /// countdown, and a countdown whose time left is not a number.
    /// </summary>
    private const string CountdownList = "{groups: [[{eventConfigId: 301, text: 'Water runs out', timeActive: true, remainSeconds: 200}], [{eventConfigId: 401, text: 'Trader leaves', timeActive: true, remainSeconds: 300}, {eventConfigId: 402, text: 'Raid', timeActive: true, remainSeconds: 45}, {eventConfigId: 403, text: 'Scout the ruins'}, {eventConfigId: 404, text: 'Signal fades', timeActive: true, remainSeconds: '90'}]], isOverflowing: true}";

    /// <summary>Defines orderOf(selector): the CSS order the script's stylesheet gives a rule with exactly that selector.</summary>
    private const string OrderOfScript = "function orderOf(selector) { var lines = document.getElementById('sl-eventlist-style').textContent.split('\\n'); for (var i = 0; i < lines.length; i++) { if (lines[i].indexOf(selector + ' {') >= 0) return Number(/order: (-?\\d+);/.exec(lines[i])[1]); } return NaN; }";

    private const string StartCountdown = "page.item(202).timeActive = true; page.item(202).remainSeconds = 45;";

    private const string PageChangedShape = "page.app._vnode.component.setupState = {};";

    private static readonly string Defaults = PageCalls.Settings(ListSettings.From(0, true, true, true));

    [Fact]
    public void A_newly_important_event_brings_the_list_back_to_the_top_while_the_pointer_is_away()
    {
        var page = Hooked(QuietList);

        page.Run("movePointer(document.body, page.row(201)); movePointer(page.row(201), document.body);");
        page.Run("page.box.scrollTop = 80;" + StartCountdown);

        Assert.Equal(PageAnswer.Ok, page.Apply(Defaults));
        Assert.Equal(0.0, page.Number("page.box.scrollTop"));
    }

    /// <summary>A player reading or scrolling the list is never yanked: the scroll to the top waits until the pointer leaves the list.</summary>
    [Fact]
    public void A_newly_important_event_waits_for_the_pointer_to_leave_the_list_before_scrolling()
    {
        var page = Hooked(QuietList);

        page.Run("movePointer(document.body, page.row(201)); page.box.scrollTop = 80;" + StartCountdown);

        Assert.Equal(PageAnswer.Ok, page.Apply(Defaults));
        Assert.Equal(80.0, page.Number("page.box.scrollTop"));

        page.Run("movePointer(page.row(201), page.row(202));");

        Assert.Equal(80.0, page.Number("page.box.scrollTop"));

        page.Run("movePointer(page.row(202), document.body);");

        Assert.Equal(0.0, page.Number("page.box.scrollTop"));
    }

    [Fact]
    public void Leaving_the_list_with_nothing_newly_important_leaves_it_where_the_player_put_it()
    {
        var page = Hooked(QuietList);

        page.Run("movePointer(null, page.row(201)); page.box.scrollTop = 80; movePointer(page.row(201), null);");

        Assert.Equal(PageAnswer.Ok, page.Apply(Defaults));
        Assert.Equal(80.0, page.Number("page.box.scrollTop"));
    }

    [Fact]
    public void An_event_that_was_already_important_does_not_pull_the_list_up_again()
    {
        var page = Hooked(QuietList);

        page.Run(StartCountdown);
        page.Apply(Defaults);
        page.Run("page.box.scrollTop = 80; page.item(202).remainSeconds = 44;");

        Assert.Equal(PageAnswer.Ok, page.Apply(Defaults));
        Assert.Equal(80.0, page.Number("page.box.scrollTop"));
    }

    [Fact]
    public void Emergencies_go_above_countdowns_and_lift_their_group()
    {
        var page = Hooked(BusyList);

        Assert.Equal("crisis", Mark(page, "page.row(201)"));
        Assert.Equal("deadline", Mark(page, "page.row(202)"));
        Assert.Equal("crisis", Mark(page, "page.groupEls[1]"));
        Assert.Equal("", Mark(page, "page.groupEls[0]"));
    }

    [Fact]
    public void With_countdowns_not_counting_only_emergencies_move_up()
    {
        var page = PageScript.On(BusyList);

        Assert.Equal(PageAnswer.Ok, page.Apply(PageCalls.Settings(ListSettings.From(0, true, false, true))));
        Assert.Equal("crisis", Mark(page, "page.row(201)"));
        Assert.Equal("", Mark(page, "page.row(202)"));

        Assert.Equal(PageAnswer.Ok, page.Apply(Defaults));
        Assert.Equal("deadline", Mark(page, "page.row(202)"));
    }

    /// <summary>Within the countdown tier the one about to run out comes first.</summary>
    [Fact]
    public void Countdowns_on_top_are_ordered_soonest_first_with_unreadable_ones_last()
    {
        var page = Hooked(CountdownList);

        Assert.Equal("0", Rank(page, "page.row(402)"));
        Assert.Equal("1", Rank(page, "page.row(401)"));
        Assert.Equal("2", Rank(page, "page.row(404)"));
        Assert.Equal("", Rank(page, "page.row(403)"));
        Assert.Equal("deadline", Mark(page, "page.row(404)"));
    }

    /// <summary>A group of countdowns is as urgent as its soonest one: the 45 second raid lifts its group above the 200 second one.</summary>
    [Fact]
    public void A_group_whose_countdown_runs_out_sooner_goes_above_the_other_countdown_groups()
    {
        var page = Hooked(CountdownList);

        Assert.Equal("0", Rank(page, "page.groupEls[1]"));
        Assert.Equal("1", Rank(page, "page.groupEls[0]"));
    }

    /// <summary>
    /// Countdowns tick at one shared rate, so in the game they never pass each other; the list is re-ranked when the
    /// game sends a new one. This is a fallback check that the two-second recheck also re-ranks from fresh times.
    /// </summary>
    [Fact]
    public void A_countdown_that_overtakes_another_moves_ahead_on_the_next_check()
    {
        var page = Hooked(CountdownList);

        page.Run("page.item(401).remainSeconds = 10;");

        Assert.Equal(PageAnswer.Ok, page.Apply(Defaults));
        Assert.Equal("0", Rank(page, "page.row(401)"));
        Assert.Equal("1", Rank(page, "page.row(402)"));
    }

    [Fact]
    public void With_soonest_first_off_countdowns_keep_the_game_s_order_on_top()
    {
        var page = Hooked(CountdownList);

        Assert.Equal(PageAnswer.Ok, page.Apply(PageCalls.Settings(ListSettings.From(0, true, true, false))));
        Assert.Equal(0.0, page.Number("page.box.querySelectorAll('[data-sl-rank]').length"));
        Assert.Equal("deadline", Mark(page, "page.row(402)"));
        Assert.Equal("deadline", Mark(page, "page.row(401)"));
    }

    /// <summary>
    /// Ranked countdowns sit below the group header and the emergencies, and above the countdowns too many to rank,
    /// which still sit above every event that is not important. No header selector means the element has none.
    /// </summary>
    [Theory]
    [InlineData(".event-group > .event-item-row", ".event-group > .event-group-header")]
    [InlineData(".event-scroll > .event-group", "")]
    public void The_stylesheet_puts_ranked_countdowns_between_emergencies_and_unranked_countdowns(string element, string header)
    {
        var page = Hooked(CountdownList);

        page.Run(OrderOfScript);

        var crisis = OrderOf(page, element + "[data-sl-important=\"crisis\"]");
        var first = OrderOf(page, element + "[data-sl-important=\"deadline\"][data-sl-rank=\"0\"]");
        var last = OrderOf(page, element + "[data-sl-important=\"deadline\"][data-sl-rank=\"63\"]");
        var unranked = OrderOf(page, element + "[data-sl-important=\"deadline\"]");

        if (header.Length > 0) Assert.True(OrderOf(page, header) < crisis);

        Assert.True(crisis < first);
        Assert.True(first < last);
        Assert.True(last < unranked);
        Assert.True(unranked < 0);
    }

    /// <summary>
    /// When the page stops looking like the one the script was written for, the list goes back to exactly what the game
    /// draws: the game's hover flag off, the rows taking their own clicks, no marks and no fixed height.
    /// </summary>
    [Fact]
    public void A_page_that_stops_fitting_the_script_gets_its_list_back_as_the_game_draws_it()
    {
        var page = Hooked(BusyList);

        Assert.True(page.Is("page.state.eventHover"));
        Assert.True(page.Is("page.box.hasAttribute('data-interactive')"));
        Assert.Equal(1.0, page.Number("page.box.querySelectorAll('[data-sl-rank]').length"));

        page.Run(PageChangedShape);

        Assert.StartsWith(PageAnswer.UnsupportedPrefix, page.Apply(Defaults));
        Assert.False(page.Is("page.state.eventHover"));
        Assert.False(page.Is("page.box.hasAttribute('data-interactive')"));
        Assert.True(page.Is("page.rows.every(function (row) { return row.getAttribute('data-interactive') === ''; })"));
        Assert.Equal(0.0, page.Number("page.box.querySelectorAll('[data-sl-important]').length"));
        Assert.Equal(0.0, page.Number("page.box.querySelectorAll('[data-sl-rank]').length"));
        Assert.False(page.Is("document.documentElement.classList.contains('sl-eventlist')"));
    }

    /// <summary>In its own scroll mode the game makes the box clickable through its class and the rows not at all.</summary>
    [Fact]
    public void In_the_game_s_scroll_mode_a_dropped_list_keeps_only_the_game_s_own_click_area()
    {
        var page = Hooked(BusyListInScrollMode);

        Assert.True(page.Is("page.box.hasAttribute('data-interactive')"));

        page.Run(PageChangedShape);

        Assert.StartsWith(PageAnswer.UnsupportedPrefix, page.Apply(Defaults));
        Assert.False(page.Is("page.box.hasAttribute('data-interactive')"));
        Assert.True(page.Is("page.box.classList.contains('unity-interactive')"));
        Assert.True(page.Is("page.rows.every(function (row) { return !row.hasAttribute('data-interactive'); })"));
    }

    [Fact]
    public void A_dropped_list_no_longer_scrolls_when_the_pointer_leaves()
    {
        var page = Hooked(QuietList);

        page.Run("movePointer(null, page.row(201)); page.box.scrollTop = 80;" + StartCountdown);
        page.Apply(Defaults);
        page.Run(PageChangedShape);
        page.Apply(Defaults);
        page.Run("movePointer(page.row(201), null);");

        Assert.Equal(80.0, page.Number("page.box.scrollTop"));
    }

    private static PageScript Hooked(string pageSpec)
    {
        var page = PageScript.On(pageSpec);

        Assert.Equal(PageAnswer.Ok, page.Apply(Defaults));

        return page;
    }

    private static string Rank(PageScript page, string element) => page.Text($"{element}.getAttribute('data-sl-rank') || ''");

    private static double OrderOf(PageScript page, string selector) => page.Number($"orderOf({PageCalls.Quote(selector)})");

    private static string Mark(PageScript page, string element) => page.Text($"{element}.getAttribute('data-sl-important') || ''");
}
