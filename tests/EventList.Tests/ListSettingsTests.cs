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
        Assert.Equal(ListSettings.DefaultMaxHeight, ListSettings.From(requested, true, true, true).MaxHeight);
    }

    [Fact]
    public void A_height_too_small_to_read_is_raised_to_the_lowest()
    {
        Assert.Equal(ListSettings.LowestMaxHeight, ListSettings.From(ListSettings.LowestMaxHeight - 1, true, true, true).MaxHeight);
    }

    [Fact]
    public void A_height_past_the_highest_is_lowered_to_the_highest()
    {
        Assert.Equal(ListSettings.HighestMaxHeight, ListSettings.From(5000, true, true, true).MaxHeight);
    }

    [Theory]
    [InlineData(ListSettings.LowestMaxHeight)]
    [InlineData(200)]
    [InlineData(ListSettings.HighestMaxHeight)]
    public void A_height_inside_the_range_is_kept_as_asked(int requested)
    {
        Assert.Equal(requested, ListSettings.From(requested, true, true, true).MaxHeight);
    }

    [Fact]
    public void The_log_says_when_countdowns_are_left_in_the_game_s_order()
    {
        Assert.Contains("countdowns in the game's order", ListSettings.From(0, true, false, true).Describe());
        Assert.Contains("game's order kept", ListSettings.From(0, false, true, true).Describe());
    }

    [Fact]
    public void The_log_says_whether_countdowns_go_soonest_first()
    {
        Assert.Contains("countdowns soonest first", ListSettings.From(0, true, true, true).Describe());
        Assert.Contains("countdowns among themselves in the game's order", ListSettings.From(0, true, true, false).Describe());
    }

    [Theory]
    [InlineData("Lanes", ListLayout.Lanes)]
    [InlineData(" lanes ", ListLayout.Lanes)]
    [InlineData("Groups", ListLayout.Groups)]
    [InlineData("Lane", ListLayout.Groups)]
    [InlineData("", ListLayout.Groups)]
    [InlineData(null, ListLayout.Groups)]
    public void Only_a_saved_Lanes_picks_the_lanes_and_anything_else_keeps_the_groups(string? saved, ListLayout expected)
    {
        Assert.Equal(expected, ListSettings.LayoutOf(saved));
    }

    [Fact]
    public void Every_layout_name_offered_in_the_settings_reads_back_as_its_layout()
    {
        Assert.Equal(new[] { ListLayout.Groups, ListLayout.Lanes }, ListSettings.LayoutNames.Select(ListSettings.LayoutOf));
    }

    /// <summary>The language is read from the game every tick; while it stays the same, nothing new is built for the check call to compare.</summary>
    [Fact]
    public void The_game_s_language_changes_only_the_language()
    {
        var settings = ListSettings.From(200, true, false, true, ListLayout.Lanes, false);
        var chinese = settings.WithChinese(true);

        Assert.Same(settings, settings.WithChinese(false));
        Assert.True(chinese.IsChinese);
        Assert.Equal(ListSettings.From(200, true, false, true, ListLayout.Lanes, true), chinese);
    }

    [Fact]
    public void The_log_says_when_the_lanes_are_on()
    {
        Assert.Equal("lanes at most 200px tall", ListSettings.From(200, true, true, true, ListLayout.Lanes, false).Describe());
    }

    [Fact]
    public void The_default_height_is_one_the_range_allows()
    {
        Assert.InRange(ListSettings.DefaultMaxHeight, ListSettings.LowestMaxHeight, ListSettings.HighestMaxHeight);
    }
}
