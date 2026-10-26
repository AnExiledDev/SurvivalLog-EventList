using EventList.Core;
using Xunit;

namespace EventList.Tests;

/// <summary>
/// The game builds a new phone page each time the phone opens and shows it two frames later, while the plugin calls
/// apply only every couple of seconds. The event badge on the messages app must be there by the time the page shows.
/// Each test sends one apply before the phone opens, with no phone screen, and none after unless it says so.
/// </summary>
public class PhoneOpeningTests
{
    private const string PhoneUp = "{groups: [[{eventConfigId: 101, text: 'Repair the gate'}]], phoneActive: true}";

    private const string AppBadge = "phone.smsApp.querySelector('.sl-eventbadge')";

    private static readonly string Defaults = PageCalls.Settings(ListSettings.From(0, true, true, true));

    private static PageScript Applied(int waiting)
    {
        var page = PageScript.InRoot(PhoneUp);

        page.Apply(Defaults, $"{{waiting: {waiting}, timed: false}}", "null");

        return page;
    }

    /// <summary>The phone opening: Root makes the frame, the page arrives a frame later, and the game builds it.</summary>
    private static void OpenPhone(PageScript page)
    {
        page.Run("var frame = openFrame(PHONE_URL); flush(); nextFrame();");
        page.Run("var doc = arrive(frame); nextFrame(); buildPhone(doc); flush();");
    }

    [Fact]
    public void A_phone_that_just_opened_shows_the_last_badge_before_the_next_paint()
    {
        var page = Applied(3);

        OpenPhone(page);

        Assert.True(page.Is(AppBadge), "the messages app got no event badge");
        Assert.Equal("3", page.Text(AppBadge + ".textContent"));
    }

    [Fact]
    public void A_phone_page_that_is_already_built_when_it_arrives_gets_the_badge_by_the_next_frame()
    {
        var page = Applied(2);

        page.Run("var frame = openFrame(PHONE_URL); flush(); buildPhone(arrive(frame)); nextFrame();");

        Assert.Equal("2", page.Text(AppBadge + ".textContent"));
    }

    [Fact]
    public void A_phone_page_loaded_into_the_frame_again_is_watched_again()
    {
        var page = Applied(1);

        OpenPhone(page);
        page.Run("navigate(frame, PHONE_URL + '?t=2'); flush(); var reloaded = arrive(frame); fireLoad(frame); flush(); buildPhone(reloaded); flush();");

        Assert.Equal("1", page.Text(AppBadge + ".textContent"));
    }

    [Fact]
    public void The_badge_drawn_early_is_the_count_from_the_latest_apply()
    {
        var page = Applied(4);

        page.Apply(Defaults, "{waiting: 6, timed: false}", "null");
        OpenPhone(page);

        Assert.Equal("6", page.Text(AppBadge + ".textContent"));
    }

    [Fact]
    public void With_nothing_waiting_the_phone_stays_as_the_game_made_it()
    {
        var page = Applied(0);

        OpenPhone(page);

        Assert.False(page.Is(AppBadge));
    }

    [Fact]
    public void The_watcher_and_the_plugins_call_together_draw_one_badge()
    {
        var page = Applied(2);

        OpenPhone(page);
        page.Apply(Defaults, "{waiting: 2, timed: false}", "phone.document");
        page.Run("flush(); nextFrame();");

        Assert.Equal(1.0, page.Number("phone.smsApp.querySelectorAll('.sl-eventbadge').length"));
    }

    [Fact]
    public void Other_pages_are_left_alone()
    {
        var page = Applied(2);

        page.Run("var frame = openFrame('https://ui.local/UI/Bag/Bag.html'); flush(); buildPhone(arrive(frame)); nextFrame(); flush();");

        Assert.False(page.Is(AppBadge));
    }
}
