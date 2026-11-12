using EventList.Core;
using Xunit;

namespace EventList.Tests;

/// <summary>The Lanes layout hooked into a fake events list and driven the way the plugin drives it, through apply.</summary>
public class LanesPageTests
{
    /// <summary>
    /// At 18:50 on day 51. The story group has a story line event with no countdown and a 30 hour countdown; the other
    /// group an emergency, a countdown of 2h 30m, one of 8h 10m, an event with no countdown and a tracker row.
    /// </summary>
    private const string Busy = "{groups: [[{eventConfigId: 101, text: 'Find the radio', endingLine: 'rescue'}, {eventConfigId: 102, text: 'Bridge repairs', time: '30:00'}], [{eventConfigId: 201, text: 'Zombie horde'}, {eventConfigId: 202, text: 'Water runs out', time: '02:30', count: '1/2'}, {eventConfigId: 203, text: 'Trader leaves', time: '08:10'}, {eventConfigId: 204, text: 'Scout the ruins'}, {eventConfigId: 205, text: 'Two texts', extraRow: true}]], chips: [{eventConfigId: 201}], clock: [18, 50], day: 'DAY51'}";

    private const string BusySiege = "{groups: [[{eventConfigId: 101, text: 'Find the radio', endingLine: 'rescue'}, {eventConfigId: 102, text: 'Bridge repairs', time: '30:00'}], [{eventConfigId: 201, text: 'Zombie horde'}, {eventConfigId: 202, text: 'Water runs out', time: '02:30', count: '1/2'}, {eventConfigId: 203, text: 'Trader leaves', time: '08:10'}, {eventConfigId: 204, text: 'Scout the ruins'}, {eventConfigId: 205, text: 'Two texts', extraRow: true}]], chips: [{eventConfigId: 201}], clock: [18, 50], day: 'DAY51', compactMode: true}";

    /// <summary>202 runs six hours in all and wants two of an item the player has one of; 203 runs twelve hours.</summary>
    private const string Facts = "{'202': {total: 21600, needs: [['Icon/Item/water.png', 2, 1]]}, '203': {total: 43200, needs: []}}";

    /// <summary>Defines lane(key), its row ids in order, and row(id), our row for an event.</summary>
    private const string Helpers = "function lane(key) { return document.querySelector('.sl-ln-' + key); } function ids(key) { return lane(key).querySelectorAll('.sl-ln-row').map(function (r) { return r.getAttribute('data-sl-id'); }).join(','); } function row(id) { return document.querySelectorAll('.sl-ln-row').filter(function (r) { return r.getAttribute('data-sl-id') === String(id); })[0] || null; } function part(id, selector) { var el = row(id).querySelector(selector); return el ? el.textContent : null; }";

    private const string NoBadge = "{waiting: 0, timed: false}";

    private static readonly string Lanes = PageCalls.Settings(ListSettings.From(0, true, true, true, ListLayout.Lanes, false));

    private static readonly string LanesChinese = PageCalls.Settings(ListSettings.From(0, true, true, true, ListLayout.Lanes, true));

    private static readonly string Groups = PageCalls.Settings(ListSettings.From(0, true, true, true));

    [Fact]
    public void Events_go_in_lanes_by_what_needs_the_player_first()
    {
        var page = Hooked(Busy);

        Assert.Equal("201,202", page.Text("ids('now')"));
        Assert.Equal("203,102", page.Text("ids('timed')"));
        Assert.Equal("101,204,205", page.Text("ids('progress')"));
        Assert.True(page.Is("lane('rest').classList.contains('is-empty')"));
    }

    /// <summary>The page's stylesheet hides the game's scroll box while this class is on; our panel sits in the game's panel instead.</summary>
    [Fact]
    public void The_lanes_take_the_place_of_the_game_s_rows()
    {
        var page = Hooked(Busy);

        Assert.True(page.Is("document.documentElement.classList.contains('sl-eventlist-lanes')"));
        Assert.True(page.Is("page.panel.querySelector('.sl-lanes') !== null"));
    }

