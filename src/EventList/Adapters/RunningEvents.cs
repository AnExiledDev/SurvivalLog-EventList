using System;
using System.Collections.Generic;
using EventList.Core;
using ExiledToolkit.Game;
using ExiledToolkit.Web;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;

namespace EventList.Adapters;

/// <summary>
/// Reads what the lanes show beyond the game's rows: how long each running event's countdown runs in all, and the
/// items it asks for with how many the player has. Read only: nothing in the game is written. An event's config does
/// not change while the game runs, so its length and its item list are read once per event; the counts held are read
/// again on each call, with the method the game checks the event with. A read that fails sends no facts and is
/// logged once. Main thread only.
/// </summary>
internal sealed class RunningEvents
{
    /// <summary>One of an event's item checks: the items it asks for and how many of each.</summary>
    private sealed record ItemCheck(ConditionSetTask Task, int[] ItemIds, int[] Counts);

    private sealed record EventShape(int TotalSeconds, ItemCheck[] Checks)
    {
        public bool IsEmpty => TotalSeconds <= 0 && Checks.Length == 0;
    }

    private static readonly EventShape Unknown = new(0, Array.Empty<ItemCheck>());

    private readonly LaneFacts _facts = new();
    private readonly Dictionary<int, EventShape> _shapes = new();
    private readonly Dictionary<int, string> _icons = new();
    private readonly List<int> _eventIds = new();
    private readonly Action<string> _log;
    private (IntPtr Pointer, int Version, int Count) _eventsStamp;
    private IntPtr _configsPointer;
    private bool _isFailureLogged;
    private bool _isIconNoteLogged;

    public RunningEvents(Action<string> log)
    {
        _log = log;
    }

    public string Read()
    {
        try
        {
            return ReadFacts();
        }
        catch (Exception ex)
        {
            if (!_isFailureLogged) _log("could not read the running events, so the lanes show no countdown bars or item needs (logged once): " + ex.Message);

            _isFailureLogged = true;

            return LaneFacts.None;
        }
    }

    private string ReadFacts()
    {
        var manager = GameSingleton.Existing<BattleLogicWorld>()?._DynamicEventManager;
        var events = manager?.CurrentArgsDict;
        var configs = manager?.Configs;

        if (events == null || configs == null) return LaneFacts.None;

        KeepShapesOf(configs);
        ReadEventIds(events);
        _facts.Begin();

        for (var i = 0; i < _eventIds.Count; i++)
        {
            var id = _eventIds[i];
            var shape = ShapeOf(id, configs);

            if (shape.IsEmpty) continue;

            _facts.Event(id, shape.TotalSeconds);
            AddNeeds(shape);
        }

        return _facts.End();
    }

    /// <summary>The running events change only when one starts or ends, so they are walked again only when the game's dictionary of them changes.</summary>
    private void ReadEventIds(Il2CppSystem.Collections.Generic.Dictionary<long, DynamicEventArgs> events)
    {
        var stamp = (events.Pointer, events._version, events.Count);

        if (stamp == _eventsStamp) return;

        _eventsStamp = stamp;
        _eventIds.Clear();

        foreach (var pair in events)
        {
            if (pair.Value != null && !_eventIds.Contains(pair.Value.EventId)) _eventIds.Add(pair.Value.EventId);
        }
    }

    private EventShape ShapeOf(int id, Il2CppSystem.Collections.Generic.Dictionary<int, DynamicEventConfig> configs)
    {
        if (_shapes.TryGetValue(id, out var known)) return known;

        var shape = configs.TryGetValue(id, out var config) && config != null ? new EventShape(config.TaskTime, ItemChecks(config)) : Unknown;

        return _shapes[id] = shape;
    }

    /// <summary>
    /// The carried items (bagRequireList) and the stored ones (itemNeedList) of each item check; an id listed twice
    /// means two of it. The game counts both lists into one map from item to count, keeping the higher count for an id in
    /// both, so the two lists are added up the same way.
    /// </summary>
    private static ItemCheck[] ItemChecks(DynamicEventConfig config)
    {
        var tasks = config.DynamicTasks;
        var checks = new List<ItemCheck>();

        for (var t = 0; tasks != null && t < tasks.Count; t++)
        {
            var task = tasks[t]?.TryCast<ConditionSetTask>();

            if (task == null) continue;

            var counts = new Dictionary<int, int>();
            var order = new List<int>();

            CountItems(task.bagRequireList, counts, order);
            CountItems(task.itemNeedList, counts, order);

            if (order.Count == 0) continue;

            var wanted = new int[order.Count];

            for (var i = 0; i < order.Count; i++) wanted[i] = counts[order[i]];

            checks.Add(new ItemCheck(task, order.ToArray(), wanted));
        }

        return checks.ToArray();
    }

    private static void CountItems(Il2CppSystem.Collections.Generic.List<int>? items, Dictionary<int, int> counts, List<int> order)
    {
        for (var i = 0; items != null && i < items.Count; i++)
        {
            var id = items[i];

            if (counts.TryGetValue(id, out var count))
            {
                counts[id] = count + 1;
            }
            else
            {
                counts[id] = 1;
                order.Add(id);
            }
        }
    }

    /// <summary>GetBagProgress hands back a new dictionary each call, one per item check per call to the page.</summary>
    private void AddNeeds(EventShape shape)
    {
        for (var c = 0; c < shape.Checks.Length; c++)
        {
            var check = shape.Checks[c];
            var held = check.Task.GetBagProgress(null);

            for (var i = 0; i < check.ItemIds.Length; i++)
            {
                var id = check.ItemIds[i];
                var have = held != null && held.TryGetValue(id, out var count) ? count : 0;

                _facts.Need(IconOf(id), check.Counts[i], have);
            }
        }
    }

    /// <summary>The item's web icon. Until the icon folder is read, an item has none and is looked up again next call.</summary>
    private string IconOf(int itemId)
    {
        if (_icons.TryGetValue(itemId, out var icon)) return icon;

        var scan = WebIcons.StartScan();

        if (!scan.IsCompleted) return "";

        if (!_isIconNoteLogged && scan.Result.Note.Length > 0) _log("item icons: " + scan.Result.Note);

        _isIconNoteLogged = true;

        var items = GameSingleton.Existing<ConfigManager>()?._Config_Item_Dict;
        var path = items != null && items.TryGetValue(itemId, out var row) && row != null ? WebIcons.Icons.Resolve(GameNames.NonEmpty(row.WebIcon) ?? GameNames.NonEmpty(row.Icon)) : null;

        return _icons[itemId] = path ?? "";
    }

    /// <summary>Loading a save makes a new event manager with configs of its own, so what was read from the last ones is dropped.</summary>
    private void KeepShapesOf(Il2CppSystem.Collections.Generic.Dictionary<int, DynamicEventConfig> configs)
    {
        if (configs.Pointer == _configsPointer) return;

        _configsPointer = configs.Pointer;
        _shapes.Clear();
    }
}
