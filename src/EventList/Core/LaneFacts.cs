using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ExiledToolkit.Web;

namespace EventList.Core;

/// <summary>
/// What the lanes know about each running event beyond its row: how long its countdown runs in all, in game seconds,
/// and the items it asks for, with how many the player has. Filled again before each call to the page, between
/// <see cref="Begin"/> and <see cref="End"/>, into buffers kept from call to call; the JavaScript literal is rebuilt
/// only when something in it changed, so most calls get the same string back. Main thread only.
/// </summary>
public sealed class LaneFacts
{
    /// <summary>No facts: the lanes draw without drain bars or item needs.</summary>
    public const string None = "{}";

    // Per event: id, total seconds, how many needs follow; then per need: icon index, need, have.
    private List<int> _numbers = new();
    private List<string> _icons = new();
    private List<int> _sentNumbers = new();
    private List<string> _sentIcons = new();
    private readonly StringBuilder _text = new();
    private int _needCountAt = -1;

    public string Literal { get; private set; } = None;

    public void Begin()
    {
        _numbers.Clear();
        _icons.Clear();
        _needCountAt = -1;
    }

    /// <summary>A running event. 0 or less seconds: it has no countdown.</summary>
    public void Event(int id, int totalSeconds)
    {
        _numbers.Add(id);
        _numbers.Add(Math.Max(0, totalSeconds));
        _needCountAt = _numbers.Count;
        _numbers.Add(0);
    }

    /// <summary>An item the last event asks for. The icon is a path under the web UI folder, or empty when the game has none.</summary>
    public void Need(string? icon, int need, int have)
    {
        if (_needCountAt < 0 || need < 1) return;

        _numbers[_needCountAt]++;
        _numbers.Add(_icons.Count);
        _icons.Add(icon ?? "");
        _numbers.Add(need);
        _numbers.Add(Math.Clamp(have, 0, need));
    }

    /// <summary>The facts as a JavaScript object literal; the same string as last time while nothing changed.</summary>
    public string End()
    {
        if (IsSame(_numbers, _sentNumbers) && IsSame(_icons, _sentIcons)) return Literal;

        (_sentNumbers, _numbers) = (_numbers, _sentNumbers);
        (_sentIcons, _icons) = (_icons, _sentIcons);

        return Literal = Build(_sentNumbers, _sentIcons);
    }

    private string Build(List<int> numbers, List<string> icons)
    {
        _text.Clear().Append('{');

        for (var i = 0; i + 2 < numbers.Count;)
        {
            var needCount = numbers[i + 2];

            if (_text.Length > 1) _text.Append(',');

            _text.Append('"').Append(Number(numbers[i])).Append("\":{total:").Append(Number(numbers[i + 1])).Append(",needs:[");
            i += 3;

            for (var n = 0; n < needCount && i + 2 < numbers.Count; n++, i += 3)
            {
                if (n > 0) _text.Append(',');

                _text.Append('[').Append(JsText.Literal(icons[numbers[i]])).Append(',').Append(Number(numbers[i + 1])).Append(',').Append(Number(numbers[i + 2])).Append(']');
            }

            _text.Append("]}");
        }

        return _text.Append('}').ToString();
    }

    private static bool IsSame<T>(List<T> now, List<T> sent)
    {
        if (now.Count != sent.Count) return false;

        var same = EqualityComparer<T>.Default;

        for (var i = 0; i < now.Count; i++)
        {
            if (!same.Equals(now[i], sent[i])) return false;
        }

        return true;
    }

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
