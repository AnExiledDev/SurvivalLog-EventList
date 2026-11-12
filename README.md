# Event List

Keeps the events list on the HUD to a fixed height, scrollable, with the important events on top, and puts a badge on the phone for event texts you have not accepted yet. A second layout, lanes, splits the list into Now, Countdowns and In progress.

In Survival Log the events list on the left of the screen grows down the screen as events pile up, and during a rescue, siege or emergency the game cuts it to four rows and a "...N more" line. Event List holds the list to a fixed height, lets the rest scroll, and moves emergencies and running countdowns to the top so they are never hidden.

## Features

- **Fixed height**: the list stops at a set height (136 HUD pixels by default, room for both group headers and four rows). The rest scrolls with the mouse wheel over the list, using the game's thin scrollbar.
- **Every event stays in the list**: the game's "...N more" cut never shows. Every event sits in the scroll box, so an important one is never hidden.
- **Emergencies on top**: an event the game shows as one of its red emergency entries, or the zombie wave while it is spawning or charging, moves to the top of the list.
- **Countdowns next**: an event with a running countdown moves up too, below the emergencies.
- **Soonest countdown first**: among the countdowns, the one with the least time left comes first.
- **Back to the top when it matters**: when an event becomes important the list scrolls back to the top so you see it. If the pointer is over the list at that moment, the scroll waits until the pointer leaves, so a list you are reading never jumps.
- **Clicks land where they did**: a list that fits keeps the game's click areas on each row. A list that overflows takes clicks and the wheel as one box, so a row scrolled out of sight never catches a click meant for the world. Clicks beside or below the list reach the game as before.
- **Lanes**: set `Layout` to `Lanes` and the list is sorted into three lanes. Now holds emergencies and countdowns with less than 6 hours left, each showing when it runs out and which items it still needs. Countdowns holds the rest of the timers, soonest first, each with a bar that shrinks as time runs out. In progress holds everything else and starts folded. During a rescue, a zombie siege or an emergency, only Now stays open. See [Lanes](#lanes).
- **Event badge on the phone**: a second badge on the phone button, and on the messages app inside the phone, counts the event texts still waiting for you to accept them. It is teal, on the other corner from the game's red badge for unread event and trade texts, which stays as it is. When one of the waiting texts has a countdown, it turns orange with an hourglass. It goes away once nothing is waiting.
- **Survives reloads**: the HUD can reload after a scene change; the mod notices within a couple of seconds and sets the list up again.
- **Steps aside after a game update**: if the HUD changes so the mod no longer recognises it, the list is left exactly as the game draws it and the log says why in one line.
- **Log file**: the mod writes what it did at startup and what the HUD answered to `BepInEx\event-list.log`.

Event List only changes what is drawn on screen. It reads the texts in your phone to count the waiting event texts, and never writes the game's state, adds nothing to your save and does not edit game files. The game's list and its data are never reordered; the order changes on screen only. You can remove it at any time; the list goes back to the game's on the next start.

Importance comes from the game's data for each event, never from its text, so it works in any game language.

## How the list is ordered

Highest first:

1. **Emergency.** The event is one of the game's emergency entries while those are showing, or it is the zombie wave's event while the wave is spawning or charging.
2. **Countdown.** The event shows a running countdown with time left. With `CountdownSoonestFirst` on (the default), the countdown with the least time left comes first. Times are compared in whole seconds, so countdowns in the same second keep the game's order. A countdown whose time left cannot be read goes after the rest of its tier, in the game's order.
3. **Everything else**, in the game's order.

Emergencies keep the game's order among themselves. Extra tracker rows under an event are never moved.

The game draws the list in groups (main and normal events), each with a header. An important event moves to the top of its group, and a group holding an important event moves above the groups that hold none. Two groups whose most important event is a countdown are ordered by their soonest countdown, so with `CountdownSoonestFirst` on, the normal group can sit above the main group when its countdown runs out sooner.

The order is worked out again each time the game sends the list a new set of events, and every two seconds as a backstop. Countdowns tick down at the same rate, so they do not pass each other in between.

## Lanes

With `Layout` set to `Lanes` (the default is `Groups`), the list is drawn as three lanes, each with a header you can click to fold or open it:

1. **Now**: emergencies, the zombie wave while it is spawning or charging, and countdowns with less than 6 hours left. Emergencies come first, in the game's order, then the countdowns, soonest first. Under each countdown a second line says when it runs out on the game clock ("runs out today 21:20") and lists the items the event still needs, with their icons and how many you have.
2. **Countdowns**: every other countdown, soonest first. A bar under each row shrinks as its time runs out.
3. **In progress**: story events, events with no countdown and the tracker rows under them, in the game's order. It starts folded, with a count and how many of them are story events.

While the game shortens its list (a rescue, a zombie siege or an emergency), only Now stays open and everything else folds into one line that says when its next countdown runs out. When that ends, the lanes open again the way you left them.

Clicking a row does what clicking it in the game's list does: an event opens its details, and a tracker row opens the phone. The list keeps its `MaxHeight` and scrolls the same way, and when a new emergency shows up it scrolls back to the top.

The lane headers and times are in Chinese when the game is in Chinese. `ImportantOnTop`, `CountdownOnTop` and `CountdownSoonestFirst` only change the `Groups` layout.

## Known issues and limits

- The event badge is checked every two seconds, so a new event text can take that long to show on it.
- The event badge counts the same conversations as the game's text badge: nothing while the phone is locked, and nothing from a contact who died or went missing.
- The rescue, warehouse rescue and zombie siege boxes are separate panels with no event behind them, so they stay where the game puts them. A list row counts as an emergency only through the game's emergency entries or the zombie wave.
- In lanes, the items an event still needs are read from the game every two seconds, so a change in your bag can take that long to show.
- The game's "...N more" line never shows. To keep every event in the scroll box, the mod holds the list's own hover flag on.

## Requirements

- Survival Log 1.1.20719. Built for this version; a game update can break it until the mod is updated.
- The [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12), the BepInEx 6 (IL2CPP) build for the game.
- [Exiled's Toolkit](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit) 0.20.0 or newer. Event List does not load without it. It applies settings changed on the Mod Manager's Mods screen without restarting the game.
- Optional: [Mod Manager](https://www.nexusmods.com/survivallog/mods/73). With it, you can change every setting in game on its Mods screen and see what each one does, without opening the config file.

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12) (if no other mods were installed before, start the game once so BepInEx finishes setup, then quit).
2. Install Exiled's Toolkit. Download `ExiledToolkit-x.y.z.zip` from [its latest release](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit/releases/latest) or from [Nexus Mods](https://www.nexusmods.com/survivallog/mods/61), and extract it into the game folder (the folder with `SurvivalLog.exe`).
3. Download `EventList-x.y.z.zip` from the [latest release](https://github.com/AnExiledDev/SurvivalLog-EventList/releases/latest) or from [Nexus Mods](https://www.nexusmods.com/survivallog/mods/82).
4. Extract the zip into the game folder (the folder with `SurvivalLog.exe`). It holds a `BepInEx` folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
   `C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\EventList.dll`

## Uninstall

Delete `EventList.dll` from the `BepInEx\plugins` folder. For a clean slate, also delete `BepInEx\config\com.dev.survivallog.eventlist.cfg` and `BepInEx\event-list.log`.

## Configuration

Settings live in `BepInEx\config\com.dev.survivallog.eventlist.cfg`, created the first time the game starts with the mod. Change them in game on the Mod Manager's Mods screen, or edit the file while the game is closed. Changed on the Mods screen, every setting takes effect at once except `Enabled`, which takes effect at the next game start. Each setting in the cfg file is explained in English and Simplified Chinese. See [CONFIG.md](CONFIG.md) for every setting.

## Troubleshooting

Built against BepInEx `6.0.0-be.788`.

Check `BepInEx\LogOutput.log` for `===== Event List 0.5.0 started`. With the default settings the line after it reads `list at most 136px tall, emergencies then countdowns on top, countdowns soonest first, event badge on the phone`, and it is written again when a setting changes on the Mods screen. With `Layout` set to `Lanes` it reads `lanes at most 136px tall, event badge on the phone`. With `PhoneEventBadge` set to `false` it ends `no event badge` instead. With `Enabled` set to `false` the start line ends `off, EventList/Enabled is false; the list is left as the game draws it`.

Once a game has loaded, look for `page script sent to CoreUI1 (install 1): the events list is held to its height`. If instead a line says `CoreUI1 is not the page this mod was written for`, a game update has changed the HUD and the list is left as the game draws it until the mod is updated. A line ending `so the event badge is not shown there` means the mod could not find the phone button or the messages app to put the badge on.

The same lines are in `BepInEx\event-list.log`, with a timestamp each, and that file is kept across restarts.

## Feedback and bug reports

Found a bug, have an idea, or something feels off? Come say so on the Core Directive Discord: https://discord.gg/trTsyxUFqg . You can also post in the Posts tab of the mod's Nexus Mods page. When reporting a bug, attach `BepInEx\LogOutput.log` and `BepInEx\event-list.log`.

To reach me directly, message `exiled.dev` on Discord; you can find me in the [official Survival Log Discord](https://discord.gg/MZGg4gQAUe). I'm also on X as [@AnExiledDev](https://x.com/AnExiledDev).

A Survival Log wiki with detailed documentation for all of my mods is coming soon.

## Changelog

- **0.5.0**: A second layout, lanes, sorts the list into Now, Countdowns and In progress. Turn it on with `Layout`.
- **0.4.0**: Settings can be changed on the Mod Manager's Mods screen without a restart, all but `Enabled`. Works with Exiled's Toolkit 0.4.0 or newer, which is optional.
- **0.3.1**: Plainer setting descriptions in the config file, each now also in Simplified Chinese. No setting or default changed.
- **0.3.0**: A second badge on the phone counts the event texts waiting for you to accept them, so they are not missed. It turns orange with an hourglass when one of them has a countdown. Turn it off with `PhoneEventBadge`.
- **0.2.0**: Countdowns on top are ordered by time left, the one about to run out first.
- **0.1.0**: First version.

## Build

This is a BepInEx 6 IL2CPP plugin. It compiles against BepInEx and the game's IL2CPP interop assemblies, so a game install with BepInEx set up and started once is required. Those assemblies come from the game and are not part of this repo. The .NET 8 SDK is required.

```
dotnet build src/EventList -c Release -p:GameDir="C:\path\to\Survival Log"
```

`GameDir` is the game folder. The default in the project file is a local path you will almost certainly need to override. Compiling also needs Exiled's Toolkit 0.20.0 or newer: point `-p:ToolkitDll="C:\path\to\ExiledToolkit.dll"` at its DLL. The output DLL is at `src\EventList\bin\Release\EventList.dll`.

The unit tests cover the pure logic, including the page script, which they run against a small stand-in for the HUD page. They need nothing from the game:

```
dotnet test tests/EventList.Tests
```

## License

MIT. Copyright (c) 2026 AnExiledDev. See [LICENSE](LICENSE).
