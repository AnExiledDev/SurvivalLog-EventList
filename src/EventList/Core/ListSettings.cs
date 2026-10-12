using System;

namespace EventList.Core;

/// <summary>How the page script draws the events list. Built only through <see cref="From"/>, so the height is always one the page can use.</summary>
public sealed record ListSettings
{
    /// <summary>
    /// Both group headers and four rows, from the game's CSS: a row is an 11px line (about 14.6px tall in the page's font)
    /// with 2.5px padding above and below, a header the same line with 2px below, 4px between the rows and header of a
    /// group and 8px between groups. Two headers and four rows come to about 135.6px.
    /// </summary>
    public const int DefaultMaxHeight = 136;

    /// <summary>A header and two rows; any less and the list is a sliver.</summary>
    public const int LowestMaxHeight = 60;

    /// <summary>Past this a fixed height stops meaning anything; the game's own scroll box stops at 220.</summary>
    public const int HighestMaxHeight = 600;

    private ListSettings(int maxHeight, bool isImportantOnTop, bool isCountdownOnTop)
    {
        MaxHeight = maxHeight;
        IsImportantOnTop = isImportantOnTop;
        IsCountdownOnTop = isCountdownOnTop;
    }

    /// <summary>In the page's own CSS pixels, the unit the game's list is laid out in.</summary>
    public int MaxHeight { get; }

    public bool IsImportantOnTop { get; }

    /// <summary>Whether an event with a running countdown counts as important. Means nothing while <see cref="IsImportantOnTop"/> is off.</summary>
    public bool IsCountdownOnTop { get; }

    public static ListSettings From(int requestedMaxHeight, bool isImportantOnTop, bool isCountdownOnTop) =>
        new(ClampMaxHeight(requestedMaxHeight), isImportantOnTop, isCountdownOnTop);

    /// <summary>0 or less asks for the default; anything else is held inside the range the page can show.</summary>
    public static int ClampMaxHeight(int requested) =>
        requested <= 0 ? DefaultMaxHeight : Math.Clamp(requested, LowestMaxHeight, HighestMaxHeight);

    public string Describe() => $"list at most {MaxHeight}px tall, " + DescribeOrder();

    private string DescribeOrder()
    {
        if (!IsImportantOnTop) return "game's order kept";

        return IsCountdownOnTop ? "emergencies then countdowns on top" : "emergencies on top, countdowns in the game's order";
    }
}
