// Loaded after FakePage.js for the tests of a phone that opens: this page sits in a Root.html of its own, whose frames
// come and go the way Root makes them, elements report what changes to mutation observers, and animation frames run
// by hand. Observers are called only by flush(), the way a browser calls them before it paints. Test code only.
var observers = [];
var pending = [];
var frameCallbacks = [];

function requestAnimationFrame(callback) {
    frameCallbacks.push(callback);

    return frameCallbacks.length;
}

// One animation frame: the callbacks asked for before it, then the observers.
function nextFrame() {
    var due = frameCallbacks;

    frameCallbacks = [];
    due.forEach(function (callback) { callback(); });
    flush();
}

function MutationObserver(callback) {
    this.callback = callback;
    this.targets = [];
    this.records = [];
}

MutationObserver.prototype.observe = function (target, options) {
    this.targets.push({ node: target, options: options });

    if (observers.indexOf(this) < 0) observers.push(this);
};

// A document holds an element when its root element is above it.
function isInside(node, ancestor) {
    var top = node;

    for (var at = node; at; at = at.parentElement) {
        if (at === ancestor) return true;

        top = at;
    }

    return !!ancestor.documentElement && top === ancestor.documentElement;
}

function report(record) {
    observers.forEach(function (observer) {
        var hears = observer.targets.some(function (t) {
            if (record.type === 'attributes' && !t.options.attributes) return false;
            if (record.type === 'childList' && !t.options.childList) return false;
            if (record.type === 'attributes' && t.options.attributeFilter && t.options.attributeFilter.indexOf(record.attributeName) < 0) return false;

            return t.node === record.target || (t.options.subtree && isInside(record.target, t.node));
        });

        if (!hears) return;

        observer.records.push(record);

        if (pending.indexOf(observer) < 0) pending.push(observer);
    });
}

// Calls the observers with what changed, again while they change things. An observer that keeps waking itself fails
// the test instead of hanging it.
function flush() {
    for (var round = 0; pending.length > 0; round++) {
        if (round > 50) throw new Error('an observer keeps waking itself');

        var due = pending;

        pending = [];
        due.forEach(function (observer) {
            var records = observer.records;

            observer.records = [];
            observer.callback(records);
        });
    }
}

var plainAppend = FakeElement.prototype.appendChild;
var plainRemove = FakeElement.prototype.removeChild;

FakeElement.prototype.nodeType = 1;

FakeElement.prototype.appendChild = function (child) {
    plainAppend.call(this, child);
    report({ type: 'childList', target: this, addedNodes: [child] });

    return child;
};

FakeElement.prototype.removeChild = function (child) {
    plainRemove.call(this, child);
    report({ type: 'childList', target: this, addedNodes: [] });

    return child;
};

FakeElement.prototype.remove = function () {
    if (this.parentElement) this.parentElement.removeChild(this);
};

FakeElement.prototype.addEventListener = function (type, listener) {
    this.listeners = this.listeners || {};
    (this.listeners[type] = this.listeners[type] || []).push(listener);
};

FakeElement.prototype.getElementsByTagName = function (tag) {
    return descendantsOf(this).filter(function (element) { return String(element.tagName).toUpperCase() === String(tag).toUpperCase(); });
};

// Root.html: its own document, holding the frames, with this page's window inside it.
var rootDoc = fakeDocument();

rootDoc.defaultView = { MutationObserver: MutationObserver, requestAnimationFrame: requestAnimationFrame };
window.parent = { document: rootDoc };

var PHONE_URL = 'https://ui.local/UI/PhoneSMS/PhoneSMS.html';

// The phone the tests read, once a phone page has arrived: { document, smsApp }.
var phone = null;

function frameWindow(location, doc) {
    return { location: location, document: doc, MutationObserver: MutationObserver };
}

// A game page frame, as Root.html makes one: appended with its address, still holding an empty page.
function openFrame(url) {
    var frame = new FakeElement('IFRAME');

    frame.src = url;
    frame.contentWindow = frameWindow('about:blank', fakeDocument());
    rootDoc.body.appendChild(frame);

    return frame;
}

// The page arrives in the frame: a new page, its body still empty.
function arrive(frame) {
    frame.contentWindow = frameWindow(frame.src, fakeDocument());

    return frame.contentWindow.document;
}

// The game builds the phone's dock into the page, as buildPhonePage draws it.
function buildPhone(doc, spec) {
    var built = buildPhonePage(spec || {});

    built.document.body.children.slice().forEach(function (child) {
        built.document.body.removeChild(child);
        doc.body.appendChild(child);
    });
    phone = { document: doc, smsApp: built.smsApp };

    return doc;
}

function fireLoad(frame) {
    ((frame.listeners || {}).load || []).slice().forEach(function (listener) { listener({ type: 'load' }); });
}

// Root.html sending a frame it already has to a new address: the old page goes, an empty one holds the frame.
function navigate(frame, url) {
    frame.src = url;
    frame.contentWindow = frameWindow('about:blank', fakeDocument());
    report({ type: 'attributes', target: frame, attributeName: 'src', addedNodes: [] });
}