    [Fact]
    public void A_row_in_Now_says_when_it_runs_out_and_what_it_still_needs()
    {
        var page = Hooked(Busy);

        Assert.Equal("2h 30m", page.Text("part(202, '.sl-ln-time')"));
        Assert.True(page.Is("row(202).querySelector('.sl-ln-time').classList.contains('now')"));
        Assert.Equal("1/2", page.Text("part(202, '.sl-ln-count')"));
        Assert.Equal("runs out today 21:20", page.Text("part(202, '.sl-ln-ends')"));
        Assert.Equal("need 2, have 1", page.Text("part(202, '.sl-ln-need')"));
        Assert.False(page.Is("row(202).querySelector('.sl-ln-need').classList.contains('is-met')"));
        Assert.Equal("../../Icon/Item/water.png", page.Text("row(202).querySelector('.sl-ln-item').getAttribute('src')"));
    }

    [Fact]
    public void An_emergency_with_no_countdown_reads_now()
    {
        var page = Hooked(Busy);

        Assert.Equal("now", page.Text("part(201, '.sl-ln-time')"));
        Assert.True(page.Is("row(201).querySelector('.sl-ln-sub') === null"));
    }

    /// <summary>Only Now carries the second line; the other lanes stay one line a row.</summary>
    [Fact]
    public void Rows_outside_Now_have_no_second_line()
    {
        var page = Hooked(Busy);

        Assert.True(page.Is("row(203).querySelector('.sl-ln-sub') === null"));
        Assert.Equal("8h 10m", page.Text("part(203, '.sl-ln-time')"));
        Assert.True(page.Is("row(203).querySelector('.sl-ln-time').classList.contains('soon')"));
        Assert.True(page.Is("row(102).querySelector('.sl-ln-time').classList.contains('later')"));
    }

    [Fact]
    public void A_countdown_s_bar_shows_how_much_of_its_time_is_left()
    {
        var page = Hooked(Busy);

        Assert.Equal("0.417", page.Text("row(202).querySelector('.sl-ln-drain').style.getPropertyValue('--sl-left')"));
        Assert.Equal("0.681", page.Text("row(203).querySelector('.sl-ln-drain').style.getPropertyValue('--sl-left')"));
        Assert.True(page.Is("row(102).querySelector('.sl-ln-drain') === null"));
    }

    [Fact]
    public void A_story_line_event_keeps_the_game_s_line_icon_in_the_line_s_colour()
    {
        var page = Hooked(Busy);

        Assert.Equal("#ef5350", page.Text("row(101).querySelector('.sl-ln-mark').style.getPropertyValue('color')"));
        Assert.Equal("M12 2l10 20H2z", page.Text("row(101).querySelector('.sl-ln-mark').children[0].getAttribute('d')"));
        Assert.True(page.Is("row(101).classList.contains('is-story')"));
        Assert.True(page.Is("row(204).querySelector('.sl-ln-diamond') !== null"));
    }

    [Fact]
    public void In_progress_starts_folded_and_says_how_many_story_events_it_holds()
    {
        var page = Hooked(Busy);

        Assert.True(page.Is("lane('progress').classList.contains('is-folded')"));
        Assert.Equal("3", page.Text("lane('progress').querySelector('.sl-ln-n').textContent"));
        Assert.Equal("1 story", page.Text("lane('progress').querySelector('.sl-ln-hint').textContent"));
        Assert.False(page.Is("lane('now').classList.contains('is-folded')"));
    }

    [Fact]
    public void A_click_on_a_folded_lane_s_header_opens_it_and_a_second_folds_it_again()
    {
        var page = Hooked(Busy);
        var syncs = page.Number("core.clickThrough.syncs");

        page.Run("click(lane('progress').querySelector('.sl-ln-label'));");

        Assert.False(page.Is("lane('progress').classList.contains('is-folded')"));
        Assert.True(page.Number("core.clickThrough.syncs") > syncs);
        Assert.Equal(0.0, page.Number("page.sent.length"));

        page.Run("click(lane('progress').querySelector('.sl-ln-head'));");

        Assert.True(page.Is("lane('progress').classList.contains('is-folded')"));
    }

    /// <summary>The game opens an event when its row is clicked; a click on ours sends the game the same message.</summary>
    [Fact]
    public void A_click_on_a_row_opens_its_event_the_way_the_game_s_row_does()
    {
        var page = Hooked(Busy);

        page.Run("click(row(203).querySelector('.sl-ln-text'));");

        Assert.Equal("EVENT_CLICK 203", page.Text("page.sent[0].action + ' ' + page.sent[0].data.eventConfigId"));
    }

