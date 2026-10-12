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
    var MARK = 'data-sl-important';
    var CLICKABLE = 'data-interactive';
    var DEFAULT_MAX_HEIGHT = 120;
    var TIER_RANK = { '': 0, deadline: 1, crisis: 2 };

    // The class and the height sit on <html>, outside the body the page's click-through watches, so setting them
    // never makes it resend its rects. The scrollbar is the game's own 4px .event-scroll scrollbar.
    // CSS order moves rows on screen without moving them in the DOM, so Vue's keyed patching never sees a change.
    var CSS = [
        'html.sl-eventlist .event-panel > .event-scroll { max-height: var(--sl-eventlist-max, 120px); overflow-y: auto; overflow-x: hidden; overscroll-behavior: contain; }',
        'html.sl-eventlist .event-panel > .event-scroll > .event-group { flex-shrink: 0; }',
        'html.sl-eventlist .event-panel > .event-scroll > .event-group[data-sl-important="deadline"] { order: -1; }',
        'html.sl-eventlist .event-panel > .event-scroll > .event-group[data-sl-important="crisis"] { order: -2; }',
        'html.sl-eventlist .event-group > .event-group-header { order: -3; }',
        'html.sl-eventlist .event-group > .event-item-row[data-sl-important="deadline"] { order: -1; }',
        'html.sl-eventlist .event-group > .event-item-row[data-sl-important="crisis"] { order: -2; }'
    ].join('\n');

    var settings = { maxHeight: DEFAULT_MAX_HEIGHT, importantOnTop: true };
    var hook = null;
    var lastImportant = {};
    var lastError = '';

    function apply(next) {
        try {
            var wanted = readSettings(next);
            var isChanged = wanted.maxHeight !== settings.maxHeight || wanted.importantOnTop !== settings.importantOnTop;
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

    function readSettings(next) {
        var height = Number(next && next.maxHeight);

        return {
            maxHeight: isFinite(height) && height > 0 ? Math.round(height) : DEFAULT_MAX_HEIGHT,
            importantOnTop: !(next && next.importantOnTop === false)
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

        return '';
    }

    function unhook() {
        if (!hook) return;

        for (var i = 0; i < hook.stops.length; i++) {
            try {
                hook.stops[i]();
            } catch (e) {
                // A watcher that cannot stop is dropped with the rest of the hook.
            }
        }

        hook = null;
        lastImportant = {};
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

        var box = document.querySelector('.event-panel > .event-scroll');

        if (!box) {
            lastImportant = {};

            if (force) hook.core.clickThrough.syncNowAfterReflow();

            return;
        }

        var changed = markImportance(box);
        changed = routeClicks(box) || changed;

        if (changed || force) hook.core.clickThrough.syncNowAfterReflow();
    }

    // While the list overflows, the box takes clicks and the wheel as one rect and the rows give theirs up, the way the
    // game's own scroll mode does it. The page's click-through does not clip to scroll boxes, so a row scrolled out of
    // sight would otherwise still catch clicks meant for the world. A list that fits keeps the game's per-row rects.
    function routeClicks(box) {
        var isOverflowing = box.scrollHeight > box.clientHeight + 1;
        var rowsCatch = !isOverflowing && !hook.ss.eventScrollMode;
        var rows = box.querySelectorAll('.event-item-row');
        var changed = setFlag(box, CLICKABLE, isOverflowing);

        for (var i = 0; i < rows.length; i++) changed = setFlag(rows[i], CLICKABLE, rowsCatch) || changed;

        return changed;
    }

    // Rows and groups are matched to the data Vue drew them from by position, which the keyed patch keeps in step.
    // Each row's shown text is compared with its item's text only to prove the match; importance never comes from text.
    // Any mismatch leaves the marks as they were until the next pass.
    function markImportance(box) {
        if (!settings.importantOnTop) return clearMarks(box);

        var groups = hook.ss.groupedEventList;
        var groupEls = childrenWithClass(box, 'event-group');

        if (!groups || groupEls.length !== groups.length) return false;

        var crisis = crisisIds(hook.st);
        var marks = [];
        var important = {};
        var isNewlyImportant = false;

        for (var g = 0; g < groups.length; g++) {
            var items = groups[g].items || [];
            var rowEls = childrenWithClass(groupEls[g], 'event-item-row');
            var groupTier = '';

            if (rowEls.length !== items.length) return false;

            for (var k = 0; k < items.length; k++) {
                var item = items[k];

                if (!showsItem(rowEls[k], item)) return false;

                var tier = tierOf(item, crisis);
                marks.push([rowEls[k], tier]);

                if (!tier) continue;

                if (TIER_RANK[tier] > TIER_RANK[lastImportant[item.eventConfigId] || '']) isNewlyImportant = true;
                if (TIER_RANK[tier] > TIER_RANK[groupTier]) groupTier = tier;

                important[item.eventConfigId] = tier;
            }

            marks.push([groupEls[g], groupTier]);
        }

        var changed = false;

        for (var m = 0; m < marks.length; m++) changed = setMark(marks[m][0], marks[m][1]) || changed;

        lastImportant = important;

        if (isNewlyImportant) box.scrollTop = 0;

        return changed;
    }

    // crisis: the game shows this event as one of its red emergency entries, or it is the zombie wave that is
    // spawning or charging. deadline: the event shows a running countdown. Extra tracker rows are never important.
    function tierOf(item, crisis) {
        var id = item && item.eventConfigId;

        if (!(id > 0) || item.extraRow) return '';
        if (crisis[id]) return 'crisis';
        if (item.timeActive && item.remainSeconds > 0) return 'deadline';

        return '';
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

        for (var i = 0; i < marked.length; i++) marked[i].removeAttribute(MARK);

        lastImportant = {};

        return marked.length > 0;
    }

    function setMark(el, tier) {
        if ((el.getAttribute(MARK) || '') === tier) return false;

        if (tier) el.setAttribute(MARK, tier);
        else el.removeAttribute(MARK);

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

    window.__sleventlist = { version: '0.1.0', apply: apply };
})();
