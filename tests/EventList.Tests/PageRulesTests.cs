using EventList.Core;
using Xunit;

namespace EventList.Tests;

/// <summary>The rules eventlist.js decides the list with, run in the script itself.</summary>
public class PageRulesTests
{
    private const string Rules = "window.__sleventlist.rules";

    /// <summary>One group of three rows too tall for the box, drawn as the game draws it: the rows take clicks, the box does not.</summary>
    private const string OverflowingList = "{groups: [[{eventConfigId: 1, text: 'a'}, {eventConfigId: 2, text: 'b'}, {eventConfigId: 3, text: 'c'}]], isOverflowing: true}";

    /// <summary>The same list in the game's own scroll mode, where the box takes clicks through its class and the rows take none.</summary>
    private const string OverflowingListInScrollMode = "{groups: [[{eventConfigId: 1, text: 'a'}, {eventConfigId: 2, text: 'b'}, {eventConfigId: 3, text: 'c'}]], isOverflowing: true, scrollMode: true}";

    private readonly PageScript _script = PageScript.Alone();

    [Fact]
    public void An_event_the_game_shows_as_an_emergency_is_a_crisis_even_while_it_counts_down()
    {
        Assert.Equal("crisis", TierOf("{eventConfigId: 7, timeActive: true, remainSeconds: 30}", "{7: true}", isCountdownImportant: true));
    }

    [Fact]
    public void An_emergency_stays_a_crisis_when_countdowns_do_not_count()
    {
        Assert.Equal("crisis", TierOf("{eventConfigId: 7, timeActive: true, remainSeconds: 30}", "{7: true}", isCountdownImportant: false));
    }

    /// <summary>Events with a countdown are important. CountdownOnTop, on by default, can turn it off.</summary>
    [Theory]
    [InlineData(true, "deadline")]
    [InlineData(false, "")]
    public void A_running_countdown_is_important_only_while_countdowns_count(bool isCountdownImportant, string expected)
    {
        Assert.Equal(expected, TierOf("{eventConfigId: 7, timeActive: true, remainSeconds: 30}", "{}", isCountdownImportant));
    }

    [Theory]
    [InlineData("{eventConfigId: 7, timeActive: true, remainSeconds: 0}")]
    [InlineData("{eventConfigId: 7, timeActive: false, remainSeconds: 30}")]
    [InlineData("{eventConfigId: 7, timeActive: true}")]
    [InlineData("{eventConfigId: 7}")]
    public void A_countdown_that_ran_out_or_is_not_running_is_not_important(string item)
    {
        Assert.Equal("", TierOf(item, "{}", isCountdownImportant: true));
    }

    /// <summary>Tracker rows the game adds under an event, and rows with no event behind them, never move.</summary>
    [Theory]
    [InlineData("{eventConfigId: 7, extraRow: true, timeActive: true, remainSeconds: 30}")]
    [InlineData("{eventConfigId: 0, timeActive: true, remainSeconds: 30}")]
    [InlineData("{eventConfigId: -3, timeActive: true, remainSeconds: 30}")]
    [InlineData("{timeActive: true, remainSeconds: 30}")]
    [InlineData("null")]
    public void Extra_rows_and_rows_without_an_event_are_never_important(string item)
    {
        Assert.Equal("", TierOf(item, "{7: true, 0: true, '-3': true}", isCountdownImportant: true));
    }

    [Theory]
    [InlineData(true, "5,8")]
    [InlineData(false, "")]
    public void Emergency_entries_count_only_while_the_game_shows_them(bool isShown, string expected)
    {
        var state = $"{{eventStatesActive: {Js(isShown)}, eventStates: [{{eventConfigId: 5}}, {{eventConfigId: 0}}, null, {{eventConfigId: 8}}], towerWaveState: 0, towerWaveEventId: 9}}";

        Assert.Equal(expected, CrisisIds(state));
    }