    [Fact]
    public void A_click_on_a_tracker_row_opens_the_phone_the_way_the_game_s_does()
    {
        var page = Hooked(Busy);

        page.Run("click(row(205));");

        Assert.Equal("OPEN_PHONE", page.Text("page.sent[0].action"));
        Assert.True(page.Is("page.sent[0].data === undefined"));
    }

    [Fact]
    public void In_a_siege_only_Now_stays_open_and_the_rest_folds_into_Everything_else()
    {
        var page = Hooked(BusySiege);

        Assert.Equal("201,202", page.Text("ids('now')"));
        Assert.Equal("203,102,101,204,205", page.Text("ids('rest')"));
        Assert.True(page.Is("lane('timed').classList.contains('is-empty') && lane('progress').classList.contains('is-empty')"));
        Assert.True(page.Is("lane('rest').classList.contains('is-folded')"));
        Assert.Equal("next 8h 10m", page.Text("lane('rest').querySelector('.sl-ln-hint').textContent"));
    }

    [Fact]
    public void When_the_siege_ends_the_lanes_come_back()
    {
        var page = Hooked(BusySiege);

        page.Run("page.setupState.eventCompactMode = false;");
        page.Apply(Lanes, NoBadge, "null", Facts);

        Assert.Equal("203,102", page.Text("ids('timed')"));
        Assert.True(page.Is("lane('rest').classList.contains('is-empty')"));
    }

    /// <summary>The game writes its countdown texts each game minute past Vue; the lanes watch the clock to keep up.</summary>
    [Fact]
    public void The_lanes_follow_the_game_clock_without_waiting_for_the_plugin()
    {
        var page = Hooked(Busy);

        page.Run("page.item(202).time = '02:29'; page.state.clockMinute = 51; Vue.runWatchers();");

        Assert.Equal("2h 29m", page.Text("part(202, '.sl-ln-time')"));
        Assert.Equal("0.414", page.Text("row(202).querySelector('.sl-ln-drain').style.getPropertyValue('--sl-left')"));
    }

    /// <summary>Rows are kept and moved, not built again, so a click or the pointer on one is not lost at each redraw.</summary>
    [Fact]
    public void A_row_keeps_its_place_on_the_page_as_its_countdown_runs()
    {
        var page = Hooked(Busy);

        page.Run("var kept = row(203); page.item(203).time = '08:09';");
        page.Apply(Lanes, NoBadge, "null", Facts);

        Assert.True(page.Is("row(203) === kept"));
        Assert.Equal("8h 9m", page.Text("part(203, '.sl-ln-time')"));
    }

    [Fact]
    public void A_countdown_that_drops_under_six_hours_moves_into_Now()
    {
        var page = Hooked(Busy);

        page.Run("page.item(203).time = '01:00';");
        page.Apply(Lanes, NoBadge, "null", Facts);

        Assert.Equal("201,203,202", page.Text("ids('now')"));
        Assert.Equal("102", page.Text("ids('timed')"));
        Assert.Equal("runs out today 19:50", page.Text("part(203, '.sl-ln-ends')"));
    }

    [Fact]
    public void An_event_that_ended_leaves_its_lane()
    {
        var page = Hooked(Busy);

        page.Run("page.setupState.groupedEventList[1].items.splice(2, 1);");
        page.Apply(Lanes, NoBadge, "null", Facts);

        Assert.Equal("102", page.Text("ids('timed')"));
        Assert.True(page.Is("row(203) === null"));
    }

    /// <summary>The page's click-through does not clip to scroll boxes, so a row scrolled out of sight would catch clicks meant for the world.</summary>
    [Fact]
    public void While_the_lanes_overflow_the_whole_panel_takes_the_clicks_and_the_rows_give_theirs_up()
    {
        var page = Hooked(Busy);

        Assert.True(page.Is("row(203).hasAttribute('data-interactive') && lane('progress').querySelector('.sl-ln-head').hasAttribute('data-interactive')"));
        Assert.False(page.Is("document.querySelector('.sl-lanes').hasAttribute('data-interactive')"));

        page.Run("var root = document.querySelector('.sl-lanes'); root.clientHeight = 136; root.scrollHeight = 300;");
        page.Apply(Lanes, NoBadge, "null", Facts);

        Assert.True(page.Is("root.hasAttribute('data-interactive')"));
        Assert.False(page.Is("row(203).hasAttribute('data-interactive') || lane('progress').querySelector('.sl-ln-head').hasAttribute('data-interactive')"));
    }

