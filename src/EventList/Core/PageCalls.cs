using System.Globalization;
using System.Text;

namespace EventList.Core;

/// <summary>
/// The JavaScript the plugin runs in the game's web view. The web view runs it in Root.html; the events list and the
/// phone button live in the CoreUI1 page, an iframe inside Root, so every call first finds that frame and then talks to
/// eventlist.js inside it. The phone's own screen is the PhoneSMS page, another iframe; when it is there, its document
/// is handed to the script too, so the event badge can sit on the messages app. Each call answers with one of the
/// <see cref="PageAnswer"/> strings.
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

    /// <summary>The phone's screen, when the game has it in the web UI: its document, or null.</summary>
    private const string FindPhone =
        "var phone=null;" +
        "for(var j=0;j<fs.length;j++){var p=fs[j].contentWindow;if(!p)continue;" +
        "try{if(/PhoneSMS\\.html/i.test(String(p.location))){phone=p.document;break;}}catch(e){}}";

    /// <summary>Cheap enough to send every couple of seconds: says whether the script is there, and hands it the settings and the badge again.</summary>
    public static string Check(ListSettings settings, PhoneBadge badge) =>
        Wrap($"if(!{Api})return '{PageAnswer.Missing}';{Apply(settings, badge)}");

    /// <summary>Evaluates the script inside the CoreUI1 frame, so it sees that page's Vue app, state and click-through.</summary>
    public static string Install(string script, ListSettings settings, PhoneBadge badge) =>
        Wrap($"w.eval({Quote(script)});if(!{Api})return '{PageAnswer.InstallLeftNothing}';{Apply(settings, badge)}");

    public static string Settings(ListSettings settings) =>
        $"{{maxHeight:{Number(settings.MaxHeight)},importantOnTop:{Bool(settings.IsImportantOnTop)},countdownOnTop:{Bool(settings.IsCountdownOnTop)},soonestFirst:{Bool(settings.IsCountdownSoonestFirst)}}}";

    /// <summary>The event badge as the page script reads it: how many event texts wait, and whether one of them runs out.</summary>
    public static string Badge(PhoneBadge badge) =>
        $"{{waiting:{Number(badge.Waiting)},timed:{Bool(badge.HasTimeLimit)}}}";

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

    private static string Apply(ListSettings settings, PhoneBadge badge) =>
        FindPhone + $"return String({Api}.apply({Settings(settings)},{Badge(badge)},phone));";

    private static string Wrap(string body) => "(function(){try{" + FindFrame + body + "}catch(e){return 'error: '+e;}})()";

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Bool(bool value) => value ? "true" : "false";
}
