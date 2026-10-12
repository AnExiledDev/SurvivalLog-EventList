using EventList.Core;
using Xunit;

namespace EventList.Tests;

public class PhoneBadgeTests
{
    private const long NoDeadline = 0;
    private const long Deadline = 86_400;

    [Fact]
    public void Counts_every_event_text_still_waiting_to_be_accepted()
    {
        var badge = PhoneBadge.Of(new[] { Waiting(NoDeadline), Waiting(NoDeadline), Waiting(NoDeadline) });

        Assert.Equal(3, badge.Waiting);
        Assert.False(badge.HasTimeLimit);
    }

    /// <summary>Accepted, finished, refused and run-out texts need nothing from the player, so they never show.</summary>
    [Theory]
    [InlineData("active")]
    [InlineData("done")]
    [InlineData("expired")]
    [InlineData("rejected")]
    [InlineData("")]
    [InlineData(null)]
    public void A_text_past_waiting_is_not_counted(string? state)
    {
        var badge = PhoneBadge.Of(new[] { Waiting(NoDeadline), new EventText(state, Deadline) });

        Assert.Equal(1, badge.Waiting);
        Assert.False(badge.HasTimeLimit);
    }

    [Fact]
    public void One_waiting_text_with_a_deadline_makes_the_badge_urgent()
    {
        var badge = PhoneBadge.Of(new[] { Waiting(NoDeadline), Waiting(Deadline), Waiting(NoDeadline) });

        Assert.Equal(3, badge.Waiting);
        Assert.True(badge.HasTimeLimit);
    }

    /// <summary>The badge goes away once nothing waits, whatever the texts that stopped waiting carried.</summary>
    [Fact]
    public void Nothing_waiting_is_no_badge_and_never_urgent()
    {
        var badge = PhoneBadge.Of(new[] { new EventText("expired", Deadline), new EventText("active", Deadline) });

        Assert.Equal(PhoneBadge.None, badge);
        Assert.Equal(0, badge.Waiting);
        Assert.False(badge.HasTimeLimit);
    }

    [Fact]
    public void An_empty_phone_is_no_badge()
    {
        Assert.Equal(PhoneBadge.None, PhoneBadge.Of(System.Array.Empty<EventText>()));
    }

    private static EventText Waiting(long deadline) => new(PhoneBadge.WaitingState, deadline);
}
