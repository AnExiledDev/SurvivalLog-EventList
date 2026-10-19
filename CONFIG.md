# Event List settings

Settings live in `BepInEx\config\com.dev.survivallog.eventlist.cfg`, created the first time the game starts with the mod. Change them in game on the Mod Manager's Mods screen, or edit the file while the game is closed. Changed on the Mods screen, every setting takes effect at once except `Enabled`, which takes effect at the next game start. Delete the file to get the defaults back.

## EventList

| Setting | Default | What it does |
| --- | --- | --- |
| `Enabled` | `true` | Keeps the events list on the left of the screen at a fixed height and scrolls the rest. `false` turns the mod off: the list stays as the game draws it, and the phone gets no event badge. Takes effect at the next game start. |
| `MaxHeight` | `136` | Tallest the list may grow, in HUD pixels, before the rest scrolls. From 60 to 600. |
| `ImportantOnTop` | `true` | Moves important events to the top of the list: emergencies and the zombie wave first, then events with a running countdown. `false` keeps the game's order; the list still has its fixed height. |
| `CountdownOnTop` | `true` | Counts events with a running countdown as important, so they move up just below emergencies. `false`: only emergencies move up. Works only while `ImportantOnTop` is `true`. |
| `CountdownSoonestFirst` | `true` | Sorts the countdowns at the top by time left, the one about to run out first. `false`: they keep the game's order. Works only while `ImportantOnTop` and `CountdownOnTop` are both `true`. |
| `PhoneEventBadge` | `true` | Shows a second badge on the phone button and on the messages app, counting the event texts you have not accepted yet. It turns orange with an hourglass when one of them has a countdown. The game's own red badge stays as it is. `false`: no event badge. Works only while `Enabled` is `true`. |

`MaxHeight` is held between 60 and 600. A value below 60 is raised to 60 and a value above 600 is lowered to 600. `0` or a negative number means the default, 136, which is room for both group headers and four rows. For comparison, the game's own scroll box, when it uses one, stops at 220.

`CountdownSoonestFirst` compares time left in whole seconds, so countdowns in the same second keep the game's order. A countdown whose time left cannot be read goes after the other countdowns, in the game's order. It can also move a whole group: when the normal group's soonest countdown runs out before the main group's, the normal group sits above the main group.

`PhoneEventBadge` counts the same conversations as the game's own text badge: none while the phone is locked, and none from a contact who died or went missing. An event text counts from the moment it arrives until you accept it or it runs out. The badge is checked every two seconds, so a new text can take that long to show.
