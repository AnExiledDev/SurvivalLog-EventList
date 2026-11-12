using ExiledToolkit.Web;
using Xunit;

namespace EventList.Tests;

/// <summary>The rules lanes.js sorts and words the list with, run in the script itself.</summary>
public class LaneRulesTests
{
    private const string Rules = "window.__sleventlistLanes.rules";

    /// <summary>
    /// Defines plan(story, other, crisis, isSiege): the game's two groups (story first) as lanes, each lane its ids in order,
    /// "now:1,2|timed:3|progress:|rest:".
    /// </summary>
    private const string PlanScript = "function plan(story, other, crisis, isSiege) { var r = window.__sleventlistLanes.rules; var lanes = r.planLanes(r.readRows([{key: 'main', items: story}, {key: 'normal', items: other}], crisis, null), isSiege); return ['now', 'timed', 'progress', 'rest'].map(function (k) { return k + ':' + lanes[k].map(function (row) { return row.id; }).join(','); }).join('|'); }";

    private readonly PageScript _script = PageScript.Alone();

    public LaneRulesTests()
    {
        _script.Run(PlanScript);
    }

    [Theory]
    [InlineData("02:30", 150)]
    [InlineData("0:05", 5)]
    [InlineData("30:00", 1800)]
    public void The_game_s_countdown_text_reads_as_game_minutes_left(string text, int minutes)
    {
        Assert.Equal(minutes, _script.Number($"{Rules}.parseLeft({JsText.Literal(text)})"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("2:3")]
    [InlineData("soon")]
    [InlineData("1:02:03")]
    public void Text_that_is_not_a_countdown_is_not_read_as_one(string text)
    {
        Assert.True(_script.Is($"{Rules}.parseLeft({JsText.Literal(text)}) === null"));
    }

    [Theory]
    [InlineData(45, "45m")]
    [InlineData(60, "1h")]
    [InlineData(200, "3h 20m")]
    [InlineData(1440, "1d")]
    [InlineData(3240, "2d 6h")]
    public void Time_left_reads_the_way_a_person_says_it(int minutes, string expected)
    {
        Assert.Equal(expected, _script.Text($"{Rules}.formatLeft({minutes}, {Rules}.words.en)"));
    }

    [Fact]
    public void Time_left_in_Chinese_uses_Chinese_units()
    {
        Assert.Equal("3小时20分", _script.Text($"{Rules}.formatLeft(200, {Rules}.words.zh)"));
        Assert.Equal("2天6小时", _script.Text($"{Rules}.formatLeft(3240, {Rules}.words.zh)"));
    }

    /// <summary>The clock is 18:50 on day 51.</summary>
    [Theory]
    [InlineData(150, "today 21:20")]
    [InlineData(400, "tomorrow 01:30")]
    [InlineData(2000, "day 53 04:10")]
    public void A_countdown_says_when_it_runs_out_on_the_game_clock(int minutesLeft, string expected)
    {
        Assert.Equal(expected, _script.Text($"{Rules}.formatEndsAt({minutesLeft}, 1130, 'DAY51', {Rules}.words.en)"));
    }

    [Fact]
    public void With_no_day_number_a_far_countdown_says_how_many_days_ahead()
    {
        Assert.Equal("+2d 04:10", _script.Text($"{Rules}.formatEndsAt(2000, 1130, '', {Rules}.words.en)"));
    }

    [Fact]
    public void With_no_clock_a_countdown_does_not_guess_when_it_runs_out()
    {
        Assert.True(_script.Is($"{Rules}.formatEndsAt(150, null, 'DAY51', {Rules}.words.en) === null"));
    }

    [Fact]
    public void Emergencies_and_countdowns_under_six_hours_go_in_Now_and_the_rest_in_their_own_lanes()
    {
        var lanes = _script.Text("plan([{eventConfigId: 101, text: 'radio'}, {eventConfigId: 102, time: '30:00'}], [{eventConfigId: 201}, {eventConfigId: 202, time: '02:30'}, {eventConfigId: 203, time: '08:10'}, {eventConfigId: 204}], {201: true}, false)");

        Assert.Equal("now:201,202|timed:203,102|progress:101,204|rest:", lanes);
    }

    /// <summary>An emergency is in Now first, whatever its countdown says; countdowns under six hours follow it, soonest first.</summary>
    [Fact]
    public void Emergencies_lead_Now_in_the_game_s_order_and_countdowns_follow_soonest_first()
    {
        var lanes = _script.Text("plan([], [{eventConfigId: 1, time: '05:00'}, {eventConfigId: 2, time: '04:00'}, {eventConfigId: 3, time: '00:30'}, {eventConfigId: 4, time: '20:00'}], {4: true, 2: true}, false)");

        Assert.Equal("now:2,4,3,1|timed:|progress:|rest:", lanes);
    }

    [Fact]
    public void A_countdown_of_exactly_six_hours_is_not_yet_in_Now()
    {
        Assert.Equal("now:|timed:1|progress:|rest:", _script.Text("plan([], [{eventConfigId: 1, time: '06:00'}], {}, false)"));
    }

    [Fact]
    public void Countdowns_with_the_same_time_left_keep_the_game_s_order()
    {
        Assert.Equal("now:|timed:3,1,2|progress:|rest:", _script.Text("plan([], [{eventConfigId: 3, time: '07:00'}, {eventConfigId: 1, time: '07:00'}, {eventConfigId: 2, time: '07:00'}], {}, false)"));
    }

    [Fact]
    public void In_a_siege_everything_but_Now_goes_in_Everything_else_countdowns_first()
    {
        var lanes = _script.Text("plan([{eventConfigId: 101}], [{eventConfigId: 201}, {eventConfigId: 203, time: '08:10'}, {eventConfigId: 204}], {201: true}, true)");

        Assert.Equal("now:201|timed:|progress:|rest:203,101,204", lanes);
    }

    /// <summary>A tracker row under an event is never an emergency, even when it shares the event's id.</summary>
    [Fact]
    public void A_tracker_row_is_never_an_emergency()
    {
        Assert.Equal("now:|timed:|progress:7|rest:", _script.Text("plan([], [{eventConfigId: 7, extraRow: true}], {7: true}, false)"));
    }

    [Fact]
    public void An_event_listed_twice_shows_once()
    {
        Assert.Equal("now:|timed:|progress:7,8|rest:", _script.Text("plan([{eventConfigId: 7}], [{eventConfigId: 7}, {eventConfigId: 8}], {}, false)"));
    }

    [Fact]
    public void Rows_from_the_game_s_story_group_are_story_rows()
    {
        Assert.True(_script.Is($"{Rules}.readRows([{{key: 'main', items: [{{eventConfigId: 1}}]}}], {{}}, null)[0].isStory"));
        Assert.False(_script.Is($"{Rules}.readRows([{{key: 'normal', items: [{{eventConfigId: 1}}]}}], {{}}, null)[0].isStory"));
    }

    [Fact]
    public void A_folded_lane_says_how_many_story_events_it_holds()
    {
        var rows = "[{isStory: true, left: null}, {isStory: false, left: null}, {isStory: true, left: null}]";

        Assert.Equal("2 story", _script.Text($"{Rules}.laneHint('progress', {rows}, {Rules}.words.en)"));
        Assert.Equal("", _script.Text($"{Rules}.laneHint('progress', [{{isStory: false, left: null}}], {Rules}.words.en)"));
        Assert.Equal("", _script.Text($"{Rules}.laneHint('now', {rows}, {Rules}.words.en)"));
    }

    [Fact]
    public void Everything_else_in_a_siege_says_when_its_next_countdown_runs_out()
    {
        Assert.Equal("next 8h 10m", _script.Text($"{Rules}.laneHint('rest', [{{isStory: true, left: 490}}, {{isStory: false, left: null}}], {Rules}.words.en)"));
    }

    [Fact]
    public void Facts_in_a_shape_the_lanes_do_not_know_are_left_out()
    {
        var fact = $"{Rules}.factOf({{7: {{total: 'long', needs: [[null, 0, 1], 'water', ['Icon/a.png', 2, -4], ['', 1.5, 'x']]}}}}, 7)";

        Assert.Equal(0.0, _script.Number($"{fact}.totalMinutes"));
        Assert.Equal(2.0, _script.Number($"{fact}.needs.length"));
        Assert.Equal("Icon/a.png:2:0", _script.Text($"{fact}.needs[0].icon + ':' + {fact}.needs[0].need + ':' + {fact}.needs[0].have"));
        Assert.Equal(":1:0", _script.Text($"{fact}.needs[1].icon + ':' + {fact}.needs[1].need + ':' + {fact}.needs[1].have"));
        Assert.Equal(0.0, _script.Number($"{Rules}.factOf({{}}, 7).needs.length"));
    }
}
