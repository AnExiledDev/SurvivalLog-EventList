// Just enough of the CoreUI1 page for eventlist.js to run in Jint: elements with classes, attributes and a parent,
// the few selectors the script uses, the three Vue calls it makes, the page's click-through, the events list and the
// HUD's phone button drawn the way the game's template draws them, and the phone's home screen from the PhoneSMS page.
// Test code only; nothing here ships.
var window = this;

var FakeElement = class {
    constructor(tagName, className) {
        var element = this;

        this.tagName = tagName;
        this.id = '';
        this.parentElement = null;
        this.children = [];
        this.textContent = '';
        this.scrollHeight = 0;
        this.clientHeight = 0;
        this.scrollTop = 0;
        this.attrs = {};
        this.classes = className ? className.split(' ') : [];
        this.styles = {};
        this.classList = {
            contains: function (name) { return element.classes.indexOf(name) >= 0; },
            add: function (name) { if (element.classes.indexOf(name) < 0) element.classes.push(name); },
            remove: function (name) { element.classes = element.classes.filter(function (c) { return c !== name; }); }
        };
        this.style = {
            getPropertyValue: function (name) { return element.styles[name] || ''; },
            setProperty: function (name, value) { element.styles[name] = value; },
            removeProperty: function (name) { delete element.styles[name]; }
        };
    }

    hasAttribute(name) { return Object.prototype.hasOwnProperty.call(this.attrs, name); }

    getAttribute(name) { return this.hasAttribute(name) ? this.attrs[name] : null; }

    setAttribute(name, value) { this.attrs[name] = String(value); }

    removeAttribute(name) { delete this.attrs[name]; }

    appendChild(child) {
        child.parentElement = this;
        this.children.push(child);

        return child;
    }

    removeChild(child) {
        var at = this.children.indexOf(child);

        if (at < 0) throw new Error('removeChild: not a child of this element');

        this.children.splice(at, 1);
        child.parentElement = null;

        return child;
    }

    // ".a > .b" is the child combinator: the last step names this element, each step before it the parent above.
    matches(selector) {
        var steps = selector.split('>').map(function (step) { return step.trim(); });
        var element = this;

        for (var i = steps.length - 1; i >= 0; i--) {
            if (!element || !matchesStep(element, steps[i])) return false;

            element = element.parentElement;
        }

        return true;
    }

    closest(selector) {
        for (var element = this; element; element = element.parentElement) {
            if (element.matches(selector)) return element;
        }

        return null;
    }

    querySelectorAll(selector) {
        return descendantsOf(this).filter(function (element) { return element.matches(selector); });
    }

    querySelector(selector) {
        return this.querySelectorAll(selector)[0] || null;
    }
};

function matchesStep(element, step) {
    if (step.charAt(0) === '.') return element.classList.contains(step.slice(1));
    if (step.charAt(0) === '[' && step.charAt(step.length - 1) === ']') return element.hasAttribute(step.slice(1, -1));

    throw new Error('the fake page does not know the selector "' + step + '"');
}

function descendantsOf(root) {
    var found = [];

    root.children.forEach(function walk(child) {
        found.push(child);
        child.children.forEach(walk);
    });

    return found;
}

var pointerListeners = {};

// A page's document. CoreUI1's is the global below; the phone's comes from buildPhonePage, as the phone is its own frame.
function fakeDocument() {
    var doc = {
        documentElement: new FakeElement('html'),
        getElementById: function (id) {
            return descendantsOf(doc.documentElement).filter(function (element) { return element.id === id; })[0] || null;
        },
        createElement: function (tagName) { return new FakeElement(tagName); },
        querySelector: function (selector) { return doc.documentElement.querySelector(selector); },
        querySelectorAll: function (selector) { return doc.documentElement.querySelectorAll(selector); }
    };

    doc.head = doc.documentElement.appendChild(new FakeElement('head'));
    doc.body = doc.documentElement.appendChild(new FakeElement('body'));

    return doc;
}

var document = fakeDocument();

document.addEventListener = function (type, listener) {
    (pointerListeners[type] = pointerListeners[type] || []).push(listener);
};
document.removeEventListener = function (type, listener) {
    pointerListeners[type] = (pointerListeners[type] || []).filter(function (l) { return l !== listener; });
};

// The browser fires mouseout on the element the pointer left, then mouseover on the one it entered. null is outside
// the page.
function movePointer(from, to) {
    if (from) firePointer('mouseout', from, to);
    if (to) firePointer('mouseover', to, from);
}

function firePointer(type, target, relatedTarget) {
    (pointerListeners[type] || []).slice().forEach(function (listener) {
        listener({ type: type, target: target, relatedTarget: relatedTarget || null });
    });
}

// A sync-flush watchEffect reruns the moment state it read is written; the page reads only the hover flag that way,
// so writes to that flag are the only ones that rerun effects here. watch callbacks are kept and never fired: the
// tests refresh the list through apply, the plugin's two-second check.
var Vue = {
    effects: [],
    watchers: [],
    watchEffect: function (run) {
        var effect = { run: run, isActive: true };

        Vue.effects.push(effect);
        run();

        return function () { effect.isActive = false; };
    },
    watch: function (sources, callback) {
        var watcher = { sources: sources, callback: callback, isActive: true };

        Vue.watchers.push(watcher);

        return function () { watcher.isActive = false; };
    },
    nextTick: function (callback) { callback(); },
    rerunEffects: function () {
        Vue.effects.filter(function (effect) { return effect.isActive; }).forEach(function (effect) { effect.run(); });
    }
};

