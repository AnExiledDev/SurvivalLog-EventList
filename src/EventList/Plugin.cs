using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using EventList.Adapters;
using EventList.Core;
using ExiledToolkit;
using ExiledToolkit.Kit;

namespace EventList;

[BepInPlugin(Guid, "Event List", Version)]
[BepInDependency(ToolkitGuid, ">=0.20.0")]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.eventlist";
    private const string Version = "0.5.0";
    private const string ToolkitGuid = "com.dev.survivallog.exiledtoolkit";

    public override void Load()
    {
        var isEnabled = Config.Bind("EventList", "Enabled", true, new ConfigDescription(
            "Keep the events list on the left of the screen at a fixed height and scroll the rest. Off: the mod does nothing, so the list stays as the game draws it and the phone gets no event badge. Takes effect at the next game start.\n" +
            "把屏幕左侧的事件列表固定在一定高度，其余内容滚动查看。关闭后本模组不做任何改动：列表保持游戏原样，手机上也没有事件角标。下次启动游戏时生效。", null, "ModManager.Restart")).Value;
        var maxHeight = Config.Bind("EventList", "MaxHeight", ListSettings.DefaultMaxHeight,
            new ConfigDescription($"Tallest the events list may grow, in HUD pixels, before the rest scrolls, in either layout. From {ListSettings.LowestMaxHeight} to {ListSettings.HighestMaxHeight}; 0 or less uses the default, {ListSettings.DefaultMaxHeight}, which fits both group headers and four rows.\n" +
            $"事件列表最多能长到多高（界面像素），超出部分滚动查看。范围 {ListSettings.LowestMaxHeight} 到 {ListSettings.HighestMaxHeight}；填 0 或更小则使用默认值 {ListSettings.DefaultMaxHeight}，正好放下「主线」「事件」两个分组标题和四行。两种样式都适用。", null, "ModManager.ShowWhen=Enabled"));
        var layout = Config.Bind("EventList", "Layout", ListSettings.GroupsName, new ConfigDescription(
            "How the events list looks. Groups: the game's list with its two groups, sorted by the settings below. " +
            "Lanes: events split into Now, Countdowns and In progress, the most urgent first. Now holds emergencies and countdowns with less than 6 hours left, and shows when each runs out and the items it still needs. " +
            "Countdowns go soonest first, each with a bar that shrinks as time runs out. In progress starts folded; click its header to open it. " +
            "While the game shortens its list (a rescue, a zombie siege or an emergency), only Now stays open and the rest folds into one line. A change shows at once. Default: Groups.\n" +
            "事件列表的样式。「分组」：游戏原本的两个分组列表，按下面的设置排序。" +
            "「分栏」：按紧急程度把事件分成「紧急」「倒计时」「进行中」三栏。「紧急」显示危机和 6 小时内结束的倒计时，并写出结束时间和还缺的物品。" +
            "「倒计时」按剩余时间排序，每行有一条随时间缩短的进度条。「进行中」默认折叠，点击标题展开。" +
            "游戏缩短列表时（救援、尸潮或危机期间），只有「紧急」保持展开，其余折叠成一行。改动会立即生效。默认「分组」。",
            new AcceptableValueList<string>(ListSettings.LayoutNames), "ModManager.ShowWhen=Enabled", "ModManager.ChoiceZh=Groups=分组", "ModManager.ChoiceZh=Lanes=分栏"));
        var isImportantOnTop = Config.Bind("EventList", "ImportantOnTop", true,
            new ConfigDescription("Move important events to the top of the list: emergencies and the zombie wave first, then events with a running countdown. Off: the list keeps the game's order.\n" +
            "把重要事件移到列表顶部：危机和尸潮排在最前，其次是正在倒计时的事件。关闭后列表保持游戏原有顺序。", null, "ModManager.ShowWhen=Enabled;Layout=Groups"));
        var isCountdownOnTop = Config.Bind("EventList", "CountdownOnTop", true,
            new ConfigDescription("Count events with a running countdown as important, so they move up just below emergencies; off, only emergencies move up. Works only while ImportantOnTop is on.\n" +
            "把正在倒计时的事件也算作重要事件，排在危机下方；关闭后只有危机会移到顶部。仅在 ImportantOnTop 开启时生效。", null, "ModManager.ShowWhen=Enabled;Layout=Groups"));
        var isCountdownSoonestFirst = Config.Bind("EventList", "CountdownSoonestFirst", true,
            new ConfigDescription("Sort the countdowns at the top by time left, the one about to run out first; off, they keep the game's order. Works only while ImportantOnTop and CountdownOnTop are on.\n" +
            "顶部的倒计时事件按剩余时间排序，最快结束的排在最前；关闭后保持游戏原有顺序。仅在 ImportantOnTop 和 CountdownOnTop 都开启时生效。", null, "ModManager.ShowWhen=Enabled;Layout=Groups;ImportantOnTop;CountdownOnTop"));
        var isPhoneBadgeOn = Config.Bind("EventList", "PhoneEventBadge", true,
            new ConfigDescription("Show a second badge on the phone button and on the messages app, counting event texts you have not accepted yet; it turns orange with an hourglass when one of them has a countdown. Works only while Enabled is on.\n" +
            "在手机按钮和手机里的「短信」上多显示一个角标，统计还没接取的事件短信；其中有带倒计时的事件时，角标变成橙色并显示沙漏。仅在 Enabled 开启时生效。", null, "ModManager.ShowWhen=Enabled"));
        var log = new ModLog(Log, "event-list.log");

        if (!isEnabled)
        {
            log.Write($"===== Event List {Version} started: off, EventList/Enabled is false; the list is left as the game draws it");

            return;
        }

        var phoneTexts = new PhoneTexts(log.Write);

        // The page reads the game's language on each tick, before it sends anything, so the settings start in English.
        ListSettings ReadSettings() => ListSettings.From(maxHeight.Value, isImportantOnTop.Value, isCountdownOnTop.Value, isCountdownSoonestFirst.Value, ListSettings.LayoutOf(layout.Value), false);
        Func<PhoneBadge> ReadBadgeSource() => BadgeSource(isPhoneBadgeOn.Value, phoneTexts);

        var page = new ListPage(ReadSettings(), ReadBadgeSource(), log.Write);

        log.Write($"===== Event List {Version} started");
        LiveSettings.Watch(() =>
        {
            var settings = ReadSettings();

            page.Use(settings, ReadBadgeSource());
            log.Write(settings.Describe() + ", " + (isPhoneBadgeOn.Value ? "event badge on the phone" : "no event badge"));
        }, maxHeight, layout, isImportantOnTop, isCountdownOnTop, isCountdownSoonestFirst, isPhoneBadgeOn);

        ListTicker.Log = log.Write;
        ListTicker.Page = page;

        AddComponent<ListTicker>();
    }

    /// <summary>With the badge off the phone's texts are never read, and the page is told nothing waits.</summary>
    private static Func<PhoneBadge> BadgeSource(bool isPhoneBadgeOn, PhoneTexts phoneTexts)
    {
        if (!isPhoneBadgeOn) return () => PhoneBadge.None;

        return phoneTexts.Read;
    }
}
