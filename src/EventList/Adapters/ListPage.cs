using System;
using System.IO;
using EventList.Core;
using GameCore.HotUpdate.ReduxUI;

namespace EventList.Adapters;

/// <summary>
/// The plugin's contact with the game's web view: it runs JavaScript there and reads the answer. Each call carries
/// the event badge as it is at that moment, read through the badge source; nothing in the game's state is written.
/// All members run on Unity's main thread; the web view's callback only stores the answer, and the next frame hands
/// it to <see cref="PageWatch"/>.
/// </summary>
internal sealed class ListPage
{
    private const string ScriptResource = "EventList.eventlist.js";

    private readonly PageWatch _watch = new();
    private readonly ReplySlot _replies = new();
    private ListSettings _settings;
    private Func<PhoneBadge> _readBadge;
    private readonly Action<string> _log;
    private string? _script;

    public ListPage(ListSettings settings, Func<PhoneBadge> readBadge, Action<string> log)
    {
        _settings = settings;
        _readBadge = readBadge;
        _log = log;
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

        var badge = _readBadge();
        var javaScript = turn.Send == PageSend.Install ? PageCalls.Install(Script(), _settings, badge) : PageCalls.Check(_settings, badge);

        if (Run(javaScript, now) is { } line) _log(line);
    }

    /// <summary>Sends the call; returns a line for the log when it could not leave.</summary>
    private string? Run(string javaScript, double now)
    {
        var webView = GameSingleton.Existing<ReduxUISystem>()?.GetWebUILayer()?.canvasWebViewPrefab?.WebView;

        if (webView == null) return _watch.NotSent(now);

        var call = _replies.Open();

        try
        {
            webView.ExecuteJavaScript(javaScript, (Action<string>)(result => _replies.Offer(call, result ?? "")));

            return null;
        }
        catch (Exception ex)
        {
            return _watch.RunFailed(now, ex.Message);
        }
    }

    /// <summary>Read on first need: most launches send it once, and some never reach a page that wants it.</summary>
    private string Script()
    {
        if (_script != null) return _script;

        using var stream = typeof(ListPage).Assembly.GetManifestResourceStream(ScriptResource) ?? throw new InvalidOperationException(ScriptResource + " is not embedded");
        using var reader = new StreamReader(stream);

        return _script = reader.ReadToEnd();
    }
}
