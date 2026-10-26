// Event List: runs inside the CoreUI1 page, the iframe in Root.html that draws the HUD.
// Holds the events list to a fixed height (the rest scrolls) and moves important events to the top, and puts the
// event badge on the phone: how many event texts wait to be accepted.
// Sent once per page load; the plugin then calls window.__sleventlist.apply(settings, badge, phoneDocument) every
// couple of seconds. phoneDocument is the PhoneSMS page's, or null while the game has no phone screen up.
// Touches only the DOM of this page and the phone page, one stylesheet in each, and the page-only eventHover flag;
// nothing reaches the game.
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

    // The event badge sits on the top-left corner, across from the game's own red text badge on the top-right, and is
    // drawn in the game's badge shape at each spot's size. Waiting texts make it teal; once one of them can run out it
    // turns orange, gains a clock and glows. It never takes the pointer, so clicks reach the button or app under it.
    var BADGE_STYLE_ID = 'sl-eventbadge-style';
    var BADGE_CLASS = 'sl-eventbadge';
    var TIMED_CLASS = 'is-timed';
    var CLOCK = '\u231B';

    // The HUD's phone button has no class of its own; its icon is the one path no other CoreUI1 button draws.
    var PHONE_ICON = 'M17 1.01L7 1c';
    var HUD_ICONS = '.icon-sq > .icon-svg';
    var PHONE_APP = '.app-dock > .is-sms';
    var PHONE_DOCK = '.app-dock';
    var PHONE_PAGE = /PhoneSMS\.html/i;
    var CHASE_FRAMES = 120;
    // The badge from the plugin's last call, drawn on a phone page the moment the game builds it.
    var lastBadge = null;

    var HUD_BADGE_CSS = badgeCss({ edge: -4, height: 15, padding: 4, font: 9.5, border: '1px solid rgba(15, 15, 15, 0.8)' });
    var APP_BADGE_CSS = badgeCss({ edge: -5, height: 22, padding: 6, font: 12, border: '2px solid #2a221b' });

    function badgeCss(size) {
        var badge = '.' + BADGE_CLASS;
        var timed = badge + '.' + TIMED_CLASS;

        return [
            badge + ' { position: absolute; top: ' + size.edge + 'px; left: ' + size.edge + 'px; z-index: 5; box-sizing: border-box; min-width: ' + size.height + 'px; height: ' + size.height + 'px; padding: 0 ' + size.padding + 'px; border-radius: ' + (size.height / 2) + 'px; border: ' + size.border + '; display: flex; align-items: center; justify-content: center; font-size: ' + size.font + 'px; font-weight: 700; line-height: 1; font-variant-numeric: tabular-nums; white-space: nowrap; color: #e9fdff; background: linear-gradient(180deg, #2bb8c9 0%, #0b6f7c 100%); box-shadow: 0 1px 2px rgba(0, 0, 0, 0.55), inset 0 1px 0 rgba(255, 255, 255, 0.25); pointer-events: none; }',
            timed + ' { color: #fff6e0; background: linear-gradient(180deg, #ffa632 0%, #d94f00 100%); animation: sl-eventbadge-glow 1.2s ease-in-out infinite; }',
            '@keyframes sl-eventbadge-glow { 0%, 100% { box-shadow: 0 1px 2px rgba(0, 0, 0, 0.55), 0 0 0 0 rgba(255, 150, 40, 0); } 50% { box-shadow: 0 1px 2px rgba(0, 0, 0, 0.55), 0 0 7px 2px rgba(255, 150, 40, 0.8); } }',
            'html.quality-low ' + timed + ' { animation: none; box-shadow: 0 1px 2px rgba(0, 0, 0, 0.55), 0 0 5px 1px rgba(255, 150, 40, 0.7); }'
        ].join('\n');
    }

    var settings = readSettings(null);
    var hook = null;
    var lastImportant = {};
    var lastError = '';
    var isPointerOver = false;
    var isScrollPending = false;

    // The badge is drawn before the list and whatever the list finds, so a list this script cannot hold still leaves
    // the badge working. A badge spot that should be on the page and is not is reported after the list's answer and
    // '; ', so a held list answers 'ok; ' and the note.
    function apply(next, nextBadge, phoneDocument) {
        lastBadge = readBadge(nextBadge);

        var badgeNote = showBadges(lastBadge, phoneDocument);

        return withNote(applyList(next), badgeNote);
    }

    function withNote(answer, note) {
        return note ? answer + '; ' + note : answer;
    }

    function applyList(next) {
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

    // Anything the plugin did not send, or sent in a shape this script does not know, means no badge.
    function readBadge(next) {
        var waiting = Number(next && next.waiting);

        if (!(isFinite(waiting) && waiting >= 1)) return { waiting: 0, timed: false };

        return { waiting: Math.floor(waiting), timed: next.timed === true };
    }

    // The count the way the game writes its own badges, 9+ past nine, after a clock when a waiting text can run out.
    function badgeLabel(badge) {
        var count = badge.waiting > 9 ? '9+' : String(badge.waiting);

        return badge.timed ? CLOCK + count : count;
    }

    // Returns '' or, for the log, which badge spot could not be found. Never throws: the list is applied after it.
    function showBadges(badge, phoneDocument) {
        try {
            var missing = [];

            if (!showHudBadge(badge)) missing.push('no phone button on the HUD, so the event badge is not shown there');
            if (phoneDocument && !showAppBadge(phoneDocument, badge)) missing.push('no messages app on the phone screen, so the event badge is not shown there');

            return missing.join('; ');
        } catch (e) {
            return 'event badge failed: ' + describe(e);
        }
    }

    // False only when texts wait, the game says its phone button is up, and the button cannot be found.
    function showHudBadge(badge) {
        var button = hudPhoneButton();

        if (!button) return !(badge.waiting && isPhoneButtonUp());

        placeBadge(document, button, badge, HUD_BADGE_CSS);

        // The badge sits past the button's edges. Today has-entry-tip keeps the button from clipping it (its overflow:
        // visible beats interactive-btn's hidden); the inline value is insurance against an update dropping that class.
        setStyle(button, 'overflow', badge.waiting ? 'visible' : '');

        return true;
    }

    // False only when texts wait and the phone's home screen is showing without its messages app.
    function showAppBadge(phoneDocument, badge) {
        var app = phoneApp(phoneDocument);

        if (!app) return !(badge.waiting && phoneDocument.querySelector(PHONE_DOCK));

        placeBadge(phoneDocument, app, badge, APP_BADGE_CSS);

        return true;
    }

    // The game builds the messages app once with each phone page and keeps it while the page lives, so the app found
    // first is kept on the page and the page is searched again only once that app has left it.
    function phoneApp(doc) {
        var app = doc.__sleventlistApp;

        if (app && app.isConnected === true) return app;

        app = doc.querySelector(PHONE_APP);
        doc.__sleventlistApp = app;

        return app;
    }

    // The game builds a new phone page every time the phone opens and shows it two frames after the page starts, long
    // before the plugin's next call. So Root.html's frames are watched from here: a phone page is found the moment its
    // frame appears, and the last badge the plugin sent goes on the messages app while the game builds the page. The
    // watchers are made once per page and call whatever this page's script is now, so a reloaded HUD never draws an
    // old count.
    function watchPhones() {
        var root = rootDocument();

        if (!root) return;

        root.__sleventlistFrame = onFrame;
        root.__sleventlistPhone = drawPhone;

        var Observer = root.defaultView && root.defaultView.MutationObserver;

        if (root.__sleventlistWatching || !Observer) return;

        root.__sleventlistWatching = true;
        new Observer(function (records) {
            for (var i = 0; i < records.length; i++) {
                var record = records[i];

                if (record.type === 'attributes') {
                    if (record.target.tagName === 'IFRAME') root.__sleventlistFrame(record.target);

                    continue;
                }

                for (var j = 0; j < record.addedNodes.length; j++) {
                    var node = record.addedNodes[j];

                    if (node.nodeType !== 1) continue;

                    if (node.tagName === 'IFRAME') {
                        root.__sleventlistFrame(node);
                        continue;
                    }

                    var inner = node.getElementsByTagName('iframe');

                    for (var k = 0; k < inner.length; k++) root.__sleventlistFrame(inner[k]);
                }
            }
        }).observe(root, { childList: true, subtree: true, attributes: true, attributeFilter: ['src'] });
    }

    function rootDocument() {
        try {
            var parent = window.parent;

            return parent && parent !== window && parent.document ? parent.document : null;
        } catch (e) {
            return null;
        }
    }

    function onFrame(frame) {
        if (!PHONE_PAGE.test(String(frame.src || ''))) return;

        if (!frame.__sleventlistFrame) {
            frame.__sleventlistFrame = true;
            frame.addEventListener('load', function () { chasePhone(frame); });
        }

        chasePhone(frame);
    }

    // A new frame first holds an empty page; the phone page replaces it a moment later. Look each frame until it is
    // there, for about two seconds at most. A frame has one look at a time: starting a new one ends the one before.
    function chasePhone(frame) {
        var chase = frame.__sleventlistChase = (frame.__sleventlistChase || 0) + 1;
        var framesLeft = CHASE_FRAMES;

        step();

        function step() {
            if (frame.__sleventlistChase !== chase || watchPhone(frame) || framesLeft-- <= 0) return;

            var root = rootDocument();
            var later = root && root.defaultView && root.defaultView.requestAnimationFrame;

            if (typeof later !== 'function') return;

            later.call(root.defaultView, step);
        }
    }

    function watchPhone(frame) {
        try {
            var win = frame.contentWindow;
            var doc = win && win.document;

            if (!doc || !PHONE_PAGE.test(String(win.location))) return false;

            var root = rootDocument();

            if (!doc.__sleventlistWatched && root && typeof win.MutationObserver !== 'undefined') {
                doc.__sleventlistWatched = true;
                new win.MutationObserver(function () { root.__sleventlistPhone(doc); }).observe(doc, { childList: true, subtree: true });
            }

            drawPhone(doc);

            return true;
        } catch (e) {
            // A page from another origin, or one being torn down: the plugin's next call tries again.
            return true;
        }
    }

    // Runs on every change to the phone page, so while the messages app is in place it costs a check that the app is
    // still on the page and a look at the app's own few children. Only adds or fixes the badge; placeBadge writes
    // nothing when it is already right.
    function drawPhone(doc) {
        if (!lastBadge) return;

        try {
            var app = phoneApp(doc);

            if (app) placeBadge(doc, app, lastBadge, APP_BADGE_CSS);
        } catch (e) {}
    }

    // The button found last is kept while it is on the page and still draws the phone icon; the HUD is searched only
    // once it is not.
    var hudButton = null;

    function hudPhoneButton() {
        if (isPhoneButton(hudButton)) return hudButton;

        var icons = document.querySelectorAll(HUD_ICONS);

        hudButton = null;

        for (var i = 0; i < icons.length; i++) {
            if (isPhoneIcon(icons[i])) {
                hudButton = icons[i].parentElement;
                break;
            }
        }

        return hudButton;
    }

    function isPhoneButton(button) {
        if (!button || button.isConnected !== true || !button.classList.contains('icon-sq')) return false;

        var icon = firstChildWithClass(button, 'icon-svg');

        return !!icon && isPhoneIcon(icon);
    }

    function isPhoneIcon(icon) {
        var path = icon.children[0];

        return !!path && String(path.getAttribute('d') || '').indexOf(PHONE_ICON) === 0;
    }

    function isPhoneButtonUp() {
        var ss = rootSetupState();

        return !!(ss && ss.state && ss.state.phoneActive);
    }

    // Draws the badge as the last child of host, or takes it away when nothing waits. Writes only what changed.
    function placeBadge(doc, host, badge, css) {
        var el = firstChildWithClass(host, BADGE_CLASS);

        if (!badge.waiting) {
            if (el) host.removeChild(el);

            return;
        }

        addBadgeStyle(doc, css);

        if (!el) {
            el = host.appendChild(doc.createElement('span'));
            el.classList.add(BADGE_CLASS);
        }

        var label = badgeLabel(badge);

        setClass(el, TIMED_CLASS, badge.timed);

        if (el.textContent !== label) el.textContent = label;
    }

    function addBadgeStyle(doc, css) {
        if (doc.getElementById(BADGE_STYLE_ID)) return;

        var style = doc.createElement('style');
        style.id = BADGE_STYLE_ID;
        style.textContent = css;

        (doc.head || doc.documentElement).appendChild(style);
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

    function setClass(el, name, isOn) {
        if (el.classList.contains(name) === isOn) return;

        if (isOn) el.classList.add(name);
        else el.classList.remove(name);
    }

    // An empty value removes the inline property, leaving the page's stylesheet in charge again.
    function setStyle(el, name, value) {
        if (el.style.getPropertyValue(name) === value) return;

        if (value) el.style.setProperty(name, value);
        else el.style.removeProperty(name);
    }

    function childrenWithClass(parent, className) {
        var found = [];

        for (var i = 0; i < parent.children.length; i++) {
            if (parent.children[i].classList.contains(className)) found.push(parent.children[i]);
        }

        return found;
    }

    function firstChildWithClass(parent, className) {
        for (var i = 0; i < parent.children.length; i++) {
            if (parent.children[i].classList.contains(className)) return parent.children[i];
        }

        return null;
    }

    function describe(e) {
        return String(e && e.message ? e.message : e);
    }

    watchPhones();

    // apply is the plugin's way in. rules are the parts that decide what the list does, kept here for the tests; the
    // page never calls them.
    window.__sleventlist = {
        version: '0.3.1',
        apply: apply,
        rules: { readSettings: readSettings, tierOf: tierOf, rankByTimeLeft: rankByTimeLeft, crisisIds: crisisIds, routeClicks: routeClicks, readBadge: readBadge, badgeLabel: badgeLabel }
    };
})();
