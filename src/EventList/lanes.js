// Event List's Lanes layout: our own panel in place of the game's event rows, split into lanes by how urgent each is.
// Now holds emergencies and countdowns under six hours, each with when it runs out and what it still needs.
// Countdowns holds the rest of the timers, soonest first, each with a bar that drains. In progress holds everything
// else, folded to one line. While the game cuts its list short (a rescue, a siege or an emergency), only Now stays
// open and the rest folds into Everything else.
// eventlist.js hooks the page and calls draw; this file only turns the game's list into lanes and keeps our panel in
// step with it. A click on a row sends the game the same message a click on the game's row sends.
(function () {
    'use strict';

    if (window.__sleventlistLanes) return;

    var STYLE_ID = 'sl-eventlist-lanes-style';
    var ROOT_CLASS = 'sl-lanes';
    var CLICKABLE = 'data-interactive';
    var ROW_ID = 'data-sl-id';
    var FOLD = 'data-sl-fold';
    var SVG_NS = 'http://www.w3.org/2000/svg';
    var DIAMOND = '\u25C6';
    var CHEVRON = '\u25B8';
    var DOT = ' \u00B7 ';
    var ICON_ROOT = '../../';

    // Countdowns under this many game minutes go to Now; under a day they read orange, past it blue.
    var NOW_MINUTES = 360;
    var DAY_MINUTES = 1440;

    var LANES = ['now', 'timed', 'progress', 'rest'];
    var FOLDABLE = { progress: true, rest: true };

    var WORDS = {
        en: {
            now: 'Now', timed: 'Countdowns', progress: 'In progress', rest: 'Everything else',
            story: '{0} story', next: 'next {0}', crisis: 'now', runsOut: 'runs out {0}', need: 'need {0}, have {1}',
            today: 'today {0}', tomorrow: 'tomorrow {0}', day: 'day {0} {1}', later: '+{0}d {1}',
            days: 'd', hours: 'h', minutes: 'm', gap: ' '
        },
        zh: {
            now: '紧急', timed: '倒计时', progress: '进行中', rest: '其他',
            story: '主线 {0}', next: '下一个 {0}', crisis: '现在', runsOut: '{0} 到期', need: '需要 {0}，已有 {1}',
            today: '今天 {0}', tomorrow: '明天 {0}', day: '第{0}天 {1}', later: '+{0}天 {1}',
            days: '天', hours: '小时', minutes: '分', gap: ''
        }
    };

    // The colours are the game's: gold story text, grey-blue event text, its row padding, shadow, hover and scrollbar.
    // The lane colours and the drain bar are ours. The panel sits in the game's .event-panel, after the scroll box this
    // layout hides.
    var CSS = [
        'html.sl-eventlist-lanes .event-panel > .event-scroll, html.sl-eventlist-lanes .event-panel > .event-tail { display: none !important; }',
        '.sl-lanes { max-height: var(--sl-eventlist-max, 136px); overflow-y: auto; overflow-x: hidden; overscroll-behavior: contain; display: flex; flex-direction: column; gap: 6px; font-size: 11px; color: #B0BEC5; }',
        '.sl-lanes::-webkit-scrollbar { width: 4px; }',
        '.sl-lanes::-webkit-scrollbar-track { background: rgba(0, 0, 0, 0.28); border-radius: 2px; }',
        '.sl-lanes::-webkit-scrollbar-thumb { background: rgba(255, 255, 255, 0.20); border-radius: 2px; }',
        '.sl-lanes::-webkit-scrollbar-thumb:hover { background: rgba(255, 255, 255, 0.32); }',
        '.sl-ln { flex-shrink: 0; --sl-lane: #9aa7ad; }',
        '.sl-ln.is-empty, .sl-ln.is-folded > .sl-ln-rows { display: none; }',
        '.sl-ln-now { --sl-lane: #ff7a4d; }',
        '.sl-ln-timed { --sl-lane: #ffb74d; }',
        '.sl-ln-head { position: relative; display: flex; align-items: center; gap: 5px; margin: 0 30px 2px 0; padding-bottom: 2px; white-space: nowrap; font-size: 9.5px; font-weight: bold; letter-spacing: 1px; text-transform: uppercase; color: var(--sl-lane); text-shadow: 0.7px 0.7px 1.4px black; }',
        '.sl-ln-head::after { content: ""; position: absolute; left: 0; right: 0; bottom: 0; height: 1px; background: linear-gradient(90deg, var(--sl-lane) 0%, rgba(0, 0, 0, 0) 100%); opacity: 0.45; }',
        '.sl-ln-head[data-sl-fold] { cursor: pointer; }',
        '.sl-ln-dot { flex-shrink: 0; width: 6px; height: 6px; border-radius: 50%; background: var(--sl-lane); box-shadow: 0 0 5px var(--sl-lane); }',
        '.sl-ln-n { font-family: Consolas, monospace; font-size: 10px; font-weight: normal; letter-spacing: 0; opacity: 0.8; }',
        '.sl-ln-chev { display: inline-block; font-size: 9px; transform: rotate(90deg); transition: transform 0.15s; }',
        '.sl-ln.is-folded .sl-ln-chev { transform: none; }',
        '.sl-ln-hint { margin-left: auto; font-size: 9.5px; font-weight: normal; letter-spacing: 0; text-transform: none; color: #8d9aa1; }',
        '.sl-ln-row { position: relative; display: grid; grid-template-columns: 11px minmax(0, 1fr) auto; column-gap: 7px; align-items: center; padding: 2.5px 30px 2.5px 0; cursor: pointer; text-shadow: 0.7px 0.7px 1.4px black; }',
        '.sl-ln-row.is-story { color: #FFC107; }',
        '.sl-ln-row.has-sub { padding-bottom: 5px; }',
        '@media (hover: hover) { .sl-ln-row:hover { background: linear-gradient(90deg, rgba(255, 255, 255, 0.12) 0%, rgba(255, 255, 255, 0.04) 70%, rgba(255, 255, 255, 0) 100%); } }',
        '.sl-ln-mark { width: 11px; height: 11px; fill: currentColor; filter: drop-shadow(0.7px 0.7px 1px black); }',
        '.sl-ln-diamond { font-size: 8px; line-height: 11px; text-align: center; opacity: 0.85; }',
        '.sl-ln-text { min-width: 0; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }',
        '.sl-ln-right { white-space: nowrap; font-family: Consolas, monospace; }',
        '.sl-ln-count { font-size: 10px; color: #CFD8DC; opacity: 0.85; }',
        '.sl-ln-count + .sl-ln-time { margin-left: 6px; }',
        '.sl-ln-time { font-size: 10.5px; font-weight: bold; color: #CFD8DC; }',
        '.sl-ln-time.now { color: #ff7a4d; }',
        '.sl-ln-time.soon { color: #ffb74d; }',
        '.sl-ln-time.later { color: #a9c3d6; }',
        '.sl-ln-sub { grid-column: 2 / 4; font-size: 9.5px; color: #8d9aa1; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }',
        '.sl-ln-need.is-met { color: #9ccc65; }',
        '.sl-ln-item { width: 13px; height: 13px; margin-right: 2px; vertical-align: -3px; filter: drop-shadow(0 0 1px black); }',
        '.sl-ln-drain { position: absolute; left: 18px; right: 30px; bottom: 0; height: 1.5px; background: rgba(255, 255, 255, 0.08); }',
        '.sl-ln-drain::after { content: ""; position: absolute; left: 0; top: 0; bottom: 0; width: calc(var(--sl-left, 1) * 100%); background: var(--sl-lane); }',
        '.sl-ln-now > .sl-ln-rows > .sl-ln-row:first-child .sl-ln-time.now { animation: sl-ln-pulse 1.6s ease-in-out infinite; }',
        'html.quality-low .sl-ln-time.now { animation: none; }',
        '@keyframes sl-ln-pulse { 50% { opacity: 0.55; } }',
        '.sl-ln-row.is-entering { animation: sl-ln-slide-in 0.35s ease-out both; }',
        '@keyframes sl-ln-slide-in { from { opacity: 0; transform: translateX(-14px); } to { opacity: 1; transform: translateX(0); } }',
        '.sl-ln-row.is-fresh { z-index: 0; }',
        '.sl-ln-row.is-fresh::before { content: ""; position: absolute; inset: 0; z-index: -1; pointer-events: none; opacity: 0; background: linear-gradient(90deg, rgba(255, 215, 0, 0.45) 0%, rgba(255, 215, 0, 0.20) 70%, rgba(255, 215, 0, 0) 100%); animation: sl-ln-fresh 1.5s ease-out; }',
        '@keyframes sl-ln-fresh { 0% { opacity: 1; } 60% { opacity: 0.4; } 100% { opacity: 0; } }'
    ].join('\n');

    // The game's countdown text is game hours and minutes, "HH:MM", with hours past 24 for long ones. Anything else is
    // not a countdown this script can read, and null.
    function parseLeft(text) {
        var match = /^(\d+):(\d{2})$/.exec(String(text == null ? '' : text));

        return match ? Number(match[1]) * 60 + Number(match[2]) : null;
    }

    // Game minutes the way a person says them: 45m, 3h 20m, 3h, 2d 6h, 2d.
    function formatLeft(minutes, words) {
        if (minutes < 60) return minutes + words.minutes;

        var days = Math.floor(minutes / DAY_MINUTES);
        var hours = Math.floor((minutes % DAY_MINUTES) / 60);
        var rest = minutes % 60;

        if (days) return hours ? days + words.days + words.gap + hours + words.hours : days + words.days;

        return rest ? hours + words.hours + words.gap + rest + words.minutes : hours + words.hours;
    }

    // When a countdown runs out on the game clock: "today 21:20", "tomorrow 03:41", "day 52 06:00". clock is the
    // game's time of day in minutes, day the HUD's day label ("DAY51"). null when the clock cannot be read.
    function formatEndsAt(minutesLeft, clock, day, words) {
        if (clock === null || !isFinite(clock)) return null;

        var at = clock + minutesLeft;
        var dayOffset = Math.floor(at / DAY_MINUTES);
        var time = pad(Math.floor((at % DAY_MINUTES) / 60)) + ':' + pad(at % 60);
        var dayDigits = /(\d+)/.exec(String(day == null ? '' : day));

        if (dayOffset === 0) return fill(words.today, time);
        if (dayOffset === 1) return fill(words.tomorrow, time);
        if (dayDigits) return fill(words.day, Number(dayDigits[1]) + dayOffset, time);

        return fill(words.later, dayOffset, time);
    }

    // The game's list as rows this file can sort: groups are the game's groupedEventList, crisis the ids it shows as
    // emergencies, lineIcons its LINE_ICON table. Rows come out in the game's order.
    function readRows(groups, crisis, lineIcons) {
        var rows = [];
        var seen = {};

        for (var g = 0; g < (groups ? groups.length : 0); g++) {
            var group = groups[g] || {};
            var items = group.items || [];

            for (var i = 0; i < items.length; i++) {
                var item = items[i];
                var id = item && item.eventConfigId;

                if (typeof id !== 'number' || seen[id]) continue;

                seen[id] = true;
                rows.push(readRow(item, group.key === 'main', crisis, lineIcons));
            }
        }

        return rows;
    }

    function readRow(item, isStory, crisis, lineIcons) {
        var icon = lineIcons && item.endingLine ? lineIcons[item.endingLine] : null;
        var timeText = item.time ? String(item.time) : '';

        return {
            id: item.eventConfigId,
            text: item.text != null ? String(item.text) : '',
            count: item.count ? String(item.count) : '',
            isStory: isStory,
            isExtra: !!item.extraRow,
            isCrisis: !item.extraRow && item.eventConfigId > 0 && !!crisis[item.eventConfigId],
            left: parseLeft(timeText),
            timeText: timeText,
            line: Array.isArray(icon) && icon.length >= 2 ? { key: String(item.endingLine), color: String(icon[0]), path: String(icon[1]) } : null,
            isFresh: !!item.isFresh,
            isEntering: !!item.isEntering
        };
    }

    // Which lane each row goes in, top to bottom. Now: emergencies in the game's order, then countdowns under six hours,
    // soonest first. Countdowns: the other countdowns, soonest first. In progress: the rest in the game's order. In a
    // siege only Now is its own lane; Everything else takes the countdowns, then the rest.
    function planLanes(rows, isSiege) {
        var crisis = [];
        var soon = [];
        var timed = [];
        var progress = [];

        for (var i = 0; i < rows.length; i++) {
            var row = rows[i];

            if (row.isCrisis) crisis.push(row);
            else if (row.left !== null && row.left < NOW_MINUTES) soon.push(row);
            else if (row.left !== null) timed.push(row);
            else progress.push(row);
        }

        soon.sort(bySoonest);
        timed.sort(bySoonest);

        var now = crisis.concat(soon);

        if (isSiege) return { now: now, timed: [], progress: [], rest: timed.concat(progress) };

        return { now: now, timed: timed, progress: progress, rest: [] };
    }

    // Array sort is stable, so countdowns with the same time left keep the game's order.
    function bySoonest(a, b) {
        return a.left - b.left;
    }

    // The folded lanes say what they hold: the next countdown in a siege, otherwise how many story events.
    function laneHint(key, rows, words) {
        if (key === 'rest' && rows.length && rows[0].left !== null) return fill(words.next, formatLeft(rows[0].left, words));
        if (!FOLDABLE[key]) return '';

        var stories = 0;

        for (var i = 0; i < rows.length; i++) {
            if (rows[i].isStory) stories++;
        }

        return stories ? fill(words.story, stories) : '';
    }

    function urgencyOf(row) {
        if (row.isCrisis) return 'now';
        if (row.left === null) return '';
        if (row.left < NOW_MINUTES) return 'now';

        return row.left < DAY_MINUTES ? 'soon' : 'later';
    }

    // The right-hand text: time left, "now" for an emergency with no countdown, or the text the game sent when this
    // script cannot read it.
    function timeLabel(row, words) {
        if (row.left !== null) return formatLeft(row.left, words);

        return row.isCrisis ? words.crisis : row.timeText;
    }

    // What the plugin knows about one event: its countdown's whole length in game seconds, and the items it asks
    // for as [icon, need, have]. Anything missing or in another shape is left out.
    function factOf(facts, id) {
        var fact = facts && facts[id];
        var total = Number(fact && fact.total);
        var needs = [];
        var listed = fact && Array.isArray(fact.needs) ? fact.needs : [];

        for (var i = 0; i < listed.length; i++) {
            var need = listed[i];

            if (!Array.isArray(need) || !(Number(need[1]) >= 1)) continue;

            needs.push({ icon: need[0] ? String(need[0]) : '', need: Math.floor(need[1]), have: Math.max(0, Math.floor(Number(need[2]) || 0)) });
        }

        return { totalMinutes: isFinite(total) && total > 0 ? total / 60 : 0, needs: needs };
    }

    function drainFraction(minutesLeft, totalMinutes) {
        return Math.max(0, Math.min(1, minutesLeft / totalMinutes)).toFixed(3);
    }

    function fill(template, a, b) {
        return template.replace('{0}', String(a)).replace('{1}', String(b));
    }

    function pad(n) {
        return n < 10 ? '0' + n : String(n);
    }

    // Our panel. send(action, data) is the game's UnitySendEvent; sync tells the page's click-through to measure
    // again. The panel carries data-no-sync, so the click-through does not remeasure on every countdown text we write;
    // draw says when rows came, went or moved, and the caller resyncs then.
    function createPanel(doc, send, sync) {
        var root = make(doc, 'div', ROOT_CLASS);
        var lanes = {};
        var rowsById = {};
        var folds = { progress: true, rest: true };
        var urgentIds = {};
        var wasSiege = false;
        var hasDrawn = false;
        var last = null;

        addStyle(doc);
        root.setAttribute('data-no-sync', '');

        for (var i = 0; i < LANES.length; i++) lanes[LANES[i]] = makeLane(doc, root, LANES[i]);

        root.addEventListener('click', onClick);
        root.addEventListener('wheel', onWheel, { passive: false });

        // host is the game's .event-panel, or null while the game has it off the page. view: { groups, crisis,
        // lineIcons, isSiege, clock, day, zh, facts }. Returns { changed, isNewlyUrgent }: changed when anything that
        // takes clicks came, went or moved; isNewlyUrgent when an event joined Now since the last draw.
        function draw(host, view) {
            var result = { changed: false, isNewlyUrgent: false };

            last = { host: host, view: view };

            if (!host) return result;

            if (root.parentElement !== host) {
                host.appendChild(root);
                result.changed = true;
            }

            if (view.isSiege && !wasSiege) folds.rest = true;

            wasSiege = !!view.isSiege;

            var words = view.zh ? WORDS.zh : WORDS.en;
            var plan = planLanes(readRows(view.groups, view.crisis || {}, view.lineIcons), wasSiege);
            var seen = {};
            var urgentNow = {};

            for (var l = 0; l < LANES.length; l++) {
                var key = LANES[l];
                var laneRows = plan[key];

                result.changed = drawLane(lanes[key], key, laneRows, words, view, seen) || result.changed;

                if (key !== 'now') continue;

                for (var n = 0; n < laneRows.length; n++) {
                    urgentNow[laneRows[n].id] = true;

                    if (hasDrawn && !urgentIds[laneRows[n].id]) result.isNewlyUrgent = true;
                }
            }

            result.changed = dropUnseen(seen) || result.changed;
            result.changed = routeClicks() || result.changed;
            urgentIds = urgentNow;
            hasDrawn = true;

            return result;
        }

        function drawLane(lane, key, laneRows, words, view, seen) {
            var changed = setClass(lane.el, 'is-empty', laneRows.length === 0);

            changed = setClass(lane.el, 'is-folded', !!FOLDABLE[key] && folds[key]) || changed;
            setText(lane.label, words[key]);
            setText(lane.count, String(laneRows.length));
            setText(lane.hint, laneHint(key, laneRows, words));

            for (var i = 0; i < laneRows.length; i++) {
                var row = laneRows[i];

                seen[row.id] = true;
                changed = placeRow(lane.rows, i, row, key === 'now', words, view) || changed;
            }

            return changed;
        }

        // Reuses the row's node while everything it was built from is the same, and only rewrites the parts that move
        // with the clock: the time left, its colour, when it runs out and the drain bar.
        function placeRow(container, index, row, hasSub, words, view) {
            var fact = factOf(view.facts, row.id);
            var endsAt = hasSub && row.left !== null ? formatEndsAt(row.left, view.clock, view.day, words) : null;
            var needs = hasSub ? fact.needs : [];
            var hasDrain = row.left !== null && fact.totalMinutes > 0;
            var key = rowKey(row, endsAt !== null, needs, hasDrain, words);
            var state = rowsById[row.id];
            var changed = false;

            if (!state || state.key !== key) {
                if (state && state.el.parentElement) state.el.parentElement.removeChild(state.el);

                state = rowsById[row.id] = buildRow(doc, row, endsAt !== null, needs, hasDrain, words, key);
                changed = true;
            }

            updateRow(state, row, endsAt, hasDrain ? drainFraction(row.left, fact.totalMinutes) : '', words);

            if (container.children[index] !== state.el) {
                container.insertBefore(state.el, container.children[index] || null);
                changed = true;
            }

            return changed;
        }

        function dropUnseen(seen) {
            var changed = false;

            for (var id in rowsById) {
                if (seen[id]) continue;

                var el = rowsById[id].el;

                if (el.parentElement) el.parentElement.removeChild(el);

                delete rowsById[id];
                changed = true;
            }

            return changed;
        }

        // While the panel overflows it takes clicks and the wheel as one rect and the rows and headers give theirs up,
        // the way the game's scroll mode does: the click-through does not clip to scroll boxes, so a row scrolled out
        // of sight would still catch clicks meant for the world. Returns whether any click area changed.
        function routeClicks() {
            var isOverflowing = root.scrollHeight > root.clientHeight + 1;
            var changed = setFlag(root, CLICKABLE, isOverflowing);

            for (var id in rowsById) changed = setFlag(rowsById[id].el, CLICKABLE, !isOverflowing) || changed;

            for (var key in FOLDABLE) changed = setFlag(lanes[key].head, CLICKABLE, !isOverflowing) || changed;

            return changed;
        }

        function onClick(e) {
            var target = e && e.target;

            if (!target || typeof target.closest !== 'function') return;

            var head = target.closest('[' + FOLD + ']');

            if (head) {
                toggleFold(head.getAttribute(FOLD));

                return;
            }

            var rowEl = target.closest('[' + ROW_ID + ']');
            var state = rowEl ? rowsById[rowEl.getAttribute(ROW_ID)] : null;

            if (!state) return;

            if (state.isExtra) send('OPEN_PHONE');
            else send('EVENT_CLICK', { eventConfigId: state.id });
        }

        function toggleFold(key) {
            if (!FOLDABLE[key]) return;

            folds[key] = !folds[key];

            if (last) draw(last.host, last.view);

            sync();
        }

        // Works like the game's wheel handler for its scroll box: scroll the list, and keep the wheel from the page
        // and the camera under it while there is something to scroll.
        function onWheel(e) {
            if (root.scrollHeight <= root.clientHeight + 1) return;

            e.stopPropagation();
            e.preventDefault();
            root.scrollTop += e.deltaY;
        }

        function remove() {
            if (root.parentElement) root.parentElement.removeChild(root);
        }

        return { root: root, draw: draw, remove: remove, isFolded: function (key) { return !!FOLDABLE[key] && folds[key]; } };
    }

    function makeLane(doc, root, key) {
        var el = root.appendChild(make(doc, 'div', 'sl-ln sl-ln-' + key + ' is-empty'));
        var head = el.appendChild(make(doc, 'div', 'sl-ln-head'));

        head.appendChild(make(doc, 'span', 'sl-ln-dot'));

        var lane = {
            el: el,
            head: head,
            label: head.appendChild(make(doc, 'span', 'sl-ln-label')),
            count: head.appendChild(make(doc, 'span', 'sl-ln-n'))
        };

        if (FOLDABLE[key]) {
            head.setAttribute(FOLD, key);
            head.appendChild(make(doc, 'span', 'sl-ln-chev')).textContent = CHEVRON;
        }

        lane.hint = head.appendChild(make(doc, 'span', 'sl-ln-hint'));
        lane.rows = el.appendChild(make(doc, 'div', 'sl-ln-rows'));

        return lane;
    }

    // Everything a row's node is built from, apart from what updateRow rewrites in place.
    function rowKey(row, hasEndsAt, needs, hasDrain, words) {
        var parts = [row.text, row.count, row.isStory, row.isExtra, row.line ? row.line.key : '', hasEndsAt, hasDrain, words.need];

        for (var i = 0; i < needs.length; i++) parts.push(needs[i].icon + ':' + needs[i].need + ':' + needs[i].have);

        return parts.join('\u0001');
    }

    function buildRow(doc, row, hasEndsAt, needs, hasDrain, words, key) {
        var el = make(doc, 'div', 'sl-ln-row');
        var state = { id: row.id, isExtra: row.isExtra, key: key, el: el, time: null, endsAt: null, drain: null };

        el.setAttribute(ROW_ID, String(row.id));
        setClass(el, 'is-story', row.isStory);
        el.appendChild(makeMark(doc, row.line));
        el.appendChild(make(doc, 'span', 'sl-ln-text')).textContent = row.text;

        var right = el.appendChild(make(doc, 'span', 'sl-ln-right'));

        if (row.count) right.appendChild(make(doc, 'span', 'sl-ln-count')).textContent = row.count;

        state.time = right.appendChild(make(doc, 'span', 'sl-ln-time'));

        if (hasEndsAt || needs.length) {
            var sub = el.appendChild(make(doc, 'span', 'sl-ln-sub'));

            setClass(el, 'has-sub', true);

            if (hasEndsAt) state.endsAt = sub.appendChild(make(doc, 'span', 'sl-ln-ends'));

            addNeeds(doc, sub, needs, hasEndsAt, words);
        }

        if (hasDrain) state.drain = el.appendChild(make(doc, 'i', 'sl-ln-drain'));

        return state;
    }

    function addNeeds(doc, sub, needs, hasEndsAt, words) {
        for (var i = 0; i < needs.length; i++) {
            var need = needs[i];

            if (i > 0 || hasEndsAt) sub.appendChild(make(doc, 'span', 'sl-ln-sep')).textContent = i > 0 ? ' ' : DOT;

            if (need.icon) {
                var img = sub.appendChild(make(doc, 'img', 'sl-ln-item'));

                img.setAttribute('src', ICON_ROOT + need.icon);
                img.setAttribute('alt', '');
            }

            var text = sub.appendChild(make(doc, 'span', 'sl-ln-need'));

            text.textContent = fill(words.need, need.need, need.have);
            setClass(text, 'is-met', need.have >= need.need);
        }
    }

    // The game's mark: its story-line icon in the line's colour, or the diamond.
    function makeMark(doc, line) {
        if (!line) {
            var diamond = make(doc, 'span', 'sl-ln-mark sl-ln-diamond');

            diamond.textContent = DIAMOND;

            return diamond;
        }

        var svg = doc.createElementNS(SVG_NS, 'svg');
        var path = doc.createElementNS(SVG_NS, 'path');

        svg.setAttribute('class', 'sl-ln-mark');
        svg.setAttribute('viewBox', '0 0 24 24');
        svg.style.setProperty('color', line.color);
        path.setAttribute('fill-rule', 'evenodd');
        path.setAttribute('d', line.path);
        svg.appendChild(path);

        return svg;
    }

    function updateRow(state, row, endsAt, drain, words) {
        var urgency = urgencyOf(row);

        setText(state.time, timeLabel(row, words));
        setClass(state.time, 'now', urgency === 'now');
        setClass(state.time, 'soon', urgency === 'soon');
        setClass(state.time, 'later', urgency === 'later');
        setClass(state.el, 'is-fresh', row.isFresh);
        setClass(state.el, 'is-entering', row.isEntering);

        if (state.endsAt) setText(state.endsAt, fill(words.runsOut, endsAt));
        if (state.drain && state.drain.style.getPropertyValue('--sl-left') !== drain) state.drain.style.setProperty('--sl-left', drain);
    }

    function addStyle(doc) {
        if (doc.getElementById(STYLE_ID)) return;

        var style = doc.createElement('style');

        style.id = STYLE_ID;
        style.textContent = CSS;
        (doc.head || doc.documentElement).appendChild(style);
    }

    function make(doc, tagName, className) {
        var el = doc.createElement(tagName);
        var names = className ? className.split(' ') : [];

        for (var i = 0; i < names.length; i++) el.classList.add(names[i]);

        return el;
    }

    // The writers below touch the page only when the value differs, and say whether they did.
    function setText(el, value) {
        if (el.textContent === value) return false;

        el.textContent = value;

        return true;
    }

    function setClass(el, name, isOn) {
        if (el.classList.contains(name) === isOn) return false;

        if (isOn) el.classList.add(name);
        else el.classList.remove(name);

        return true;
    }

    function setFlag(el, name, isOn) {
        if (el.hasAttribute(name) === isOn) return false;

        if (isOn) el.setAttribute(name, '');
        else el.removeAttribute(name);

        return true;
    }

    window.__sleventlistLanes = {
        version: '0.1.0',
        createPanel: createPanel,
        rules: { parseLeft: parseLeft, formatLeft: formatLeft, formatEndsAt: formatEndsAt, readRows: readRows, planLanes: planLanes, laneHint: laneHint, factOf: factOf, words: WORDS }
    };
})();
