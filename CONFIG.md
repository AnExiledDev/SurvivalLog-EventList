# Event List settings

Settings live in `BepInEx\config\com.dev.survivallog.eventlist.cfg`, created the first time the game starts with the mod. Change them in game on the Mod Manager's Mods screen, or edit the file while the game is closed. Every setting you change on the Mods screen takes effect at once, except `Enabled`, which takes effect at the next game start. Delete the file to get the defaults back.

## EventList

| Setting | Default | What it does |
| --- | --- | --- |
| `Enabled` | `true` | Keeps the events list on the left of the screen at a fixed height and lets the rest scroll. With `false` the mod does nothing at all: the list stays as the game draws it, and the phone gets no event badge. Takes effect at the next game start. |
| `MaxHeight` | `136` | How tall the list can get, in HUD pixels, before the rest scrolls. It works the same in both layouts. From 60 to 600. |
| `Layout` | `Groups` | How the list looks. `Groups` is the game's list with its two groups, sorted by the three settings below. `Lanes` splits the events into Now, Countdowns and In progress, with the most urgent first (see below). |
| `ImportantOnTop` | `true` | Moves important events to the top of the list: emergencies and the zombie wave first, then events with a running countdown. With `false` the list keeps the game's order, but it still has its fixed height. |
| `CountdownOnTop` | `true` | Counts events with a running countdown as important, so they move up just below emergencies. With `false` only emergencies move up. Works only while `ImportantOnTop` is `true`. |
| `CountdownSoonestFirst` | `true` | Sorts the countdowns at the top by time left, so the one about to run out comes first. With `false` they keep the game's order. Works only while `ImportantOnTop` and `CountdownOnTop` are both `true`. |
| `PhoneEventBadge` | `true` | Shows a second badge on the phone button and on the messages app that counts the event texts you haven't accepted yet. It turns orange with an hourglass when one of them has a countdown. The game's red badge stays as it is. With `false` there's no event badge. Works only while `Enabled` is `true`. |

`MaxHeight` stays between 60 and 600. A value below 60 is raised to 60 and a value above 600 is lowered to 600. `0` or a negative number means the default, 136, which is room for both group headers and four rows. For comparison, the game's scroll box, when it uses one, stops at 220.

`ImportantOnTop`, `CountdownOnTop` and `CountdownSoonestFirst` only change the `Groups` layout. The Mods screen hides them while `Layout` is `Lanes`.

With `Layout` set to `Lanes`:

- **Now** holds emergencies, the zombie wave while it is spawning or charging, and countdowns with less than 6 hours left. Emergencies come first, then the countdowns, soonest first. Each event here says when it runs out on the game clock, and an event that asks for items lists them with their icons and how many you have.
- **Countdowns** holds the other countdowns, soonest first, each with a bar that shrinks as time runs out.
- **In progress** holds everything else, which is story events and other events with no countdown, plus the tracker rows. It starts folded and shows how many events it holds and how many of them are story events. Click its header to open or fold it.
- During a rescue, a zombie siege or an emergency, the game shortens its list. Then only Now stays open, and the other lanes fold into one line called Everything else, which shows when the next countdown runs out.
- Clicking a row does what clicking it in the game's list does: an event opens its details, and a tracker row opens the phone.

`CountdownSoonestFirst` compares time left in whole seconds, so countdowns in the same second keep the game's order. A countdown whose time left can't be read goes after the other countdowns, in the game's order. It can also move a whole group: when the normal group's soonest countdown runs out before the main group's, the normal group sits above the main group.

`PhoneEventBadge` counts the same conversations as the game's text badge. It counts nothing while the phone is locked, and nothing from a contact who died or went missing. An event text counts from the moment it arrives until you accept it or it runs out. The badge is checked every two seconds, so a new text can take that long to show.
