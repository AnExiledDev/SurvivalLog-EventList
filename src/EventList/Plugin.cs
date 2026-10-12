using System;
using System.IO;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using EventList.Adapters;
using EventList.Core;

namespace EventList;

[BepInPlugin(Guid, "Event List", Version)]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.eventlist";
    private const string Version = "0.3.0";

    public override void Load()
    {
        var isEnabled = Config.Bind("EventList", "Enabled", true,
            "Hold the events list on the left of the HUD to a fixed height and let the rest scroll. Off: the list is left exactly as the game draws it.").Value;
        var maxHeight = Config.Bind("EventList", "MaxHeight", ListSettings.DefaultMaxHeight,
            $"Tallest the events list may grow, in the page's pixels (the game's own scroll box stops at 220). 0 means the default, {ListSettings.DefaultMaxHeight}, room for both group headers and four rows. Held between {ListSettings.LowestMaxHeight} and {ListSettings.HighestMaxHeight}.").Value;
        var isImportantOnTop = Config.Bind("EventList", "ImportantOnTop", true,
            "Move important events to the top of the list: an event the game shows as an emergency or a zombie wave first, then events with a running countdown (see CountdownOnTop). Off: the game's order.").Value;
        var isCountdownOnTop = Config.Bind("EventList", "CountdownOnTop", true,
            "Count an event with a running countdown as important, so it moves to the top, below emergencies. Off: only emergencies move. Does nothing while ImportantOnTop is off.").Value;
        var isCountdownSoonestFirst = Config.Bind("EventList", "CountdownSoonestFirst", true,
            "Order the countdowns on top by time left, the one about to run out first. Countdowns with the same time left, and any whose time cannot be read, keep the game's order, the unreadable ones last. Off: countdowns keep the game's order among themselves. Does nothing while CountdownOnTop or ImportantOnTop is off.").Value;
        var isPhoneBadgeOn = Config.Bind("EventList", "PhoneEventBadge", true,
            "Show a second badge on the phone button, and on the messages app inside the phone, counting event texts still waiting to be accepted. It turns orange with an hourglass when one of them has a countdown. The game's own text badge is left as it is. Off: no event badge. Does nothing while Enabled is off.").Value;
        var log = new ListLog(Log, Path.Combine(Paths.BepInExRootPath, "event-list.log"));
        var settings = ListSettings.From(maxHeight, isImportantOnTop, isCountdownOnTop, isCountdownSoonestFirst);

        if (!isEnabled)
        {
            log.Write($"===== Event List {Version} started: off, EventList/Enabled is false; the list is left as the game draws it");

            return;
        }

        log.Write($"===== Event List {Version} started: {settings.Describe()}, " + (isPhoneBadgeOn ? "event badge on the phone" : "no event badge"));

        ListTicker.Log = log.Write;
        ListTicker.Page = new ListPage(settings, BadgeSource(isPhoneBadgeOn, log.Write), log.Write);

        AddComponent<ListTicker>();
    }

    /// <summary>With the badge off the phone's texts are never read, and the page is told nothing waits.</summary>
    private static Func<PhoneBadge> BadgeSource(bool isPhoneBadgeOn, Action<string> log)
    {
        if (!isPhoneBadgeOn) return () => PhoneBadge.None;

        return new PhoneTexts(log).Read;
    }
}