    [Fact]
    public void The_wheel_scrolls_the_lanes_only_while_they_overflow()
    {
        var page = Hooked(Busy);

        Assert.False(page.Is("dispatch(document.querySelector('.sl-lanes'), 'wheel', {deltaY: 40}).isStopped"));

        page.Run("var root = document.querySelector('.sl-lanes'); root.clientHeight = 136; root.scrollHeight = 300;");

        Assert.True(page.Is("dispatch(root, 'wheel', {deltaY: 40}).isStopped"));
        Assert.Equal(40.0, page.Number("root.scrollTop"));
    }

    [Fact]
    public void A_new_emergency_brings_the_lanes_back_to_the_top_while_the_pointer_is_away()
    {
        var page = Hooked(Busy);

        page.Run("document.querySelector('.sl-lanes').scrollTop = 60; page.state.eventStates.push({eventConfigId: 204});");
        page.Apply(Lanes, NoBadge, "null", Facts);

        Assert.Equal("201,204,202", page.Text("ids('now')"));
        Assert.Equal(0.0, page.Number("document.querySelector('.sl-lanes').scrollTop"));
    }

    [Fact]
    public void In_Chinese_the_lanes_write_their_own_words_in_Chinese()
    {
        var page = Hooked(Busy, LanesChinese);

        Assert.Equal("紧急", page.Text("lane('now').querySelector('.sl-ln-label').textContent"));
        Assert.Equal("2小时30分", page.Text("part(202, '.sl-ln-time')"));
        Assert.Equal("今天 21:20 到期", page.Text("part(202, '.sl-ln-ends')"));
        Assert.Equal("需要 2，已有 1", page.Text("part(202, '.sl-ln-need')"));
        Assert.Equal("主线 1", page.Text("lane('progress').querySelector('.sl-ln-hint').textContent"));
    }

    [Fact]
    public void A_change_of_the_game_s_language_rewords_the_lanes()
    {
        var page = Hooked(Busy);

        page.Apply(LanesChinese, NoBadge, "null", Facts);

        Assert.Equal("今天 21:20 到期", page.Text("part(202, '.sl-ln-ends')"));
    }

    [Fact]
    public void Without_facts_the_lanes_still_sort_and_time_the_events()
    {
        var page = PageScript.On(Busy);

        page.Run(Helpers);

        Assert.Equal(PageAnswer.Ok, page.Apply(Lanes));
        Assert.Equal("201,202", page.Text("ids('now')"));
        Assert.Equal("runs out today 21:20", page.Text("part(202, '.sl-ln-ends')"));
        Assert.True(page.Is("row(202).querySelector('.sl-ln-drain') === null && row(202).querySelector('.sl-ln-need') === null"));
    }

    [Fact]
    public void Switching_back_to_groups_gives_the_list_back_to_the_game()
    {
        var page = Hooked(Busy);

        Assert.Equal(PageAnswer.Ok, page.Apply(Groups, NoBadge, "null"));
        Assert.True(page.Is("document.querySelector('.sl-lanes') === null"));
        Assert.False(page.Is("document.documentElement.classList.contains('sl-eventlist-lanes')"));
    }

    /// <summary>Without the game's row message the lanes could not open an event, so they stay off and the log says why.</summary>
    [Fact]
    public void A_page_with_no_way_to_send_a_row_click_keeps_the_game_s_list()
    {
        var page = PageScript.On(Busy);

        page.Run("delete page.setupState.UnitySendEvent;");

        Assert.StartsWith("unsupported: no way to send a row click", page.Apply(Lanes));
        Assert.True(page.Is("document.querySelector('.sl-lanes') === null"));
    }

    private static PageScript Hooked(string pageSpec) => Hooked(pageSpec, Lanes);

    private static PageScript Hooked(string pageSpec, string settings)
    {
        var page = PageScript.On(pageSpec);

        page.Run(Helpers);

        Assert.Equal(PageAnswer.Ok, page.Apply(settings, NoBadge, "null", Facts));

        return page;
    }
}
