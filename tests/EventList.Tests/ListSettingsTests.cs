using EventList.Core;
using Xunit;

namespace EventList.Tests;

public class ListSettingsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-40)]
    public void A_height_of_zero_or_less_means_the_default_height(int requested)
    {
        Assert.Equal(ListSettings.DefaultMaxHeight, ListSettings.From(requested, true, true).MaxHeight);
    }

    [Fact]
    public void A_height_too_small_to_read_is_raised_to_the_lowest()
    {
        Assert.Equal(ListSettings.LowestMaxHeight, ListSettings.From(ListSettings.LowestMaxHeight - 1, true, true).MaxHeight);
    }

    [Fact]
    public void A_height_past_the_highest_is_lowered_to_the_highest()
    {
        Assert.Equal(ListSettings.HighestMaxHeight, ListSettings.From(5000, true, true).MaxHeight);
    }

    [Theory]
    [InlineData(ListSettings.LowestMaxHeight)]
    [InlineData(200)]
    [InlineData(ListSettings.HighestMaxHeight)]
    public void A_height_inside_the_range_is_kept_as_asked(int requested)
    {
        Assert.Equal(requested, ListSettings.From(requested, true, true).MaxHeight);
    }

    [Fact]
    public void The_log_says_when_countdowns_are_left_in_the_game_s_order()
    {
        Assert.Contains("countdowns in the game's order", ListSettings.From(0, true, false).Describe());
        Assert.Contains("game's order kept", ListSettings.From(0, false, true).Describe());
    }

    [Fact]
    public void The_default_height_is_one_the_range_allows()
    {
        Assert.InRange(ListSettings.DefaultMaxHeight, ListSettings.LowestMaxHeight, ListSettings.HighestMaxHeight);
    }
}
