using System.Globalization;
using System.Text;

namespace EventList.Core;

/// <summary>
/// The JavaScript the plugin runs in the game's web view. The web view runs it in Root.html; the events list lives in
/// the CoreUI1 page, an iframe inside Root, so every call first finds that frame and then talks to eventlist.js inside it.
/// Each call answers with one of the <see cref="PageAnswer"/> strings.
/// </summary>
public static class PageCalls
{
    private const string Api = "w.__sleventlist";

    /// <summary>The frame search the other CoreUI1 mods use: the iframe whose page is CoreUI1.html.</summary>
    private const string FindFrame =
        "var fs=document.querySelectorAll('iframe'),w=null;" +
        "for(var i=0;i<fs.length;i++){var c=fs[i].contentWindow;if(!c)continue;" +
        "try{if(!/CoreUI1\\.html/i.test(String(c.location)))continue;}catch(e){continue;}w=c;break;}" +
        "if(!w)return '" + PageAnswer.NoPage + "';";

    /// <summary>Cheap enough to send every couple of seconds: says whether the script is there, and hands it the settings again.</summary>
    public static string Check(ListSettings settings) =>
        Wrap($"if(!{Api})return '{PageAnswer.Missing}';return String({Api}.apply({Settings(settings)}));");

    /// <summary>Evaluates the script inside the CoreUI1 frame, so it sees that page's Vue app, state and click-through.</summary>
    public static string Install(string script, ListSettings settings) =>
        Wrap($"w.eval({Quote(script)});if(!{Api})return '{PageAnswer.InstallLeftNothing}';return String({Api}.apply({Settings(settings)}));");

    public static string Settings(ListSettings settings) =>
        $"{{maxHeight:{Number(settings.MaxHeight)},importantOnTop:{Bool(settings.IsImportantOnTop)}}}";

    /// <summary>A JavaScript string literal that no quote, backslash or line break in the text can end early.</summary>
    public static string Quote(string text)
    {
        var literal = new StringBuilder(text.Length + 2).Append('"');

        foreach (var c in text)
        {
            switch (c)
            {
                case '"':
                    literal.Append("\\\"");
                    break;
                case '\\':
                    literal.Append("\\\\");
                    break;
                case < ' ' or '\u2028' or '\u2029':
                    literal.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    break;
                default:
                    literal.Append(c);
                    break;
            }
        }

        return literal.Append('"').ToString();
    }

    private static string Wrap(string body) => "(function(){try{" + FindFrame + body + "}catch(e){return 'error: '+e;}})()";

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Bool(bool value) => value ? "true" : "false";
}
