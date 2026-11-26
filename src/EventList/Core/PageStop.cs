namespace EventList.Core;

/// <summary>
/// The mod's on/off switch. While it is off, nothing is sent to the page but one stop call, made after the player
/// turns the mod off, so the page script gives the list back to the game and takes its badges away. A call that could
/// not leave, or got no answer, is tried again a couple of seconds later. Any answer from the page ends it: a script
/// that failed to stop says so in the log and the next page load clears it anyway.
/// </summary>
public sealed class PageStop
{
    public const double RetrySeconds = 2;

    private bool _isWanted;
    private bool _isAwaiting;
    private double _answerDueAt;
    private double _nextTryAt;

    public PageStop(bool isOn)
    {
        IsOn = isOn;
    }

    public bool IsOn { get; private set; }

    /// <summary>Returns true when the mod was off and is now on, so the page watch starts over.</summary>
    public bool Set(bool isOn)
    {
        if (isOn == IsOn) return false;

        IsOn = isOn;
        _isWanted = !isOn;
        _isAwaiting = false;
        _nextTryAt = 0;

        return isOn;
    }

    /// <summary>One step per frame while off. Returns true when the stop call should be sent now.</summary>
    public bool Step(double now, string? reply, out string? logLine)
    {
        logLine = null;

        if (_isAwaiting)
        {
            if (reply == null && now < _answerDueAt) return false;

            _isAwaiting = false;

            if (reply == null)
            {
                _nextTryAt = now + RetrySeconds;

                return false;
            }

            _isWanted = false;
            logLine = reply is PageAnswer.Stopped or PageAnswer.NoPage ? "off: the events list is back the way the game draws it" : "off, but the page script could not give the list back: " + reply;

            return false;
        }

        if (!_isWanted || now < _nextTryAt) return false;

        _isAwaiting = true;
        _answerDueAt = now + PageWatch.AnswerWaitSeconds;

        return true;
    }

    /// <summary>The stop call never left: the web view is not up yet, or it threw. Tried again shortly.</summary>
    public void NotSent(double now)
    {
        _isAwaiting = false;
        _nextTryAt = now + RetrySeconds;
    }
}
