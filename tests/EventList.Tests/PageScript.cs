using Jint;

namespace EventList.Tests;

/// <summary>
/// eventlist.js exactly as the plugin embeds it, run in Jint on top of FakePage.js, a page with only the parts of
/// CoreUI1 the script touches. Tests talk to it in JavaScript expressions and read back plain strings, numbers and
/// booleans.
/// </summary>
internal sealed class PageScript
{
    private readonly Engine _engine = new();

    private PageScript(string? pageSpec)
    {
        _engine.Execute(Resource("EventList.Tests.FakePage.js"));

        if (pageSpec != null) _engine.Execute("var page = buildPage(" + pageSpec + ");");

        _engine.Execute(Resource("EventList.eventlist.js"));
    }

    /// <summary>The script with no events list in the page, for the rules it exports.</summary>
    public static PageScript Alone() => new(null);

    /// <summary>The script in a page drawn from a buildPage spec; the page is the global <c>page</c>.</summary>
    public static PageScript On(string pageSpec) => new(pageSpec);

    /// <summary>What the plugin's check call does once it has found the frame.</summary>
    public string Apply(string settings) => Text($"window.__sleventlist.apply({settings})");

    /// <summary>The check call with the event badge, and the phone's document (a JavaScript expression, or "null").</summary>
    public string Apply(string settings, string badge, string phoneDocument) =>
        Text($"window.__sleventlist.apply({settings}, {badge}, {phoneDocument})");

    public void Run(string statements) => _engine.Execute(statements);

    public string Text(string expression) => _engine.Evaluate($"String({expression})").AsString();

    public bool Is(string expression) => _engine.Evaluate($"!!({expression})").AsBoolean();

    public double Number(string expression) => _engine.Evaluate($"Number({expression})").AsNumber();

    private static string Resource(string name)
    {
        using var stream = typeof(PageScript).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException(name + " is not embedded in the test assembly");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
