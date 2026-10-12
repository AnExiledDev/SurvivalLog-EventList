# Event List

A BepInEx 6 (IL2CPP) plugin for Survival Log that holds the events list on the left of the HUD to a fixed height. A longer list scrolls inside that height instead of growing down the screen, and important events are kept at the top of it.

## What it does

- **Fixed height.** The list stops at `MaxHeight` (120 page pixels by default, about the game's own four-row list). The rest scrolls with the mouse wheel over the list, using the game's own thin scrollbar and the game's own wheel handling.
- **Every event stays in the list.** While a rescue, siege or emergency panel shows, the game cuts the list to four rows and a "...N more" line. With this mod every event sits in the scroll box, so an important one is never hidden behind "N more".
- **Clicks land where they did.** A list that fits keeps the game's per-row click areas. A list that overflows takes clicks and the wheel as one box, the way the game's own scroll mode does, so a row scrolled out of sight never catches a click meant for the world. Clicks beside or below the list reach the game as before.
- **Important events on top**, defined below. The order changes on screen only (CSS `order`); the game's list and its data are never reordered. When an event becomes important the list scrolls back to the top so you see it.
- **Pure UI.** The plugin runs a script in the HUD page and reads its answer; it never reads or writes the game's state. The script touches the HUD page's DOM, one stylesheet, and the list's own hover flag (see Known limits).
- **Survives reloads.** The plugin asks the HUD page every two seconds whether the script is there and sends it again after a page reload or scene change. If a game update changes the HUD so the script no longer recognises it, the list is left exactly as the game draws it and the log says why in one line.

## What counts as important

Importance comes from the game's own data for each event, never from its text. Two tiers, highest first:

1. **Emergency.** The event's id is one of the game's emergency chips (`eventStates`) while those chips are showing (`eventStatesActive`), or it is the zombie wave's event (`towerWaveEventId`) while the wave is spawning or charging (`towerWaveState` 1 or 2).
2. **Countdown.** The event shows a running countdown (`timeActive` with `remainSeconds` above 0).

Everything else keeps the game's order, and so do events within one tier. The game draws the list in groups with a header each; an important event moves to the top of its group, and a group holding an important event moves above the groups that hold none. Extra tracker rows are never moved.

Each row's text is compared with its event's text only to prove the row on screen is the event the data describes. If they disagree (mid-update), nothing is moved until the next pass.

## Config

`BepInEx/config/com.dev.survivallog.eventlist.cfg`:

| Setting | Default | What it does |
| --- | --- | --- |
| `EventList/Enabled` | `true` | `false` turns the mod off: the list is left exactly as the game draws it. |
| `EventList/MaxHeight` | `120` | Tallest the list grows, in the page's pixels. `0` means the default. Held between 60 and 600; the game's own scroll box stops at 220. |
| `EventList/ImportantOnTop` | `true` | `false` keeps the game's order. The list still has its fixed height. |

Settings are read when the game starts.

Log: `BepInEx/event-list.log`.

## Known limits

- The rescue, warehouse rescue and zombie siege boxes are separate panels with no event id, so they cannot be matched to a row in the list. They stay where the game puts them; a list row counts as an emergency only through the emergency chips or the zombie wave.
- Whether the game also shows its emergency events as rows in this list has not been confirmed in game. If it does not, only the countdown tier moves anything.
- Counting a running countdown as important is a judgment call. It cannot be switched off on its own; `ImportantOnTop = false` turns off both tiers.
- The game's "...N more" line never shows; every event is in the scroll box instead. To do that the script holds the list's hover flag on. As far as the HUD page's code shows, the game uses that flag only for how the list itself is drawn.
- The default height is worked out from the game's CSS, not measured in game. If four rows do not fit exactly, change `MaxHeight`.
- How far one wheel notch scrolls is the game's own step.

## Install

Copy `EventList.dll` into `BepInEx/plugins`. Needs BepInEx 6 (IL2CPP build).

## Build and test

```sh
dotnet build src/EventList -c Release -p:GameDir="D:\path\to\Survival Log"
dotnet test tests/EventList.Tests
```

The build references BepInEx and the game's interop assemblies under `GameDir`, so BepInEx must be installed and the game launched once. The tests compile only the pure `Core` code and need nothing from the game.

## License

MIT, see [LICENSE](LICENSE).
