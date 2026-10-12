using System;

namespace EventList.Core;

/// <summary>How the page script draws the events list. Built only through <see cref="From"/>, so the height is always one the page can use.</summary>
public sealed record ListSettings
{
    /// <summary>About the height of the game's own four-row list, a header and four rows, with part of a fifth showing that more scroll below.</summary>
    public const int DefaultMaxHeight = 120;

    /// <summary>A header and two rows; any less and the list is a sliver.</summary>
    public const int LowestMaxHeight = 60;

    /// <summary>Past this a fixed height stops meaning anything; the game's own scroll box stops at 220.</summary>
    public const int HighestMaxHeight = 600;

    private ListSettings(int maxHeight, bool isImportantOnTop)
    {
        MaxHeight = maxHeight;
        IsImportantOnTop = isImportantOnTop;
    }

    /// <summary>In the page's own CSS pixels, the unit the game's list is laid out in.</summary>
    public int MaxHeight { get; }

    public bool IsImportantOnTop { get; }

    public static ListSettings From(int requestedMaxHeight, bool isImportantOnTop) => new(ClampMaxHeight(requestedMaxHeight), isImportantOnTop);

    /// <summary>0 or less asks for the default; anything else is held inside the range the page can show.</summary>
    public static int ClampMaxHeight(int requested) =>
        requested <= 0 ? DefaultMaxHeight : Math.Clamp(requested, LowestMaxHeight, HighestMaxHeight);

    public string Describe() =>
        $"list at most {MaxHeight}px tall, " + (IsImportantOnTop ? "important events on top" : "game's order kept");
}
