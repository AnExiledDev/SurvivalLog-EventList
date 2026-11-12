using System;
using System.IO;
using EventList.Core;
using ExiledToolkit.Game;
using ExiledToolkit.Kit;
using ExiledToolkit.Web;

namespace EventList.Adapters;

/// <summary>
/// The plugin's contact with the game's web view: it runs JavaScript there and reads the answer. Each call carries
/// the event badge as it is at that moment, read through the badge source, and in the lanes layout what the plugin
/// read about the running events; nothing in the game's state is written.
/// All members run on Unity's main thread; the web view's callback only stores the answer, and the next frame hands
/// it to <see cref="PageWatch"/>.
/// </summary>
internal sealed class ListPage
{
    private const string ScriptResource = "EventList.eventlist.js";

    /// <summary>Sent just before eventlist.js, which draws the lanes with it.</summary>
    private const string LanesResource = "EventList.lanes.js";

    private readonly PageWatch _watch = new();
    private readonly ReplySlot _replies = new();
    private readonly CheckCall _check = new();
    private readonly RunningEvents _events;
    private ListSettings _settings;
    private Func<PhoneBadge> _readBadge;
    private readonly Action<string> _log;
    private string? _script;

    public ListPage(ListSettings settings, Func<PhoneBadge> readBadge, Action<string> log)
    {
        _settings = settings;
        _readBadge = readBadge;
        _log = log;
        _events = new RunningEvents(log);
    }

    /// <summary>Called on the main thread at start and after each change; the next call to the page carries them.</summary>
    public void Use(ListSettings settings, Func<PhoneBadge> readBadge)
    {
        _settings = settings;
        _readBadge = readBadge;
    }

    public void Tick(double now)
    {
        var turn = _watch.Step(now, _replies.Take());

        if (turn.LogLine != null) _log(turn.LogLine);

        if (turn.Send == PageSend.Nothing) return;

        _settings = _settings.WithChinese(GameLanguage.IsChinese());

        var badge = _readBadge();
        var facts = _settings.Layout == ListLayout.Lanes ? _events.Read() : LaneFacts.None;
        var javaScript = turn.Send == PageSend.Install ? PageCalls.Install(Script(), _settings, badge, facts) : _check.For(_settings, badge, facts);

        if (Run(javaScript, now) is { } line) _log(line);
    }

    /// <summary>Sends the call; returns a line for the log when it could not leave.</summary>
    private string? Run(string javaScript, double now)
    {
        if (WebUi.TryRun(javaScript, _replies, out var failure)) return null;

        return failure == null ? _watch.NotSent(now) : _watch.RunFailed(now, failure.Message);
    }

    /// <summary>Read on first need: most launches send it once, and some never reach a page that wants it.</summary>
    private string Script() => _script ??= Resource(LanesResource) + "\n" + Resource(ScriptResource);

    private static string Resource(string name)
    {
        using var stream = typeof(ListPage).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException(name + " is not embedded");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
