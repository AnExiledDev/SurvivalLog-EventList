using System;
using System.IO;
using EventList.Core;
using GameCore.HotUpdate.ReduxUI;

namespace EventList.Adapters;

/// <summary>
/// The plugin's only contact with the game: it runs JavaScript in the game's web view and reads the answer.
/// Nothing in the game's state is read or written. All members run on Unity's main thread; the web view's
/// callback only stores the answer, and the next frame hands it to <see cref="PageWatch"/>.
/// </summary>
internal sealed class ListPage
{
    private const string ScriptResource = "EventList.eventlist.js";

    private readonly PageWatch _watch = new();
    private readonly ReplySlot _replies = new();
    private readonly ListSettings _settings;
    private readonly Action<string> _log;
    private readonly string _check;
    private string? _install;

    public ListPage(ListSettings settings, Action<string> log)
    {
        _settings = settings;
        _log = log;
        _check = PageCalls.Check(settings);
    }

    public void Tick(double now)
    {
        var turn = _watch.Step(now, _replies.Take());

        if (turn.LogLine != null) _log(turn.LogLine);

        if (turn.Send == PageSend.Nothing) return;

        var javaScript = turn.Send == PageSend.Install ? Install() : _check;

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

    /// <summary>Built on first need: most launches send it once, and some never reach a page that wants it.</summary>
    private string Install()
    {
        if (_install != null) return _install;

        using var stream = typeof(ListPage).Assembly.GetManifestResourceStream(ScriptResource) ?? throw new InvalidOperationException(ScriptResource + " is not embedded");
        using var reader = new StreamReader(stream);

        return _install = PageCalls.Install(reader.ReadToEnd(), _settings);
    }
}
