# Event List settings

Settings live in `BepInEx\config\com.dev.survivallog.eventlist.cfg`, created the first time the game starts with the mod. Edit the file while the game is closed; changes apply at the next game start. Delete the file to get the defaults back.

## EventList

| Setting | Default | What it does |
| --- | --- | --- |
| `Enabled` | `true` | `false` turns the mod off: the list is left exactly as the game draws it, and the phone gets no event badge. |
| `MaxHeight` | `136` | Tallest the list may grow, in the page's pixels. Anything past it scrolls. |
| `ImportantOnTop` | `true` | Moves important events to the top of the list: emergencies first, then running countdowns. `false` keeps the game's order; the list still has its fixed height. |
| `CountdownOnTop` | `true` | Counts an event with a running countdown as important, so it moves up below the emergencies. `false`: only emergencies move. Does nothing while `ImportantOnTop` is `false`. |
| `CountdownSoonestFirst` | `true` | Orders the countdowns on top by time left, the one about to run out first. `false`: countdowns keep the game's order among themselves. Does nothing while `CountdownOnTop` or `ImportantOnTop` is `false`. |
| `PhoneEventBadge` | `true` | Shows a second badge on the phone button on the HUD, and on the messages app inside the phone, counting the event texts still waiting to be accepted. It turns orange with an hourglass when one of them can run out. The game's own red text badge is left as it is. `false`: no event badge. Does nothing while `Enabled` is `false`. |

`MaxHeight` is held between 60 and 600. A value below 60 is raised to 60 and a value above 600 is lowered to 600. `0` or a negative number means the default, 136, which is room for both group headers and four rows. For comparison, the game's own scroll box, when it uses one, stops at 220.

`CountdownSoonestFirst` compares time left in whole seconds, so countdowns in the same second keep the game's order. A countdown whose time left cannot be read goes after the other countdowns, in the game's order. It can also move a whole group: when the normal group's soonest countdown runs out before the main group's, the normal group sits above the main group.

`PhoneEventBadge` counts the same conversations as the game's own text badge: none while the phone is locked, and none from a contact who died or went missing. An event text counts from the moment it arrives until you accept it or it runs out. The badge is checked every two seconds, so a new text can take that long to show.
