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
    private const string Version = "0.1.0";

    public override void Load()
    {
        var isEnabled = Config.Bind("EventList", "Enabled", true,
            "Hold the events list on the left of the HUD to a fixed height and let the rest scroll. Off: the list is left exactly as the game draws it.").Value;
        var maxHeight = Config.Bind("EventList", "MaxHeight", ListSettings.DefaultMaxHeight,
            $"Tallest the events list may grow, in the page's pixels (the game's own scroll box stops at 220). 0 means the default, {ListSettings.DefaultMaxHeight}, about the game's four-row list. Held between {ListSettings.LowestMaxHeight} and {ListSettings.HighestMaxHeight}.").Value;
        var isImportantOnTop = Config.Bind("EventList", "ImportantOnTop", true,
            "Move important events to the top of the list: an event the game shows as an emergency or a zombie wave first, then events with a running countdown. Off: the game's order.").Value;
        var log = new ListLog(Log, Path.Combine(Paths.BepInExRootPath, "event-list.log"));
        var settings = ListSettings.From(maxHeight, isImportantOnTop);

        if (!isEnabled)
        {
            log.Write($"===== Event List {Version} started: off, EventList/Enabled is false; the list is left as the game draws it");

            return;
        }

        log.Write($"===== Event List {Version} started: {settings.Describe()}");

        ListTicker.Log = log.Write;
        ListTicker.Page = new ListPage(settings, log.Write);

        AddComponent<ListTicker>();
    }
}