    [Theory]
    [InlineData(1, 9, "9")]
    [InlineData(2, 9, "9")]
    [InlineData(0, 9, "")]
    [InlineData(3, 9, "")]
    [InlineData(1, 0, "")]
    public void The_zombie_wave_counts_only_while_it_spawns_or_charges(int waveState, int waveEventId, string expected)
    {
        var state = $"{{eventStatesActive: false, eventStates: [], towerWaveState: {waveState}, towerWaveEventId: {waveEventId}}}";

        Assert.Equal(expected, CrisisIds(state));
    }

    /// <summary>The page falls back to the same default the plugin uses, so a bad call never shows a different list.</summary>
    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{maxHeight: 0}")]
    [InlineData("{maxHeight: -5}")]
    [InlineData("{maxHeight: 'tall'}")]
    [InlineData("{maxHeight: 1 / 0}")]
    [InlineData("{maxHeight: NaN}")]
    public void A_height_the_page_cannot_use_means_the_plugin_s_default_height(string settings)
    {
        Assert.Equal(ListSettings.DefaultMaxHeight, _script.Number($"{Rules}.readSettings({settings}).maxHeight"));
    }

    [Fact]
    public void A_height_is_rounded_to_whole_pixels()
    {
        Assert.Equal(151.0, _script.Number($"{Rules}.readSettings({{maxHeight: 150.6}}).maxHeight"));
    }

    /// <summary>Every order is on unless the plugin says false; a value the plugin never sends does not switch one off.</summary>
    [Theory]
    [InlineData("null", true, true, true)]
    [InlineData("{}", true, true, true)]
    [InlineData("{importantOnTop: false}", false, true, true)]
    [InlineData("{countdownOnTop: false}", true, false, true)]
    [InlineData("{soonestFirst: false}", true, true, false)]
    [InlineData("{importantOnTop: 0, countdownOnTop: 'no', soonestFirst: null}", true, true, true)]
    public void Only_an_explicit_false_turns_an_order_off(string settings, bool isImportantOnTop, bool isCountdownOnTop, bool isSoonestFirst)
    {
        Assert.Equal(isImportantOnTop, _script.Is($"{Rules}.readSettings({settings}).importantOnTop"));
        Assert.Equal(isCountdownOnTop, _script.Is($"{Rules}.readSettings({settings}).countdownOnTop"));
        Assert.Equal(isSoonestFirst, _script.Is($"{Rules}.readSettings({settings}).soonestFirst"));
    }

    [Theory]
    [InlineData(150, true, false, true)]
    [InlineData(0, false, true, true)]
    [InlineData(400, true, true, false)]
    public void The_page_uses_the_settings_the_plugin_sends(int maxHeight, bool isImportantOnTop, bool isCountdownOnTop, bool isSoonestFirst)
    {
        var settings = ListSettings.From(maxHeight, isImportantOnTop, isCountdownOnTop, isSoonestFirst);
        var read = $"{Rules}.readSettings({PageCalls.Settings(settings)})";

        Assert.Equal(settings.MaxHeight, _script.Number(read + ".maxHeight"));
        Assert.Equal(settings.IsImportantOnTop, _script.Is(read + ".importantOnTop"));
        Assert.Equal(settings.IsCountdownOnTop, _script.Is(read + ".countdownOnTop"));
        Assert.Equal(settings.IsCountdownSoonestFirst, _script.Is(read + ".soonestFirst"));
    }

    /// <summary>The countdown about to run out comes first.</summary>
    [Theory]
    [InlineData("[300, 45, 120]", "2,0,1")]
    [InlineData("[45, 120, 300]", "0,1,2")]
    [InlineData("[1.5, 0.25]", "1,0")]
    [InlineData("[]", "")]
    public void Countdowns_are_ranked_soonest_first(string timesLeft, string expectedRanks)
    {
        Assert.Equal(expectedRanks, RankByTimeLeft(timesLeft));
    }

