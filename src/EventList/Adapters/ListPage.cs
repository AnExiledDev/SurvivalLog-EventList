using System;
using System.IO;
using System.Threading;
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
    private readonly ListSettings _settings;
    private readonly Action<string> _log;
    private readonly string _check;
    private string? _install;
    private string? _reply;
    private bool _hasReportedRunFailure;

    public ListPage(ListSettings settings, Action<string> log)
    {
        _settings = settings;
        _log = log;
        _check = PageCalls.Check(settings);
    }

    public void Tick(double now)
    {
        var turn = _watch.Step(now, Interlocked.Exchange(ref _reply, null));

        if (turn.LogLine != null) _log(turn.LogLine);

        if (turn.Send == PageSend.Nothing) return;

        var javaScript = turn.Send == PageSend.Install ? Install() : _check;

        if (Run(javaScript)) return;

        if (_watch.NotSent(now) is { } line) _log(line);
    }

    private bool Run(string javaScript)
    {
        var webView = GameSingleton.Existing<ReduxUISystem>()?.GetWebUILayer()?.canvasWebViewPrefab?.WebView;

        if (webView == null) return false;

        try
        {
            webView.ExecuteJavaScript(javaScript, (Action<string>)(result => Interlocked.Exchange(ref _reply, result ?? "")));

            return true;
        }
        catch (Exception ex)
        {
            if (!_hasReportedRunFailure) _log("could not run script in the web UI (reported once): " + ex.Message);

            _hasReportedRunFailure = true;

            return false;
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
