using EventList.Core;
using Xunit;

namespace EventList.Tests;

/// <summary>
/// The event badge eventlist.js draws on the HUD's phone button and on the messages app inside the phone, driven
/// through apply the way the plugin's check call drives it.
/// </summary>
public class PhoneBadgePageTests
{
    /// <summary>A short list, with the game showing its phone button and its own red badge for two unread texts.</summary>
    private const string PhoneUp = "{groups: [[{eventConfigId: 101, text: 'Repair the gate'}]], phoneActive: true, gameBadge: true}";

    private const string PhoneButtonGone = "{groups: [[{eventConfigId: 101, text: 'Repair the gate'}]], phoneActive: true, phoneButtonGone: true}";

    private const string PhoneDown = "{groups: [[{eventConfigId: 101, text: 'Repair the gate'}]]}";

    private const string NoPhoneScreen = "null";

    private const string HudBadge = "page.phoneButton.querySelector('.sl-eventbadge')";

    private const string AppBadge = "phone.smsApp.querySelector('.sl-eventbadge')";

    /// <summary>
    /// Root.html's iframes, in the global <c>root</c>: one that throws on reading its location (as a frame from another
    /// origin does), CoreUI1 (this script's own page), then the phone, in the global <c>phone</c>.
    /// </summary>
    private const string RootWithThreeFrames =
        "var phone = buildPhonePage({});" +
        "window.location = 'https://ui.local/CoreUI1.html';" +
        "var rootFrames = [" +
        "{ contentWindow: { get location() { throw new Error('Blocked a frame from accessing a cross-origin frame'); } } }," +
        "{ contentWindow: window }," +
        "{ contentWindow: { location: 'https://ui.local/PhoneSMS.html', document: phone.document } }];" +
        "var root = { querySelectorAll: function (selector) { return selector === 'iframe' ? rootFrames : []; } };";

    private static readonly string Defaults = PageCalls.Settings(ListSettings.From(0, true, true, true));

    private static readonly string Clock = ((char)0x231B).ToString();

    [Fact]
    public void Waiting_event_texts_show_their_count_on_the_phone_button()
    {
        var page = PageScript.On(PhoneUp);

        Assert.Equal(PageAnswer.Ok, page.Apply(Defaults, Badge(2, false), NoPhoneScreen));
        Assert.Equal("2", page.Text(HudBadge + ".textContent"));
        Assert.False(page.Is(HudBadge + ".classList.contains('is-timed')"));
        Assert.True(page.Is("document.getElementById('sl-eventbadge-style')"));
    }

    /// <summary>The player asked for the event badge beside the text badge, never in place of it.</summary>
    [Fact]
    public void The_games_own_text_badge_is_left_as_it_is()
    {
        var page = PageScript.On(PhoneUp);

        page.Apply(Defaults, Badge(2, true), NoPhoneScreen);

        Assert.Equal(1.0, page.Number("page.phoneButton.querySelectorAll('.phone-badge').length"));
        Assert.Equal("2", page.Text("page.phoneButton.querySelector('.phone-badge').textContent"));
        Assert.False(page.Is("page.phoneButton.querySelector('.phone-badge').classList.contains('sl-eventbadge')"));
    }

    [Fact]
    public void An_event_text_that_can_run_out_makes_the_badge_urgent_with_a_clock()
    {
        var page = PageScript.On(PhoneUp);

        page.Apply(Defaults, Badge(2, true), NoPhoneScreen);

        Assert.True(page.Is(HudBadge + ".classList.contains('is-timed')"));
        Assert.Equal(Clock + "2", page.Text(HudBadge + ".textContent"));
    }

    [Fact]
    public void The_badge_calms_down_once_no_waiting_text_can_run_out()
    {
        var page = PageScript.On(PhoneUp);

        page.Apply(Defaults, Badge(2, true), NoPhoneScreen);
        page.Apply(Defaults, Badge(1, false), NoPhoneScreen);

        Assert.False(page.Is(HudBadge + ".classList.contains('is-timed')"));
        Assert.Equal("1", page.Text(HudBadge + ".textContent"));
        Assert.Equal(1.0, page.Number("document.querySelectorAll('.sl-eventbadge').length"));
    }