var core = { clickThrough: { syncNowAfterReflow: function () {} } };

// Draws the events list as CoreUI1's template does: the panel, its scroll box, one group per list with a header, and
// a row per item with its text in .event-text. Rows take clicks through data-interactive unless the game is in its own
// scroll mode, where the box takes them through its unity-interactive class instead.
// Below the list sit the HUD's square buttons: the phone, while the game has it up, then the bag. The phone button is
// known only by its icon; with gameBadge it also shows the game's own red text badge. phoneButtonGone draws the game
// saying its phone is up with no button on the page, as a game update that redraws it could.
// spec: { groups: [[item, ...], ...], chips: [{ eventConfigId }], scrollMode, isOverflowing, phoneActive, gameBadge,
//         phoneButtonGone }
function buildPage(spec) {
    var isHovered = false;
    var groups = spec.groups.map(function (items) { return { items: items }; });
    var chips = spec.chips || [];
    var state = {
        phoneActive: !!spec.phoneActive,
        eventList: [].concat.apply([], spec.groups),
        eventListActive: true,
        eventStates: chips,
        eventStatesActive: chips.length > 0,
        towerWaveState: 0,
        towerWaveEventId: 0
    };

    Object.defineProperty(state, 'eventHover', {
        enumerable: true,
        get: function () { return isHovered; },
        set: function (value) {
            var isChanged = value !== isHovered;
            isHovered = value;

            if (isChanged) Vue.rerunEffects();
        }
    });

    var setupState = { groupedEventList: groups, eventScrollMode: !!spec.scrollMode, state: state };
    var app = document.body.appendChild(new FakeElement('div'));
    var panel = app.appendChild(new FakeElement('div', 'event-panel'));
    var box = panel.appendChild(new FakeElement('div', spec.scrollMode ? 'event-scroll unity-interactive' : 'event-scroll'));
    var rowsById = {};

    app.id = 'app';
    app._vnode = { component: { setupState: setupState } };

    groups.forEach(function (group) {
        var groupEl = box.appendChild(new FakeElement('div', 'event-group'));

        groupEl.appendChild(new FakeElement('div', 'event-group-header'));

        group.items.forEach(function (item) {
            var row = groupEl.appendChild(new FakeElement('div', 'event-item-row'));
            var text = row.appendChild(new FakeElement('span', 'event-text'));

            text.textContent = item.text;
            rowsById[item.eventConfigId] = row;

            if (!spec.scrollMode) row.setAttribute('data-interactive', '');
        });
    });

    box.clientHeight = 136;
    box.scrollHeight = spec.isOverflowing ? 300 : 136;

    var tools = app.appendChild(new FakeElement('div', 'vertical-tools'));
    var isPhoneDrawn = spec.phoneActive && !spec.phoneButtonGone;
    var phoneButton = isPhoneDrawn ? tools.appendChild(hudButton(PHONE_ICON_PATH, !!spec.gameBadge)) : null;

    tools.appendChild(hudButton(BAG_ICON_PATH, false));

    return {
        app: app,
        phoneButton: phoneButton,
        state: state,
        box: box,
        groupEls: box.children.slice(),
        rows: descendantsOf(box).filter(function (element) { return element.classList.contains('event-item-row'); }),
        row: function (id) { return rowsById[id]; },
        item: function (id) { return state.eventList.filter(function (item) { return item.eventConfigId === id; })[0]; }
    };
}

var PHONE_ICON_PATH = 'M17 1.01L7 1c-1.1 0-2 .9-2 2v18c0 1.1.9 2 2 2h10c1.1 0 2-.9 2-2V3c0-1.1-.9-1.99-2-1.99zM17 19H7V5h10v14z';
var BAG_ICON_PATH = 'M20 6h-4V4c0-1.11-.89-2-2-2h-4c-1.11 0-2 .89-2 2v2H4c-1.11 0-1.99.89-1.99 2L2 19c0 1.11.89 2 2 2h16c1.11 0 2-.89 2-2V8c0-1.11-.89-2-2-2z';

// A square HUD button as CoreUI1 draws it: its icon, the game's red badge when it has one, then its hover tip.
function hudButton(iconPath, hasGameBadge) {
    var button = new FakeElement('button', 'ui-btn icon-sq interactive-btn has-entry-tip');
    var svg = button.appendChild(new FakeElement('svg', 'icon-svg'));

    svg.appendChild(new FakeElement('path')).setAttribute('d', iconPath);

    if (hasGameBadge) button.appendChild(new FakeElement('span', 'phone-badge')).textContent = '2';

    button.appendChild(new FakeElement('div', 'entry-tip'));

    return button;
}

// The phone's home screen from PhoneSMS.html, in its own document: the dock with the messages app, its unread dot
// first when texts are unread, then the loan app. smsAppGone draws the dock without the messages app; homeHidden draws
// a phone screen with no dock, as when a conversation is open.
// spec: { unread, smsAppGone, homeHidden }
function buildPhonePage(spec) {
    var doc = fakeDocument();
    var dock = spec.homeHidden ? null : doc.body.appendChild(new FakeElement('div', 'app-dock'));
    var sms = dock && !spec.smsAppGone ? dock.appendChild(new FakeElement('div', 'app is-sms')) : null;

    if (sms && spec.unread) sms.appendChild(new FakeElement('div', 'unread-dot')).textContent = String(spec.unread);
    if (sms) sms.appendChild(new FakeElement('div', 'app-icon'));
    if (dock) dock.appendChild(new FakeElement('div', 'app is-loanshark'));

    return { document: doc, smsApp: sms };
}
