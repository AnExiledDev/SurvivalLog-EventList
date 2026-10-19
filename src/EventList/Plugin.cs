using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using EventList.Adapters;
using EventList.Core;
using ExiledToolkit;

namespace EventList;

[BepInPlugin(Guid, "Event List", Version)]
[BepInDependency(ExiledToolkit.Plugin.Guid, ">=0.4.0")]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.eventlist";
    private const string Version = "0.4.0";

    public override void Load()
    {
        var isEnabled = Config.Bind("EventList", "Enabled", true, new ConfigDescription(
            "Keep the events list on the left of the screen at a fixed height and scroll the rest. Off: the mod does nothing, so the list stays as the game draws it and the phone gets no event badge. Takes effect at the next game start.\n" +
            "把屏幕左侧的事件列表固定在一定高度，其余内容滚动查看。关闭后本模组不做任何改动：列表保持游戏原样，手机上也没有事件角标。下次启动游戏时生效。", null, "ModManager.Restart")).Value;
        var maxHeight = Config.Bind("EventList", "MaxHeight", ListSettings.DefaultMaxHeight,
            $"Tallest the events list may grow, in HUD pixels, before the rest scrolls. From {ListSettings.LowestMaxHeight} to {ListSettings.HighestMaxHeight}; 0 or less uses the default, {ListSettings.DefaultMaxHeight}, which fits both group headers and four rows.\n" +
            $"事件列表最多能长到多高（界面像素），超出部分滚动查看。范围 {ListSettings.LowestMaxHeight} 到 {ListSettings.HighestMaxHeight}；填 0 或更小则使用默认值 {ListSettings.DefaultMaxHeight}，正好放下「主线」「事件」两个分组标题和四行。");
        var isImportantOnTop = Config.Bind("EventList", "ImportantOnTop", true,
            "Move important events to the top of the list: emergencies and the zombie wave first, then events with a running countdown. Off: the list keeps the game's order.\n" +
            "把重要事件移到列表顶部：危机和尸潮排在最前，其次是正在倒计时的事件。关闭后列表保持游戏原有顺序。");
        var isCountdownOnTop = Config.Bind("EventList", "CountdownOnTop", true,
            "Count events with a running countdown as important, so they move up just below emergencies; off, only emergencies move up. Works only while ImportantOnTop is on.\n" +
            "把正在倒计时的事件也算作重要事件，排在危机下方；关闭后只有危机会移到顶部。仅在 ImportantOnTop 开启时生效。");
        var isCountdownSoonestFirst = Config.Bind("EventList", "CountdownSoonestFirst", true,
            "Sort the countdowns at the top by time left, the one about to run out first; off, they keep the game's order. Works only while ImportantOnTop and CountdownOnTop are on.\n" +
            "顶部的倒计时事件按剩余时间排序，最快结束的排在最前；关闭后保持游戏原有顺序。仅在 ImportantOnTop 和 CountdownOnTop 都开启时生效。");
        var isPhoneBadgeOn = Config.Bind("EventList", "PhoneEventBadge", true,
            "Show a second badge on the phone button and on the messages app, counting event texts you have not accepted yet; it turns orange with an hourglass when one of them has a countdown. Works only while Enabled is on.\n" +
            "在手机按钮和手机里的「短信」上多显示一个角标，统计还没接取的事件短信；其中有带倒计时的事件时，角标变成橙色并显示沙漏。仅在 Enabled 开启时生效。");
        var log = new ListLog(Log, Path.Combine(Paths.BepInExRootPath, "event-list.log"));

        if (!isEnabled)
        {
            log.Write($"===== Event List {Version} started: off, EventList/Enabled is false; the list is left as the game draws it");

            return;
        }

        var phoneTexts = new PhoneTexts(log.Write);

        ListSettings ReadSettings() => ListSettings.From(maxHeight.Value, isImportantOnTop.Value, isCountdownOnTop.Value, isCountdownSoonestFirst.Value);
        Func<PhoneBadge> ReadBadgeSource() => BadgeSource(isPhoneBadgeOn.Value, phoneTexts);

        var page = new ListPage(ReadSettings(), ReadBadgeSource(), log.Write);

        log.Write($"===== Event List {Version} started");
        LiveSettings.Watch(() =>
        {
            var settings = ReadSettings();

            page.Use(settings, ReadBadgeSource());
            log.Write(settings.Describe() + ", " + (isPhoneBadgeOn.Value ? "event badge on the phone" : "no event badge"));
        }, maxHeight, isImportantOnTop, isCountdownOnTop, isCountdownSoonestFirst, isPhoneBadgeOn);

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