    [Fact]
    public void The_badge_goes_away_when_no_event_text_waits()
    {
        var page = PageScript.On(PhoneUp);

        page.Apply(Defaults, Badge(2, true), NoPhoneScreen);

        Assert.Equal(PageAnswer.Ok, page.Apply(Defaults, Badge(0, false), NoPhoneScreen));
        Assert.False(page.Is(HudBadge));
        Assert.True(page.Is("page.phoneButton.querySelector('.phone-badge')"));
    }

    /// <summary>The game's button clips what sits past its edge; the badge needs it open, and only while it shows.</summary>
    [Fact]
    public void The_phone_button_is_opened_for_the_badge_and_given_back_after()
    {
        var page = PageScript.On(PhoneUp);

        page.Apply(Defaults, Badge(2, false), NoPhoneScreen);

        Assert.Equal("visible", page.Text("page.phoneButton.style.getPropertyValue('overflow')"));

        page.Apply(Defaults, Badge(0, false), NoPhoneScreen);

        Assert.False(page.Is("'overflow' in page.phoneButton.styles"));
    }

    /// <summary>The plugin calls every couple of seconds; the HUD is searched for the phone button only when it is new.</summary>
    [Fact]
    public void A_phone_button_once_found_is_not_searched_for_again()
    {
        var page = PageScript.On(PhoneUp);

        page.Apply(Defaults, Badge(2, false), NoPhoneScreen);
        page.Run("var searches = countSearches(document, '.icon-sq > .icon-svg');");
        page.Apply(Defaults, Badge(3, false), NoPhoneScreen);
        page.Apply(Defaults, Badge(1, true), NoPhoneScreen);

        Assert.Equal(0.0, page.Number("searches.count"));
        Assert.Equal(Clock + "1", page.Text(HudBadge + ".textContent"));
    }

    [Fact]
    public void A_phone_button_the_game_draws_again_gets_the_badge()
    {
        var page = PageScript.On(PhoneUp);

        page.Apply(Defaults, Badge(2, false), NoPhoneScreen);
        page.Run("var tools = page.phoneButton.parentElement; tools.removeChild(page.phoneButton);" +
                 "page.phoneButton = tools.appendChild(hudButton(PHONE_ICON_PATH, false));");
        page.Apply(Defaults, Badge(2, false), NoPhoneScreen);

        Assert.Equal("2", page.Text(HudBadge + ".textContent"));
    }

    [Fact]
    public void Only_the_phone_button_carries_the_badge()
    {
        var page = PageScript.On(PhoneUp);

        page.Apply(Defaults, Badge(2, false), NoPhoneScreen);

        Assert.Equal(1.0, page.Number("document.querySelectorAll('.sl-eventbadge').length"));
    }

    [Fact]
    public void More_than_nine_waiting_texts_read_nine_plus_as_the_games_badges_do()
    {
        var page = PageScript.On(PhoneUp);

        page.Apply(Defaults, Badge(12, false), NoPhoneScreen);

        Assert.Equal("9+", page.Text(HudBadge + ".textContent"));
    }

    [Fact]
    public void Waiting_event_texts_show_on_the_messages_app_inside_the_phone()
    {
        var page = PageScript.On(PhoneUp);

        page.Run("var phone = buildPhonePage({unread: 3});");

        Assert.Equal(PageAnswer.Ok, page.Apply(Defaults, Badge(4, true), "phone.document"));
        Assert.Equal(Clock + "4", page.Text(AppBadge + ".textContent"));
        Assert.True(page.Is(AppBadge + ".classList.contains('is-timed')"));
        Assert.Equal("3", page.Text("phone.smsApp.querySelector('.unread-dot').textContent"));
        Assert.True(page.Is("phone.document.getElementById('sl-eventbadge-style')"));
        Assert.Equal(1.0, page.Number("phone.document.querySelectorAll('.sl-eventbadge').length"));
    }

    [Fact]
    public void The_messages_app_badge_goes_away_when_no_event_text_waits()
    {
        var page = PageScript.On(PhoneUp);

        page.Run("var phone = buildPhonePage({});");
        page.Apply(Defaults, Badge(4, false), "phone.document");
        page.Apply(Defaults, Badge(0, false), "phone.document");

        Assert.False(page.Is(AppBadge));
    }

    /// <summary>The game hides its phone button at times; with no button wanted there is nothing to report.</summary>
    [Fact]
    public void A_phone_the_game_is_not_showing_gets_no_badge_and_no_complaint()
    {
        var page = PageScript.On(PhoneDown);

        page.Run("var phone = buildPhonePage({homeHidden: true});");

        Assert.Equal(PageAnswer.Ok, page.Apply(Defaults, Badge(2, true), "phone.document"));
        Assert.Equal(0.0, page.Number("document.querySelectorAll('.sl-eventbadge').length"));
    }

