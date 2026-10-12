// Event List: runs inside the CoreUI1 page, the iframe in Root.html that draws the HUD.
// Holds the events list to a fixed height (the rest scrolls) and moves important events to the top.
// Sent once per page load; the plugin then calls window.__sleventlist.apply(settings) every couple of seconds.
// Touches only this page's DOM, one stylesheet, and the page-only eventHover flag; nothing reaches the game.
(function () {
    'use strict';

    if (window.__sleventlist) return;

    var STYLE_ID = 'sl-eventlist-style';
    var ACTIVE_CLASS = 'sl-eventlist';
    var HEIGHT_VAR = '--sl-eventlist-max';
    var PANEL = '.event-panel';
    var BOX = '.event-panel > .event-scroll';
    var ROW = '.event-item-row';
    var MARK = 'data-sl-important';
    var RANK = 'data-sl-rank';
    var CLICKABLE = 'data-interactive';
    var DEFAULT_MAX_HEIGHT = 136;
    var TIER_RANK = { '': 0, deadline: 1, crisis: 2 };

    // Countdowns past this many in one group or one list get no rank and sit after the ranked ones in the game's order.
    var RANKED = 64;

    // The class and the height sit on <html>, outside the body the page's click-through watches, so setting them
    // never makes it resend its rects. The scrollbar is the game's own 4px .event-scroll scrollbar.
    // CSS order moves rows on screen without moving them in the DOM, so Vue's keyed patching never sees a change.
    // Equal orders keep DOM order, which is the game's. A ranked countdown sits between the emergencies and the
    // unranked countdowns, its rank counting up from -800.
    var CSS = [
        'html.sl-eventlist .event-panel > .event-scroll { max-height: var(--sl-eventlist-max, ' + DEFAULT_MAX_HEIGHT + 'px); overflow-y: auto; overflow-x: hidden; overscroll-behavior: contain; }',
        'html.sl-eventlist .event-panel > .event-scroll > .event-group { flex-shrink: 0; }',
        'html.sl-eventlist .event-panel > .event-scroll > .event-group[data-sl-important="deadline"] { order: -700; }',
        'html.sl-eventlist .event-panel > .event-scroll > .event-group[data-sl-important="crisis"] { order: -900; }',
        'html.sl-eventlist .event-group > .event-group-header { order: -1000; }',
        'html.sl-eventlist .event-group > .event-item-row[data-sl-important="deadline"] { order: -700; }',
        'html.sl-eventlist .event-group > .event-item-row[data-sl-important="crisis"] { order: -900; }'
    ].concat(rankedCountdownRules()).join('\n');

    function rankedCountdownRules() {
        var rules = [];

        for (var r = 0; r < RANKED; r++) {
            var rank = '[data-sl-important="deadline"][' + RANK + '="' + r + '"] { order: ' + (r - 800) + '; }';

            rules.push('html.sl-eventlist .event-panel > .event-scroll > .event-group' + rank);
            rules.push('html.sl-eventlist .event-group > .event-item-row' + rank);
        }

        return rules;
    }

    var settings = readSettings(null);
    var hook = null;
    var lastImportant = {};
    var lastError = '';
    var isPointerOver = false;
    var isScrollPending = false;

    function apply(next) {
        try {
            var wanted = readSettings(next);
            var isChanged = wanted.maxHeight !== settings.maxHeight || wanted.importantOnTop !== settings.importantOnTop || wanted.countdownOnTop !== settings.countdownOnTop || wanted.soonestFirst !== settings.soonestFirst;
            var previousHook = hook;
            settings = wanted;

            var problem = hookPage();

            if (problem) {
                unhook();
                hideStyle();

                return 'unsupported: ' + problem;
            }

            showStyle();

            // A fresh hook or a new height changes the layout Vue is about to patch; read it after the patch lands.
            if (hook !== previousHook || isChanged) window.Vue.nextTick(function () { safeRefresh(true); });
            else safeRefresh(false);

            return takeError() || 'ok';
        } catch (e) {
            return 'error: ' + describe(e);
        }
    }

    // Anything the plugin did not send, or sent in a shape this script does not know, means the default.
    function readSettings(next) {
        var height = Number(next && next.maxHeight);

        return {
            maxHeight: isFinite(height) && height > 0 ? Math.round(height) : DEFAULT_MAX_HEIGHT,
            importantOnTop: !(next && next.importantOnTop === false),
            countdownOnTop: !(next && next.countdownOnTop === false),
            soonestFirst: !(next && next.soonestFirst === false)
        };
    }

    // Returns '' once hooked, or why this page is not the one the script was written against.
    function hookPage() {
        var Vue = window.Vue;

        if (!Vue || typeof Vue.watch !== 'function' || typeof Vue.watchEffect !== 'function' || typeof Vue.nextTick !== 'function') return 'no Vue';

        var ss = rootSetupState();

        if (!ss) return 'no Vue app on #app';
        if (hook && hook.ss === ss) return '';
        if (!('groupedEventList' in ss) || !('eventScrollMode' in ss) || !ss.state) return 'no event list in the app';

        var st = ss.state;
        var core = pageGlobal('core');

        if (!Array.isArray(st.eventList) || !('eventHover' in st)) return 'the event list state has a new shape';
        if (!core || !core.clickThrough || typeof core.clickThrough.syncNowAfterReflow !== 'function') return 'no click-through';

        unhook();
        hook = { ss: ss, st: st, core: core, stops: [] };

        // The game cuts its list to four rows plus "N more" while a rescue, siege or emergency shows, unless the
        // pointer is over it. Holding its hover flag on keeps every row in the scroll box, so nothing important is cut.
        // The flag is read only by the list itself; the game never sees it.
        hook.stops.push(Vue.watchEffect(function () {
            if (!st.eventHover) st.eventHover = true;
        }, { flush: 'sync' }));

        // Runs once after each patch that changes the list, never for the per-second countdowns and never for our own
        // DOM writes, so there is no feedback loop. The two-second apply from the plugin is the backstop.
        hook.stops.push(Vue.watch([
            function () { return ss.groupedEventList; },
            function () { return ss.eventScrollMode; },
            function () { return st.eventListActive; },
            function () { return crisisKey(st); }
        ], function () { safeRefresh(false); }, { flush: 'post' }));

        // With the hover flag held on, it no longer says where the pointer is, so the script follows the pointer itself.
        // Capture phase, so a handler in the page that stops the event cannot hide it from us.
        document.addEventListener('mouseover', onPointerOver, true);
        document.addEventListener('mouseout', onPointerOut, true);
        hook.stops.push(function () {
            document.removeEventListener('mouseover', onPointerOver, true);
            document.removeEventListener('mouseout', onPointerOut, true);
        });

        return '';
    }

    function unhook() {
        if (!hook) return;

        var dropped = hook;
        hook = null;
        lastImportant = {};
        isPointerOver = false;
        isScrollPending = false;

        for (var i = 0; i < dropped.stops.length; i++) {
            try {
                dropped.stops[i]();
            } catch (e) {
                // A watcher that cannot stop is dropped with the rest of the hook.
            }
        }

        try {
            giveBackToGame(dropped);
        } catch (e) {
            // A page going away or changing shape may refuse some of it; what is left goes with the page.
        }
    }

    // Puts the list back the way the game draws it: hover flag off as when the pointer is away, the game's own click
    // areas (the box takes clicks through its own class in scroll mode, rows take their own otherwise), and no marks.
    function giveBackToGame(dropped) {
        var box = document.querySelector(BOX);

        dropped.st.eventHover = false;

        if (box) {
            var rows = box.querySelectorAll(ROW);
            var rowsCatch = !dropped.ss.eventScrollMode;

            setFlag(box, CLICKABLE, false);

            for (var i = 0; i < rows.length; i++) setFlag(rows[i], CLICKABLE, rowsCatch);

            clearMarks(box);
        }

        dropped.core.clickThrough.syncNowAfterReflow();
    }

    // In the production build the root vnode and its component stay on the mount point, and setupState is what the
    // template reads: groupedEventList, eventScrollMode, state.
    function rootSetupState() {
        var app = document.getElementById('app');
        var vnode = app && app._vnode;
        var instance = vnode && vnode.component;

        return instance && instance.setupState ? instance.setupState : null;
    }

    // The page keeps `core` in a top-level const, which is not on window; an indirect eval reads the page's globals.
    function pageGlobal(name) {
        try {
            return (0, eval)(name);
        } catch (e) {
            return undefined;
        }
    }

    function showStyle() {
        var html = document.documentElement;
        var height = settings.maxHeight + 'px';

        if (!document.getElementById(STYLE_ID)) {
            var style = document.createElement('style');
            style.id = STYLE_ID;
            style.textContent = CSS;
            (document.head || html).appendChild(style);
        }

        if (html.style.getPropertyValue(HEIGHT_VAR) !== height) html.style.setProperty(HEIGHT_VAR, height);

        html.classList.add(ACTIVE_CLASS);
    }

    function hideStyle() {
        document.documentElement.classList.remove(ACTIVE_CLASS);
    }

    function safeRefresh(force) {
        try {
            refresh(force);
        } catch (e) {
            lastError = describe(e);
        }
    }

    function takeError() {
        var error = lastError;
        lastError = '';

        return error ? 'refresh failed: ' + error : '';
    }

    function refresh(force) {
        if (!hook) return;

        var box = document.querySelector(BOX);

        if (!box) {
            lastImportant = {};

            if (force) hook.core.clickThrough.syncNowAfterReflow();

            return;
        }

        var changed = markImportance(box);
        changed = routeClicks(box, hook.ss.eventScrollMode) || changed;

        if (changed || force) hook.core.clickThrough.syncNowAfterReflow();
    }

    // While the list overflows, the box takes clicks and the wheel as one rect and the rows give theirs up, the way the
    // game's own scroll mode does it. The page's click-through does not clip to scroll boxes, so a row scrolled out of
    // sight would otherwise still catch clicks meant for the world. A list that fits keeps the game's per-row rects.
    // Returns whether any click area changed, so the click-through is resent only then.
    function routeClicks(box, isGameScrollMode) {
        var isOverflowing = box.scrollHeight > box.clientHeight + 1;
        var rowsCatch = !isOverflowing && !isGameScrollMode;
        var rows = box.querySelectorAll(ROW);
        var changed = setFlag(box, CLICKABLE, isOverflowing);

        for (var i = 0; i < rows.length; i++) changed = setFlag(rows[i], CLICKABLE, rowsCatch) || changed;

        return changed;
    }

    // Rows and groups are matched to the data Vue drew them from by position, which the keyed patch keeps in step.
    // Each row's shown text is compared with its item's text only to prove the match; importance never comes from text.
    // Any mismatch leaves the marks as they were until the next pass. Countdown rows are ranked within their group, and
    // countdown groups among themselves by their soonest countdown.
    function markImportance(box) {
        if (!settings.importantOnTop) return clearMarks(box);

        var groups = hook.ss.groupedEventList;
        var groupEls = childrenWithClass(box, 'event-group');

        if (!groups || groupEls.length !== groups.length) return false;

        var crisis = crisisIds(hook.st);
        var marks = [];
        var ranks = [];
        var countdownGroups = { els: [], times: [] };
        var important = {};
        var isNewlyImportant = false;

        for (var g = 0; g < groups.length; g++) {
            var items = groups[g].items || [];
            var rowEls = childrenWithClass(groupEls[g], 'event-item-row');
            var countdownRows = { els: [], times: [] };
            var groupTier = '';

            if (rowEls.length !== items.length) return false;

            for (var k = 0; k < items.length; k++) {
                var item = items[k];

                if (!showsItem(rowEls[k], item)) return false;

                var tier = tierOf(item, crisis, settings.countdownOnTop);
                marks.push([rowEls[k], tier]);

                if (tier === 'deadline') addCountdown(countdownRows, rowEls[k], item.remainSeconds);
                else ranks.push([rowEls[k], '']);

                if (!tier) continue;

                if (TIER_RANK[tier] > TIER_RANK[lastImportant[item.eventConfigId] || '']) isNewlyImportant = true;
                if (TIER_RANK[tier] > TIER_RANK[groupTier]) groupTier = tier;

                important[item.eventConfigId] = tier;
            }

            marks.push([groupEls[g], groupTier]);
            rankCountdowns(countdownRows, ranks);

            if (groupTier === 'deadline') addCountdown(countdownGroups, groupEls[g], soonestTimeLeft(countdownRows.times));
            else ranks.push([groupEls[g], '']);
        }

        rankCountdowns(countdownGroups, ranks);

        var changed = false;

        for (var m = 0; m < marks.length; m++) changed = setMark(marks[m][0], marks[m][1]) || changed;
        for (var n = 0; n < ranks.length; n++) changed = setRank(ranks[n][0], ranks[n][1]) || changed;

        lastImportant = important;

        if (isNewlyImportant) scrollToTopWhenFree(box);

        return changed;
    }

    // An event that newly becomes important brings the list back to the top so it is seen, but never under the
    // pointer: a player reading or scrolling the list is not yanked, and the list goes back up once the pointer leaves.
    function scrollToTopWhenFree(box) {
        if (isPointerOver) isScrollPending = true;
        else box.scrollTop = 0;
    }

    function onPointerOver(e) {
        pointerMoved(isInPanel(e.target));
    }

    // Moving between parts of the list fires mouseout too; only a move to somewhere outside it is leaving.
    function onPointerOut(e) {
        if (!isInPanel(e.relatedTarget)) pointerMoved(false);
    }

    function pointerMoved(isOver) {
        isPointerOver = isOver;

        if (isOver || !isScrollPending) return;

        var box = document.querySelector(BOX);
        isScrollPending = false;

        if (box) box.scrollTop = 0;
    }

    function isInPanel(el) {
        return !!(el && typeof el.closest === 'function' && el.closest(PANEL));
    }

    // crisis: the game shows this event as one of its red emergency entries, or it is the zombie wave that is
    // spawning or charging. deadline: the event shows a running countdown, while countdowns count as important.
    // Extra tracker rows are never important.
    function tierOf(item, crisis, isCountdownImportant) {
        var id = item && item.eventConfigId;

        if (!(id > 0) || item.extraRow) return '';
        if (crisis[id]) return 'crisis';
        if (isCountdownImportant && item.timeActive && item.remainSeconds > 0) return 'deadline';

        return '';
    }

    function addCountdown(countdowns, el, timeLeft) {
        countdowns.els.push(el);
        countdowns.times.push(timeLeft);
    }

    // With soonest first off, every countdown loses its rank and the tier keeps the game's order.
    function rankCountdowns(countdowns, ranks) {
        var order = settings.soonestFirst ? rankByTimeLeft(countdowns.times) : [];

        for (var i = 0; i < countdowns.els.length; i++) {
            var rank = order[i];

            ranks.push([countdowns.els[i], rank < RANKED ? String(rank) : '']);
        }
    }

    // Each countdown's place in its tier, from its whole seconds left, soonest first. Ties keep the game's order, and a
    // countdown whose time left cannot be read goes after every one that can, in the game's order.
    function rankByTimeLeft(times) {
        var order = [];
        var ranks = [];

        for (var i = 0; i < times.length; i++) order.push(i);

        order.sort(function (a, b) {
            var left = sortableTimeLeft(times[a]);
            var right = sortableTimeLeft(times[b]);

            if (left !== right) return left < right ? -1 : 1;

            return a - b;
        });

        for (var r = 0; r < order.length; r++) ranks[order[r]] = r;

        return ranks;
    }

    // A group of countdowns is as urgent as its soonest one; undefined when none can be read.
    function soonestTimeLeft(times) {
        var soonest;

        for (var i = 0; i < times.length; i++) {
            if (isReadableTime(times[i]) && (soonest === undefined || times[i] < soonest)) soonest = times[i];
        }

        return soonest;
    }

    // Whole seconds, rounded up: two times inside the same second, or times apart only by float noise between the game's
    // messages, count as a tie and keep the game's order.
    function sortableTimeLeft(time) {
        return isReadableTime(time) ? Math.ceil(time) : Infinity;
    }

    function isReadableTime(time) {
        return typeof time === 'number' && isFinite(time);
    }

    function crisisIds(st) {
        var ids = {};
        var states = st.eventStatesActive ? st.eventStates : null;

        if (states) {
            for (var i = 0; i < states.length; i++) {
                var id = states[i] && states[i].eventConfigId;

                if (id > 0) ids[id] = true;
            }
        }

        if ((st.towerWaveState === 1 || st.towerWaveState === 2) && st.towerWaveEventId > 0) ids[st.towerWaveEventId] = true;

        return ids;
    }

    function crisisKey(st) {
        return Object.keys(crisisIds(st)).join(',');
    }

    function showsItem(rowEl, item) {
        var textEl = firstChildWithClass(rowEl, 'event-text');
        var text = item && item.text != null ? String(item.text) : '';

        return !!textEl && textEl.textContent === text;
    }

    function clearMarks(box) {
        var marked = box.querySelectorAll('[' + MARK + ']');
        var ranked = box.querySelectorAll('[' + RANK + ']');

        for (var i = 0; i < marked.length; i++) marked[i].removeAttribute(MARK);
        for (var j = 0; j < ranked.length; j++) ranked[j].removeAttribute(RANK);

        lastImportant = {};

        return marked.length > 0 || ranked.length > 0;
    }

    function setMark(el, tier) {
        return setText(el, MARK, tier);
    }

    function setRank(el, rank) {
        return setText(el, RANK, rank);
    }

    // An empty value removes the attribute. Returns whether anything changed.
    function setText(el, name, value) {
        if ((el.getAttribute(name) || '') === value) return false;

        if (value) el.setAttribute(name, value);
        else el.removeAttribute(name);

        return true;
    }

    function setFlag(el, name, isOn) {
        if (el.hasAttribute(name) === isOn) return false;

        if (isOn) el.setAttribute(name, '');
        else el.removeAttribute(name);

        return true;
    }

    function childrenWithClass(parent, className) {
        var found = [];

        for (var i = 0; i < parent.children.length; i++) {
            if (parent.children[i].classList.contains(className)) found.push(parent.children[i]);
        }

        return found;
    }

    function firstChildWithClass(parent, className) {
        var found = childrenWithClass(parent, className);

        return found.length ? found[0] : null;
    }

    function describe(e) {
        return String(e && e.message ? e.message : e);
    }

    // apply is the plugin's way in. rules are the parts that decide what the list does, kept here for the tests; the
    // page never calls them.
    window.__sleventlist = {
        version: '0.2.0',
        apply: apply,
        rules: { readSettings: readSettings, tierOf: tierOf, rankByTimeLeft: rankByTimeLeft, crisisIds: crisisIds, routeClicks: routeClicks }
    };
})();
