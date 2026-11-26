using EventList.Core;
using Xunit;

namespace EventList.Tests;

public class PageStopTests
{
    private const double Frame = 0.1;

    [Fact]
    public void A_mod_that_starts_off_sends_nothing()
    {
        var stop = new PageStop(false);

        Assert.False(stop.Step(0, null, out _));
        Assert.False(stop.Step(100, null, out _));
    }

    /// <summary>Turning the mod off has to give the list back right away, without a restart.</summary>
    [Fact]
    public void Turning_the_mod_off_sends_one_stop_call()
    {
        var stop = new PageStop(true);

        stop.Set(false);

        Assert.True(stop.Step(0, null, out _));
        Assert.False(stop.Step(Frame, null, out _));
        Assert.False(stop.Step(2 * Frame, PageAnswer.Stopped, out var line));
        Assert.NotNull(line);
        Assert.False(stop.Step(100, null, out _));
    }

    [Fact]
    public void A_stop_call_that_could_not_leave_is_tried_again()
    {
        var stop = new PageStop(true);

        stop.Set(false);
        stop.Step(0, null, out _);
        stop.NotSent(0);

        Assert.False(stop.Step(PageStop.RetrySeconds - Frame, null, out _));
        Assert.True(stop.Step(PageStop.RetrySeconds, null, out _));
    }

    [Fact]
    public void A_stop_call_with_no_answer_is_tried_again()
    {
        var stop = new PageStop(true);

        stop.Set(false);
        stop.Step(0, null, out _);

        Assert.False(stop.Step(PageWatch.AnswerWaitSeconds, null, out _));
        Assert.True(stop.Step(PageWatch.AnswerWaitSeconds + PageStop.RetrySeconds, null, out _));
    }

    [Fact]
    public void A_page_with_no_events_list_has_nothing_to_give_back()
    {
        var stop = new PageStop(true);

        stop.Set(false);
        stop.Step(0, null, out _);
        stop.Step(Frame, PageAnswer.NoPage, out _);

        Assert.False(stop.Step(100, null, out _));
    }

    [Fact]
    public void Turning_the_mod_back_on_says_so_and_drops_the_stop()
    {
        var stop = new PageStop(true);

        stop.Set(false);

        Assert.True(stop.Set(true));
        Assert.True(stop.IsOn);
        Assert.False(stop.Set(true));
    }
}