    [Theory]
    [InlineData("[60, 60, 60]", "0,1,2")]
    [InlineData("[90, 60, 90, 60]", "2,0,3,1")]
    public void Countdowns_with_the_same_time_left_keep_the_game_s_order(string timesLeft, string expectedRanks)
    {
        Assert.Equal(expectedRanks, RankByTimeLeft(timesLeft));
    }

    /// <summary>Countdowns inside the same whole second count as a tie, so two rows never swap over a fraction of a second or over float noise between the game's messages.</summary>
    [Fact]
    public void Countdowns_in_the_same_whole_second_keep_the_game_s_order()
    {
        Assert.Equal("0,1", RankByTimeLeft("[12.7, 12.2]"));
    }

    /// <summary>A time left that is not a finite number cannot be compared, so that countdown keeps its place among the unreadable ones, after the rest.</summary>
    [Theory]
    [InlineData("[undefined, 300, 45]", "2,1,0")]
    [InlineData("[null, '30', 90, NaN, 1 / 0, {}]", "1,2,0,3,4,5")]
    [InlineData("[undefined, null]", "0,1")]
    public void Countdowns_whose_time_left_cannot_be_read_go_last_in_the_game_s_order(string timesLeft, string expectedRanks)
    {
        Assert.Equal(expectedRanks, RankByTimeLeft(timesLeft));
    }

    /// <summary>The page's click-through does not clip to scroll boxes, so rows scrolled out of sight must give up their clicks.</summary>
    [Fact]
    public void An_overflowing_list_takes_clicks_as_one_box_and_its_rows_give_theirs_up()
    {
        var page = PageScript.On(OverflowingList);

        Assert.True(page.Is($"{Rules}.routeClicks(page.box, false)"));
        Assert.True(page.Is("page.box.hasAttribute('data-interactive')"));
        Assert.True(page.Is("page.rows.every(function (row) { return !row.hasAttribute('data-interactive'); })"));
    }

    [Fact]
    public void A_list_that_fits_again_gives_clicks_back_to_its_rows()
    {
        var page = PageScript.On(OverflowingList);

        page.Run($"{Rules}.routeClicks(page.box, false); page.box.scrollHeight = page.box.clientHeight;");

        Assert.True(page.Is($"{Rules}.routeClicks(page.box, false)"));
        Assert.False(page.Is("page.box.hasAttribute('data-interactive')"));
        Assert.True(page.Is("page.rows.every(function (row) { return row.getAttribute('data-interactive') === ''; })"));
    }

    /// <summary>In its own scroll mode the game draws rows without click areas; the mod never adds them back.</summary>
    [Fact]
    public void In_the_game_s_scroll_mode_rows_never_take_clicks()
    {
        var page = PageScript.On(OverflowingListInScrollMode);

        page.Run($"{Rules}.routeClicks(page.box, true); page.box.scrollHeight = page.box.clientHeight; {Rules}.routeClicks(page.box, true);");

        Assert.True(page.Is("page.rows.every(function (row) { return !row.hasAttribute('data-interactive'); })"));
        Assert.False(page.Is("page.box.hasAttribute('data-interactive')"));
        Assert.True(page.Is("page.box.classList.contains('unity-interactive')"));
    }

    /// <summary>The click-through is resent only when a click area changed.</summary>
    [Fact]
    public void Click_areas_already_in_place_report_no_change()
    {
        var page = PageScript.On(OverflowingList);

        page.Run($"{Rules}.routeClicks(page.box, false);");

        Assert.False(page.Is($"{Rules}.routeClicks(page.box, false)"));
    }

    private string TierOf(string item, string crisis, bool isCountdownImportant) =>
        _script.Text($"{Rules}.tierOf({item}, {crisis}, {Js(isCountdownImportant)})");

    private string RankByTimeLeft(string timesLeft) => _script.Text($"{Rules}.rankByTimeLeft({timesLeft}).join(',')");

    private string CrisisIds(string state) => _script.Text($"Object.keys({Rules}.crisisIds({state})).join(',')");

    private static string Js(bool value) => value ? "true" : "false";
}
