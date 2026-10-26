namespace EventList.Core;

/// <summary>
/// The check call as it was last built. The plugin sends it every couple of seconds and it changes only with the
/// settings or the badge, so the same text is handed back until one of them does.
/// </summary>
public sealed class CheckCall
{
    private ListSettings? _settings;
    private PhoneBadge? _badge;
    private string _call = "";

    public string For(ListSettings settings, PhoneBadge badge)
    {
        if (settings.Equals(_settings) && badge.Equals(_badge)) return _call;

        _settings = settings;
        _badge = badge;

        return _call = PageCalls.Check(settings, badge);
    }
}
