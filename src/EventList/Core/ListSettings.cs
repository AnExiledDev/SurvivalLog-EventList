using System;

namespace EventList.Core;

/// <summary>Groups: the game's list, held to a height and reordered. Lanes: our own panel, events split into lanes by how urgent they are.</summary>
public enum ListLayout
{
    Groups,
    Lanes
}

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

    /// <summary>Past this a fixed height stops meaning anything; the game's scroll box stops at 220.</summary>
    public const int HighestMaxHeight = 600;

    public const string GroupsName = "Groups";
    public const string LanesName = "Lanes";

    public static readonly string[] LayoutNames = { GroupsName, LanesName };

    private ListSettings(int maxHeight, bool isImportantOnTop, bool isCountdownOnTop, bool isCountdownSoonestFirst, ListLayout layout, bool isChinese)
    {
        MaxHeight = maxHeight;
        IsImportantOnTop = isImportantOnTop;
        IsCountdownOnTop = isCountdownOnTop;
        IsCountdownSoonestFirst = isCountdownSoonestFirst;
        Layout = layout;
        IsChinese = isChinese;
    }

    /// <summary>In the page's own CSS pixels, the unit the game's list is laid out in.</summary>
    public int MaxHeight { get; }

    public bool IsImportantOnTop { get; }

    /// <summary>Whether an event with a running countdown counts as important. Means nothing while <see cref="IsImportantOnTop"/> is off.</summary>
    public bool IsCountdownOnTop { get; }

    /// <summary>Whether the countdowns on top are ordered by time left, soonest first. Means nothing while <see cref="IsCountdownOnTop"/> is off.</summary>
    public bool IsCountdownSoonestFirst { get; }

    /// <summary>The three order settings above mean nothing in <see cref="ListLayout.Lanes"/>; the height still holds.</summary>
    public ListLayout Layout { get; }

    /// <summary>Whether the game shows Chinese, for the words the lanes write themselves. Not a setting: read from the game.</summary>
    public bool IsChinese { get; }

    public static ListSettings From(int requestedMaxHeight, bool isImportantOnTop, bool isCountdownOnTop, bool isCountdownSoonestFirst) =>
        From(requestedMaxHeight, isImportantOnTop, isCountdownOnTop, isCountdownSoonestFirst, ListLayout.Groups, false);

    public static ListSettings From(int requestedMaxHeight, bool isImportantOnTop, bool isCountdownOnTop, bool isCountdownSoonestFirst, ListLayout layout, bool isChinese) =>
        new(ClampMaxHeight(requestedMaxHeight), isImportantOnTop, isCountdownOnTop, isCountdownSoonestFirst, layout, isChinese);

    /// <summary>The saved Layout text as a layout; anything but Lanes is Groups, so a typo leaves the list as it was.</summary>
    public static ListLayout LayoutOf(string? saved) =>
        string.Equals(saved?.Trim(), LanesName, StringComparison.OrdinalIgnoreCase) ? ListLayout.Lanes : ListLayout.Groups;

    /// <summary>These settings with the game's language as it is now; the same object when it has not changed.</summary>
    public ListSettings WithChinese(bool isChinese) =>
        isChinese == IsChinese ? this : new(MaxHeight, IsImportantOnTop, IsCountdownOnTop, IsCountdownSoonestFirst, Layout, isChinese);

    /// <summary>0 or less asks for the default; anything else is held inside the range the page can show.</summary>
    public static int ClampMaxHeight(int requested) =>
        requested <= 0 ? DefaultMaxHeight : Math.Clamp(requested, LowestMaxHeight, HighestMaxHeight);

    public string Describe() =>
        Layout == ListLayout.Lanes ? $"lanes at most {MaxHeight}px tall" : $"list at most {MaxHeight}px tall, " + DescribeOrder();

    private string DescribeOrder()
    {
        if (!IsImportantOnTop) return "game's order kept";

        if (!IsCountdownOnTop) return "emergencies on top, countdowns in the game's order";

        return IsCountdownSoonestFirst ? "emergencies then countdowns on top, countdowns soonest first" : "emergencies then countdowns on top, countdowns among themselves in the game's order";
    }
}
