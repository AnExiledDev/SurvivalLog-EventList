using System.Collections.Generic;

namespace EventList.Core;

/// <summary>
/// An event text in the phone as the game keeps it: its state (the game's PhoneSMSMsgSave.EventState) and the game
/// second its offer runs out (TaskDeadlineGameSec), 0 when the offer never runs out.
/// </summary>
public readonly record struct EventText(string? State, long DeadlineGameSeconds);

/// <summary>
/// What the event badge on the phone shows: how many event texts still wait to be accepted, and whether any of them
/// runs out if left alone. Built only through <see cref="Of"/>, so a badge with nothing waiting is never urgent.
/// </summary>
public sealed record PhoneBadge
{
    /// <summary>The state the game gives an event text from delivery until it is accepted, refused or runs out.</summary>
    public const string WaitingState = "pending";

    public static readonly PhoneBadge None = new(0, false);

    private PhoneBadge(int waiting, bool hasTimeLimit)
    {
        Waiting = waiting;
        HasTimeLimit = hasTimeLimit;
    }

    public int Waiting { get; }

    /// <summary>At least one waiting text expires: the game turns it to "expired" once its deadline passes.</summary>
    public bool HasTimeLimit { get; }

    public static PhoneBadge Of(IEnumerable<EventText> texts)
    {
        var waiting = 0;
        var hasTimeLimit = false;

        foreach (var text in texts)
        {
            if (text.State != WaitingState) continue;

            waiting++;
            hasTimeLimit |= text.DeadlineGameSeconds > 0;
        }

        return waiting == 0 ? None : new PhoneBadge(waiting, hasTimeLimit);
    }
}
