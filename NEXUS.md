Name: Event List
Summary: Keeps the events list on the HUD to a fixed height that scrolls, so it never runs down your screen or hides events behind "...N more". Emergencies and the soonest countdowns move to the top, and a badge on the phone counts event texts you have not accepted yet, turning orange when one has a countdown.

Description (BBCode):
Tired of the events list running down your screen, or hiding events behind "...N more"? Event List keeps the list to a fixed height and lets the rest scroll. Emergencies and running countdowns move to the top so you never miss them. And a second badge on the phone counts the event texts still waiting for you to accept them, so those are not missed either.

[size=5]Main features[/size]
[list]
[*][b]Fixed height[/b]: the list stops at a set height and scrolls with the mouse wheel.[/*]
[*][b]Nothing hidden[/b]: the "...N more" cut never shows; every event stays in the list.[/*]
[*][b]Emergencies on top[/b]: emergencies and the zombie wave move to the top.[/*]
[*][b]Countdowns next[/b]: events with a running countdown come right after, the one about to run out first.[/*]
[*][b]Back to the top[/b]: when an event becomes important the list scrolls up to show it, but never while your pointer is on the list.[/*]
[*][b]Clicks work as before[/b]: clicks on the list and around it land where they did.[/*]
[*][b]Event badge on the phone[/b]: a second badge on the phone button, and on the messages app inside the phone, counts the event texts waiting for you to accept them. It turns orange with an hourglass when one of them has a countdown, and goes away once nothing is waiting. The game's own red badge stays as it is.[/*]
[/list]
Event List only changes what is drawn on screen. It reads the texts in your phone to count the waiting event texts, and never changes the game's data, adds nothing to your save and does not edit game files. You can remove it at any time.

Works in any game language.

[size=5]Known issues[/size]
The event badge on the phone is checked every two seconds, so a new event text can take that long to show on it, and like the game's own badge it counts nothing while the phone is locked. Turn off PhoneEventBadge in the config if you do not want it. With the soonest-first order of countdowns on, a whole group can move: the normal events can sit above the main events when their countdown runs out sooner. Turn off CountdownSoonestFirst in the config if you prefer the game's order. The rescue and siege boxes are separate panels and stay where the game puts them.

[size=5]Requirements[/size]
[list]
[*]Survival Log 1.1.19131. A game update can break it until the mod is updated.[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]. Install it first.[/*]
[*]Optional: [url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.4.0 or newer. With it, settings changed on the Mod Manager's Mods screen take effect without restarting the game. Without it, they take effect at the next game start.[/*]
[/list]

[size=5]Installation[/size]
[list=1]
[*]Install [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i](if no other mods were installed before, start the game once so BepInEx finishes setup, then quit)[/i].[/*]
[*]Optional, to change settings without a restart: install Exiled's Toolkit the same way. Download its zip and extract it into the game folder.[/*]
[*]Download the zip from the Files tab.[/*]
[*]Extract it into the game folder (the folder with [font=Courier New]SurvivalLog.exe[/font]). The zip holds a BepInEx folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\EventList.dll[/code][/*]
[/list]

[size=5]Uninstall[/size]
Delete [font=Courier New]EventList.dll[/font] from the [font=Courier New]BepInEx\plugins[/font] folder.

[size=5]Configuration[/size]
To change settings, edit [font=Courier New]BepInEx\config\com.dev.survivallog.eventlist.cfg[/font] while the game is closed, or do it in game on the Mod Manager's Mods screen, where changes take effect at once, all but [font=Courier New]Enabled[/font], which takes effect at the next game start (see [url=https://github.com/AnExiledDev/SurvivalLog-EventList/blob/main/CONFIG.md]CONFIG.md[/url] for every setting; the cfg file explains each setting in English and Chinese). Without Exiled's Toolkit installed, changes made on the Mods screen take effect at the next game start instead.

[size=5]Feedback[/size]
Found a bug or have an idea? Come say hi on the [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url], or post in this page's Posts tab. For bugs, please include [font=Courier New]BepInEx\LogOutput.log[/font] and [font=Courier New]BepInEx\event-list.log[/font].
To reach me directly, message [b]exiled.dev[/b] on Discord; you can find me in the [url=https://discord.gg/MZGg4gQAUe]official Survival Log Discord[/url]. I'm also on X as [url=https://x.com/AnExiledDev]@AnExiledDev[/url].
Coming soon: a Survival Log wiki with detailed documentation for all of my mods.

[size=5]Source and license[/size]
Open source under MIT: [url=https://github.com/AnExiledDev/SurvivalLog-EventList]github.com/AnExiledDev/SurvivalLog-EventList[/url]

[line]
[size=5]简体中文[/size]

事件列表在屏幕上越拉越长，或者把事件藏在省略提示后面？Event List 把列表固定在一定高度，其余内容滚动查看。危机和正在进行的倒计时会移到顶部，让你不会错过。手机上还会多一个角标，统计还在等你接取的事件短信，这些也不会被错过。

[size=5]主要功能[/size]
[list]
[*][b]固定高度[/b]：列表到一定高度就停止增长，用鼠标滚轮滚动查看。[/*]
[*][b]不再隐藏[/b]：不会再出现省略多出事件的提示，每个事件都留在列表里。[/*]
[*][b]危机置顶[/b]：危机和尸潮移到最上方。[/*]
[*][b]倒计时其次[/b]：正在倒计时的事件紧随其后，最快结束的排在最前。[/*]
[*][b]回到顶部[/b]：事件变得重要时，列表会向上滚动显示它，但鼠标停在列表上时不会。[/*]
[*][b]点击照常[/b]：在列表上和周围的点击，效果和原来一样。[/*]
[*][b]手机上的事件角标[/b]：手机按钮和手机里的「短信」上会多一个角标，统计等你接取的事件短信。其中有带倒计时的事件时，角标变成橙色并显示沙漏；没有待接取的短信时，角标消失。游戏自己的红色角标保持不变。[/*]
[/list]
Event List 只改变屏幕上的显示。它读取手机里的短信来统计待接取的事件短信，从不改动游戏数据，不会往存档里写入任何内容，也不会修改游戏文件。你可以随时移除它。

适用于任何游戏语言。

[size=5]已知问题[/size]
手机上的事件角标每两秒检查一次，所以新的事件短信可能要等这么久才会显示在角标上；和游戏自己的角标一样，手机锁定时它不计数。如果不想要它，请在配置中关闭 PhoneEventBadge。开启倒计时按最快结束排序时，整个分组可能会移动：当普通事件的倒计时先结束时，「事件」分组可能排到「主线」分组上方。如果你更喜欢游戏原有的顺序，请在配置中关闭 CountdownSoonestFirst。救援和围攻框是单独的面板，保持在游戏放置的位置。

[size=5]前置要求[/size]
[list]
[*]Survival Log 1.1.19131。游戏更新后模组可能暂时失效，直到模组更新为止。[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]，请先安装。[/*]
[*]可选：[url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.4.0 或更新版本。装了它，在 Mod Manager 的「模组」界面改动的设置无需重启游戏即可生效；没装的话，改动在下次启动游戏时生效。[/*]
[/list]

[size=5]安装[/size]
[list=1]
[*]安装 [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i]（如果之前没有装过其他模组，先启动一次游戏，让 BepInEx 完成初始化，然后退出）[/i]。[/*]
[*]可选（改动设置无需重启游戏）：用同样的方式安装 Exiled's Toolkit，下载它的压缩包并解压到游戏目录。[/*]
[*]在「文件」页下载压缩包。[/*]
[*]把压缩包解压到游戏目录（[font=Courier New]SurvivalLog.exe[/font] 所在的文件夹）。压缩包里带有 BepInEx 文件夹，DLL 会自动放到正确位置。如果提示合并文件夹，选择合并。解压后应为：
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\EventList.dll[/code][/*]
[/list]

[size=5]卸载[/size]
从 [font=Courier New]BepInEx\plugins[/font] 文件夹中删除 [font=Courier New]EventList.dll[/font]。

[size=5]配置[/size]
如需修改高度、排序或关闭角标，可以在游戏内 Mod Manager 的「模组」界面修改（改动立即生效，[font=Courier New]Enabled[/font] 在下次启动游戏时生效），也可以在游戏关闭时编辑 [font=Courier New]BepInEx\config\com.dev.survivallog.eventlist.cfg[/font]。配置文件中每项设置都有中文说明，完整说明见 [url=https://github.com/AnExiledDev/SurvivalLog-EventList/blob/main/CONFIG.md]CONFIG.md[/url]（英文）。没装 Exiled's Toolkit 的话，在「模组」界面的改动改为在下次启动游戏时生效。

[size=5]反馈[/size]
发现 bug 或有好点子？欢迎来 [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url] 打个招呼，或在本页的「Posts」标签里发帖。报告 bug 时，请附上 [font=Courier New]BepInEx\LogOutput.log[/font] 和 [font=Courier New]BepInEx\event-list.log[/font]。
想直接联系我，可以在 Discord 上给 [b]exiled.dev[/b] 发消息，我在 [url=https://discord.gg/MZGg4gQAUe]Survival Log 官方 Discord[/url] 里。也可以在 X 上找我：[url=https://x.com/AnExiledDev]@AnExiledDev[/url]。
即将推出：Survival Log 维基，收录我所有模组的详细说明。

[size=5]源代码与许可[/size]
基于 MIT 许可开源：[url=https://github.com/AnExiledDev/SurvivalLog-EventList]github.com/AnExiledDev/SurvivalLog-EventList[/url]