    [Fact]
    public void A_phone_button_the_game_says_is_up_but_cannot_be_found_is_reported()
    {
        var page = PageScript.On(PhoneButtonGone);

        Assert.Equal(
            PageAnswer.OkWithNotePrefix + "no phone button on the HUD, so the event badge is not shown there",
            page.Apply(Defaults, Badge(2, false), NoPhoneScreen));
    }

    [Fact]
    public void A_missing_button_is_not_reported_while_nothing_waits()
    {
        var page = PageScript.On(PhoneButtonGone);

        Assert.Equal(PageAnswer.Ok, page.Apply(Defaults, Badge(0, false), NoPhoneScreen));
    }

    [Fact]
    public void A_home_screen_without_the_messages_app_is_reported()
    {
        var page = PageScript.On(PhoneUp);

        page.Run("var phone = buildPhonePage({smsAppGone: true});");

        Assert.Equal(
            PageAnswer.OkWithNotePrefix + "no messages app on the phone screen, so the event badge is not shown there",
            page.Apply(Defaults, Badge(2, false), "phone.document"));
    }

    /// <summary>The badge does not depend on the list: a game update that breaks the list must not take the badge with it.</summary>
    [Fact]
    public void The_badge_still_shows_on_a_page_whose_list_the_script_does_not_fit()
    {
        var page = PageScript.On(PhoneUp);

        page.Run("page.app._vnode.component.setupState = {};");

        Assert.StartsWith(PageAnswer.UnsupportedPrefix, page.Apply(Defaults, Badge(2, false), NoPhoneScreen));
        Assert.Equal("2", page.Text(HudBadge + ".textContent"));
    }

    [Fact]
    public void A_missing_badge_spot_is_still_reported_on_a_page_whose_list_the_script_does_not_fit()
    {
        var page = PageScript.On(PhoneButtonGone);

        page.Run("window.Vue = undefined;");

        Assert.Equal(
            PageAnswer.UnsupportedPrefix + "no Vue; no phone button on the HUD, so the event badge is not shown there",
            page.Apply(Defaults, Badge(2, false), NoPhoneScreen));
    }

    /// <summary>
    /// The plugin's own check call, run from Root.html as the game's web view runs it: it must step past a frame it may
    /// not read, find CoreUI1, and hand the script the phone frame's document, so the messages app gets its badge.
    /// </summary>
    [Fact]
    public void The_check_call_from_Root_hands_the_phone_frames_document_to_the_script()
    {
        var page = PageScript.On(PhoneUp);
        var badge = PhoneBadge.Of(new[] { new EventText(PhoneBadge.WaitingState, 0) });
        var check = PageCalls.Check(ListSettings.From(0, true, true, true), badge);

        page.Run(RootWithThreeFrames);

        Assert.Equal(PageAnswer.Ok, page.Text("(function (document) { return " + check + "; })(root)"));
        Assert.True(page.Is(AppBadge), "the messages app in the phone frame got no event badge");
        Assert.Equal("1", page.Text(AppBadge + ".textContent"));
    }

    /// <summary>A plugin that sends no badge, or one this script cannot read, draws nothing rather than a wrong count.</summary>
    [Theory]
    [InlineData("undefined")]
    [InlineData("null")]
    [InlineData("{waiting: 'many', timed: true}")]
    [InlineData("{waiting: -2, timed: true}")]
    [InlineData("{waiting: 0.5, timed: true}")]
    public void A_badge_the_script_cannot_read_draws_nothing(string badge)
    {
        var page = PageScript.On(PhoneUp);

        Assert.Equal(PageAnswer.Ok, page.Apply(Defaults, badge, NoPhoneScreen));
        Assert.False(page.Is(HudBadge));
    }

    [Fact]
    public void A_list_call_without_a_badge_still_answers_ok()
    {
        var page = PageScript.On(PhoneUp);

        Assert.Equal(PageAnswer.Ok, page.Apply(Defaults));
        Assert.False(page.Is(HudBadge));
    }

    private static string Badge(int waiting, bool hasTimeLimit) =>
        $"{{waiting: {waiting}, timed: {(hasTimeLimit ? "true" : "false")}}}";
}
