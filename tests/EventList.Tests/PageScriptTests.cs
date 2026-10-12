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

    private const string StartCountdown = "page.item(202).timeActive = true; page.item(202).remainSeconds = 45;";

    private const string PageChangedShape = "page.app._vnode.component.setupState = {};";

    private static readonly string Defaults = PageCalls.Settings(ListSettings.From(0, true, true));

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

        Assert.Equal(PageAnswer.Ok, page.Apply(PageCalls.Settings(ListSettings.From(0, true, false))));
        Assert.Equal("crisis", Mark(page, "page.row(201)"));
        Assert.Equal("", Mark(page, "page.row(202)"));

        Assert.Equal(PageAnswer.Ok, page.Apply(Defaults));
        Assert.Equal("deadline", Mark(page, "page.row(202)"));
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

        page.Run(PageChangedShape);

        Assert.StartsWith(PageAnswer.UnsupportedPrefix, page.Apply(Defaults));
        Assert.False(page.Is("page.state.eventHover"));
        Assert.False(page.Is("page.box.hasAttribute('data-interactive')"));
        Assert.True(page.Is("page.rows.every(function (row) { return row.getAttribute('data-interactive') === ''; })"));
        Assert.Equal(0.0, page.Number("page.box.querySelectorAll('[data-sl-important]').length"));
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

    private static string Mark(PageScript page, string element) => page.Text($"{element}.getAttribute('data-sl-important') || ''");
}
