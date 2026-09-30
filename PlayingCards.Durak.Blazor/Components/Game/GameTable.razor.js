let dnet = null;
let root = null;
let drag = null;

const THRESHOLD = 6;

let animToken = 0;
let animLayer = null;
const ANIM_MAX_CLONES = 14;

let handRects = new Map();
let freshSlots = [];
let dealDur = 0;
let handObserver = null;
let armedPreFly = new WeakSet();

function dealDuration() {
    if (!dealDur) {
        const raw = getComputedStyle(document.documentElement).getPropertyValue('--deal');
        const v = parseFloat(raw);
        dealDur = raw.trim().endsWith('ms') ? v : v * 1000;

        if (!dealDur || Number.isNaN(dealDur)) {
            dealDur = 380;
        }
    }

    return dealDur;
}

export function init(rootEl, dotNetRef) {
    root = rootEl;
    dnet = dotNetRef;
    root.addEventListener('pointerdown', onPointerDown);

    // MutationObserver ловит любую вставку карты (рука/поле) синхронно, микротаском сразу после
    // DOM-патча Blazor, до пейнта. afterRender() же приходит только после RTT сервер-клиент
    // и потому не успевает измерить/спрятать карту до кадра.
    handObserver = new MutationObserver(onBoardMutated);
    handObserver.observe(root, { childList: true, subtree: true });
    onBoardMutated();
}

function onBoardMutated() {
    syncHand();
    armPreFly();
}

export function dispose() {
    if (root) {
        root.removeEventListener('pointerdown', onPointerDown);
    }

    if (handObserver) {
        handObserver.disconnect();
        handObserver = null;
    }

    detachWindow();
    cleanup();
    clearAnim();
    handRects = new Map();
    freshSlots = [];
    armedPreFly = new WeakSet();
    root = null;
    dnet = null;
}

function onPointerDown(e) {
    if (e.pointerType === 'mouse' && e.button !== 0) {
        return;
    }

    const slot = e.target.closest('.hand-slot[data-playable="true"]');
    if (!slot || drag) {
        return;
    }

    const card = slot.querySelector('.play-card');
    if (!card) {
        return;
    }

    drag = {
        pointerId: e.pointerId,
        card,
        cardKey: slot.dataset.cardKey,
        startX: e.clientX,
        startY: e.clientY,
        px: e.clientX,
        py: e.clientY,
        prevPx: e.clientX,
        prevPy: e.clientY,
        vx: 0,
        vy: 0,
        curX: 0,
        curY: 0,
        rz: { v: 0, vel: 0 },
        rx: { v: 0, vel: 0 },
        ry: { v: 0, vel: 0 },
        sc: { v: 0.86, vel: 0 },
        raf: 0,
        lastT: 0,
        originRect: card.getBoundingClientRect(),
        avatar: null,
        moved: false,
        targets: null,
        mode: 'none',
        hoverZone: null,
    };

    window.addEventListener('pointermove', onPointerMove);
    window.addEventListener('pointerup', onPointerUp);
    window.addEventListener('pointercancel', onPointerUp);
}

function onPointerMove(e) {
    if (!drag || e.pointerId !== drag.pointerId) {
        return;
    }

    drag.px = e.clientX;
    drag.py = e.clientY;

    if (!drag.moved) {
        if (Math.hypot(e.clientX - drag.startX, e.clientY - drag.startY) < THRESHOLD) {
            return;
        }

        startDragging();
    }

    e.preventDefault();
    updateHover(e.clientX, e.clientY);
}

async function onPointerUp(e) {
    if (!drag || e.pointerId !== drag.pointerId) {
        return;
    }

    detachWindow();

    if (drag.raf) {
        cancelAnimationFrame(drag.raf);
        drag.raf = 0;
    }

    if (!drag.moved) {
        drag = null;
        return;
    }

    suppressNextClick();

    const zone = drag.hoverZone;
    const attackKey = drag.mode === 'defence' && zone ? zone.dataset.cardKey : null;

    if (zone) {
        let ok = false;

        try {
            ok = await dnet.invokeMethodAsync('OnCardDropped', drag.cardKey, attackKey);
        } catch {
            ok = false;
        }

        if (ok) {
            await snapToZone(zone);
            cleanup();
            return;
        }
    }

    await snapBack();
    cleanup();
}

function startDragging() {
    drag.moved = true;

    const a = drag.card.cloneNode(true);
    a.classList.add('card-drag-avatar');
    a.classList.remove('active', 'dimmed');

    const r = drag.originRect;
    a.style.position = 'fixed';
    a.style.margin = '0';
    a.style.left = `${r.left}px`;
    a.style.top = `${r.top}px`;
    a.style.width = `${r.width}px`;
    a.style.height = `${r.height}px`;
    a.style.transition = 'box-shadow .16s ease';

    document.body.appendChild(a);
    drag.avatar = a;
    drag.card.classList.add('dnd-ghost');
    document.body.classList.add('dnd-dragging');

    drag.prevPx = drag.px;
    drag.prevPy = drag.py;
    drag.lastT = 0;
    drag.raf = requestAnimationFrame(tick);

    dnet.invokeMethodAsync('GetDropTargets', drag.cardKey).then(t => {
        if (!drag) {
            return;
        }

        drag.targets = t;
        drag.mode = t.field ? 'attack' : (t.defence && t.defence.length ? 'defence' : 'none');
        armZones();
    });
}

function armZones() {
    if (!drag || !drag.targets || !root) {
        return;
    }

    if (drag.mode === 'attack') {
        const field = root.querySelector('.field[data-drop="field"]');
        if (field) {
            field.classList.add('drop-ok');
        }
    } else if (drag.mode === 'defence') {
        for (const i of drag.targets.defence) {
            const zone = root.querySelector(`.field-card[data-field-index="${i}"]`);
            if (zone) {
                zone.classList.add('drop-ok');
            }
        }
    }
}

function updateHover(x, y) {
    if (!drag) {
        return;
    }

    let zone = null;
    const el = document.elementFromPoint(x, y);

    if (el) {
        if (drag.mode === 'attack') {
            const field = el.closest('.field[data-drop="field"]');
            if (field && field.classList.contains('drop-ok')) {
                zone = field;
            }
        } else if (drag.mode === 'defence') {
            const card = el.closest('.field-card[data-field-index]');
            if (card && card.classList.contains('drop-ok')) {
                zone = card;
            }
        }
    }

    if (zone !== drag.hoverZone) {
        if (drag.hoverZone) {
            drag.hoverZone.classList.remove('drop-hover');
        }

        if (zone) {
            zone.classList.add('drop-hover');
        }

        drag.hoverZone = zone;

        if (drag.avatar) {
            drag.avatar.classList.toggle('over-target', !!zone);
        }
    }
}

function snapToZone(zone) {
    return new Promise(resolve => {
        const a = drag.avatar;
        if (!a) {
            resolve();
            return;
        }

        const zr = zone.getBoundingClientRect();
        const o = drag.originRect;
        const tx = (zr.left + zr.width / 2) - (o.left + o.width / 2);
        const ty = (zr.top + zr.height / 2) - (o.top + o.height / 2);

        a.style.transition = 'transform .22s cubic-bezier(.2,.75,.25,1), opacity .22s ease';
        a.style.transform = `translate(${tx}px, ${ty}px) rotate(0deg) scale(.62)`;
        a.style.opacity = '0';
        sparkle(zr.left + zr.width / 2, zr.top + zr.height / 2);
        setTimeout(resolve, 230);
    });
}

function snapBack() {
    return new Promise(resolve => {
        const a = drag.avatar;
        if (!a) {
            resolve();
            return;
        }

        a.style.transition = 'transform .34s cubic-bezier(.34,1.56,.64,1)';
        a.style.transform = 'translate(0px, 0px) rotate(0deg) scale(1)';
        setTimeout(resolve, 350);
    });
}

function tick(t) {
    if (!drag || !drag.avatar) {
        return;
    }

    const f = drag.lastT ? Math.min((t - drag.lastT) / 16.67, 2.4) : 1;
    drag.lastT = t;

    const rawVx = drag.px - drag.prevPx;
    const rawVy = drag.py - drag.prevPy;
    drag.prevPx = drag.px;
    drag.prevPy = drag.py;
    drag.vx += (rawVx - drag.vx) * 0.35;
    drag.vy += (rawVy - drag.vy) * 0.35;

    const targetX = drag.px - drag.startX;
    const targetY = drag.py - drag.startY;
    drag.curX += (targetX - drag.curX) * Math.min(1, 0.55 * f);
    drag.curY += (targetY - drag.curY) * Math.min(1, 0.55 * f);

    spring(drag.rz, clamp(drag.vx * 0.55, -16, 16), 0.10, 0.55, f);
    spring(drag.ry, clamp(drag.vx * 0.75, -22, 22), 0.12, 0.55, f);
    spring(drag.rx, clamp(-drag.vy * 0.75, -22, 22), 0.12, 0.55, f);
    spring(drag.sc, drag.hoverZone ? 1.13 : 1.08, 0.16, 0.6, f);

    const calm = 1 - Math.min(1, (Math.abs(drag.vx) + Math.abs(drag.vy)) / 7);
    const idleRot = Math.sin(t * 0.005) * 1.3 * calm;
    const idleBob = Math.sin(t * 0.005 + 1) * 1.6 * calm;

    drag.avatar.style.transform =
        `translate(${drag.curX}px, ${drag.curY + idleBob}px) `
        + `perspective(820px) `
        + `rotateX(${drag.rx.v}deg) rotateY(${drag.ry.v}deg) `
        + `rotateZ(${drag.rz.v + idleRot}deg) scale(${drag.sc.v})`;

    drag.raf = requestAnimationFrame(tick);
}

function spring(s, target, stiffness, damping, f) {
    const acc = stiffness * (target - s.v) - damping * s.vel;
    s.vel += acc * f;
    s.v += s.vel * f;
}

function cleanup() {
    if (!drag) {
        return;
    }

    if (drag.raf) {
        cancelAnimationFrame(drag.raf);
        drag.raf = 0;
    }

    if (drag.avatar) {
        drag.avatar.remove();
    }

    if (drag.card) {
        drag.card.classList.remove('dnd-ghost');
    }

    clearZones();
    document.body.classList.remove('dnd-dragging');
    drag = null;
}

function clearZones() {
    if (!root) {
        return;
    }

    root.querySelectorAll('.drop-ok, .drop-hover')
        .forEach(z => z.classList.remove('drop-ok', 'drop-hover'));
}

function detachWindow() {
    window.removeEventListener('pointermove', onPointerMove);
    window.removeEventListener('pointerup', onPointerUp);
    window.removeEventListener('pointercancel', onPointerUp);
}

function suppressNextClick() {
    const handler = ev => {
        ev.stopImmediatePropagation();
        ev.preventDefault();
        window.removeEventListener('click', handler, true);
    };

    window.addEventListener('click', handler, true);
    setTimeout(() => window.removeEventListener('click', handler, true), 350);
}

function sparkle(x, y) {
    const layer = document.createElement('div');
    layer.className = 'dnd-sparkle-layer';
    layer.style.left = `${x}px`;
    layer.style.top = `${y}px`;

    for (let i = 0; i < 9; i++) {
        const s = document.createElement('span');
        const ang = (Math.PI * 2 * i) / 9;
        const dist = 38 + (i % 3) * 7;
        s.style.setProperty('--tx', `${Math.cos(ang) * dist}px`);
        s.style.setProperty('--ty', `${Math.sin(ang) * dist}px`);
        s.style.animationDelay = `${(i % 3) * 25}ms`;
        layer.appendChild(s);
    }

    const ring = document.createElement('div');
    ring.className = 'dnd-pulse-ring';
    layer.appendChild(ring);

    document.body.appendChild(layer);
    setTimeout(() => layer.remove(), 700);
}

function clamp(v, lo, hi) {
    return v < lo ? lo : (v > hi ? hi : v);
}

export function afterRender(diff) {
    if (!root) {
        return;
    }

    if (diff && !prefersReducedMotion()) {
        animate(diff);
    }
}

function syncHand() {
    const slots = root.querySelectorAll('.hand-slot[data-card-key]');
    const next = new Map();
    const fresh = [];
    const reduced = prefersReducedMotion();
    const prevEmpty = handRects.size === 0;
    const hand = root.querySelector('.hand');
    const scroll = hand ? hand.scrollLeft : 0;

    for (const slot of slots) {
        const key = slot.dataset.cardKey;
        const rect = slot.getBoundingClientRect();
        next.set(key, { left: rect.left + scroll, top: rect.top });

        const prev = handRects.get(key);

        if (!prev) {
            fresh.push(slot);
            continue;
        }

        const dx = prev.left - (rect.left + scroll);
        const dy = prev.top - rect.top;

        if (!reduced && (Math.abs(dx) > 2 || Math.abs(dy) > 2)) {
            slot.animate(
                [
                    { transform: `translate(${dx}px, ${dy}px)` },
                    { transform: 'translate(0, 0)' },
                ],
                { duration: dealDuration() * 0.7, easing: 'cubic-bezier(0.2, 0.75, 0.25, 1)' });
        }
    }

    handRects = next;
    freshSlots = prevEmpty ? [] : fresh;
}

// Единая точка страховки для ЛЮБОЙ карты, помеченной сервером классом pre-fly (рука/поле,
// visibility:hidden в CardView.razor.css) — чтобы не мелькнуть в финальной позиции до прилёта
// клона (flyCardOnto → land() снимает класс раньше срока). Если анимация не случилась (budget
// ANIM_MAX_CLONES исчерпан, обрыв связи, reduced-motion) — снимаем сами по таймауту, иначе
// карта останется невидимой навсегда. armedPreFly не даёт переставить таймер повторно на
// том же элементе при последующих мутациях.
function armPreFly() {
    const reduced = prefersReducedMotion();

    for (const card of root.querySelectorAll('.play-card.pre-fly')) {
        if (armedPreFly.has(card)) {
            continue;
        }

        armedPreFly.add(card);

        if (reduced) {
            releasePreFly(card);
            continue;
        }

        setTimeout(() => releasePreFly(card), dealDuration() + 250);
    }
}

function releasePreFly(card) {
    card.classList.remove('pre-fly');
    card.style.visibility = '';
}

function animate(diff) {
    clearAnim();
    const token = ++animToken;
    const layer = document.createElement('div');
    layer.className = 'board-anim-layer';
    document.body.appendChild(layer);
    animLayer = layer;

    let budget = ANIM_MAX_CLONES;

    if (budget > 0 && diff.beatCards && diff.beatCards.length) {
        const from = rectOf(root.querySelector('.field[data-drop="field"]'));
        const to = rectOf(root.querySelector('[data-discard-anchor]'));

        if (from && to) {
            budget = flySweep(layer, token, diff.beatCards, from, to, budget, true);
        }
    }

    if (budget > 0 && diff.takeCards && diff.takeCards.length && diff.takeTarget >= 0) {
        const from = rectOf(root.querySelector('.field[data-drop="field"]'));
        const to = rectOf(badge(diff.takeTarget));

        if (from && to) {
            budget = flySweep(layer, token, diff.takeCards, from, to, budget, false);
        }
    }

    if (diff.throwIns && diff.throwIns.length) {
        let k = 0;

        for (const t of diff.throwIns) {
            if (budget <= 0) {
                break;
            }

            const slot = root.querySelector(`.field-card[data-field-index="${t.fieldIndex}"]`);
            const from = rectOf(badge(t.throwerIndex));

            if (slot && from) {
                flyThrowIn(layer, token, slot, from, k * 70);
                budget--;
                k++;
            }
        }
    }

    if (budget > 0 && diff.covers && diff.covers.length) {
        let k = 0;

        for (const c of diff.covers) {
            if (budget <= 0) {
                break;
            }

            const slot = root.querySelector(`.field-card[data-field-index="${c.fieldIndex}"]`);
            const from = rectOf(badge(c.defenderIndex));

            if (slot && from) {
                flyCover(layer, token, slot, from, k * 70);
                budget--;
                k++;
            }
        }
    }

    if (budget > 0 && diff.draws && diff.draws.length) {
        const deck = rectOf(root.querySelector('[data-deck-anchor]'));

        if (deck) {
            budget = flyDraws(layer, token, diff.draws, deck, budget);
        }
    }

    if (!layer.childElementCount) {
        clearAnim();
    }
}

function flySweep(layer, token, cards, from, to, budget, faceDown) {
    const n = Math.min(cards.length, budget);

    for (let i = 0; i < n; i++) {
        const node = faceDown ? backCard() : faceCard(cards[i].rank, cards[i].suit);
        const jx = (i - (n - 1) / 2) * 16;
        const spin = (i % 2 ? 1 : -1) * (8 + (i % 3) * 4);
        flyBetween(layer, token, node, from.x + jx, from.y, to.x, to.y, i * 45, true, spin);
    }

    return budget - n;
}

function flyDraws(layer, token, draws, deck, budget) {
    for (const d of draws) {
        if (budget <= 0) {
            break;
        }

        if (d.toType === 'hand') {
            budget = flyDrawsToHand(layer, token, d.count, deck, budget);
            continue;
        }

        const to = rectOf(badge(d.badgeIndex));

        if (!to) {
            continue;
        }

        const n = Math.min(d.count, budget);

        for (let i = 0; i < n; i++) {
            flyBetween(layer, token, backCard(), deck.x, deck.y, to.x, to.y, i * 80, false, (i % 2 ? -6 : 6));
            budget--;
        }
    }

    return budget;
}

function flyDrawsToHand(layer, token, count, deck, budget) {
    const slots = freshSlots.slice(0, count);
    let k = 0;

    for (const slot of slots) {
        if (budget <= 0) {
            break;
        }

        const card = slot.querySelector('.play-card');

        if (!card) {
            continue;
        }

        flyCardOnto(layer, token, card, deck, k * 80, 0);
        budget--;
        k++;
    }

    return budget;
}

function flyBetween(layer, token, node, fromX, fromY, toX, toY, delay, fadeOut, spin = 0) {
    node.style.position = 'fixed';
    node.style.left = '0';
    node.style.top = '0';
    node.style.margin = '0';
    node.style.pointerEvents = 'none';
    node.style.opacity = fadeOut ? '1' : '0';
    node.style.transform = `translate(${fromX}px, ${fromY}px) translate(-50%, -50%) rotate(${spin}deg) scale(0.82)`;
    node.style.willChange = 'transform, opacity';
    layer.appendChild(node);

    const start = performance.now() + delay;
    const dur = dealDuration();
    const arc = Math.min(70, Math.hypot(toX - fromX, toY - fromY) * 0.16);

    function step(now) {
        if (token !== animToken) {
            return;
        }

        const t = (now - start) / dur;

        if (t < 0) {
            requestAnimationFrame(step);
            return;
        }

        const p = t >= 1 ? 1 : ease(t);
        const pop = t >= 1 ? 1 : Math.min(1, easeOutBack(t));
        const lift = t >= 1 ? 0 : Math.sin(Math.PI * p) * arc;
        const x = fromX + (toX - fromX) * p;
        const y = fromY + (toY - fromY) * p - lift;
        const scale = 0.82 + 0.18 * pop;
        const rot = spin * (1 - p);
        node.style.transform = `translate(${x}px, ${y}px) translate(-50%, -50%) rotate(${rot}deg) scale(${scale})`;

        if (fadeOut) {
            node.style.opacity = t > 0.7 ? String(Math.max(0, 1 - (t - 0.7) / 0.3)) : '1';
        } else {
            node.style.opacity = String(Math.min(1, t * 2));
        }

        if (t < 1) {
            requestAnimationFrame(step);
        } else if (fadeOut) {
            node.remove();
        } else {
            node.style.transition = 'opacity .14s ease';
            node.style.opacity = '0';
            setTimeout(() => node.remove(), 160);
        }
    }

    requestAnimationFrame(step);
}

function flyCardOnto(layer, token, cardEl, from, delay, endRot) {
    if (!cardEl) {
        return;
    }

    const r = cardEl.getBoundingClientRect();

    if (r.width === 0 && r.height === 0) {
        return;
    }

    const w = cardEl.offsetWidth || r.width;
    const h = cardEl.offsetHeight || r.height;
    const cx = r.left + r.width / 2;
    const cy = r.top + r.height / 2;

    const clone = cardEl.cloneNode(true);
    clone.classList.remove('active', 'dimmed', 'dnd-ghost', 'pre-fly');
    clone.style.visibility = 'visible';
    clone.style.position = 'fixed';
    clone.style.margin = '0';
    clone.style.left = `${cx - w / 2}px`;
    clone.style.top = `${cy - h / 2}px`;
    clone.style.width = `${w}px`;
    clone.style.height = `${h}px`;
    clone.style.pointerEvents = 'none';
    clone.style.willChange = 'transform';
    clone.style.transformOrigin = 'center';
    clone.style.transition = 'none';

    const dx = from.x - cx;
    const dy = from.y - cy;
    const rot0 = clamp(-dx * 0.04, -14, 14);

    clone.style.transform = `translate(${dx}px, ${dy}px) rotate(${rot0}deg) scale(0.6)`;
    layer.appendChild(clone);

    cardEl.style.visibility = 'hidden';

    const start = performance.now() + delay;
    const dur = dealDuration();
    const arc = Math.min(70, Math.hypot(dx, dy) * 0.16);
    let done = false;

    function land() {
        if (done) {
            return;
        }

        done = true;
        releasePreFly(cardEl);
        clone.remove();
    }

    function step(now) {
        if (token !== animToken) {
            land();
            return;
        }

        const t = (now - start) / dur;

        if (t < 0) {
            requestAnimationFrame(step);
            return;
        }

        const p = t >= 1 ? 1 : ease(t);
        const pop = t >= 1 ? 1 : easeOutBack(t);
        const lift = t >= 1 ? 0 : Math.sin(Math.PI * p) * arc;
        const x = dx * (1 - p);
        const y = dy * (1 - p) - lift;
        const scale = 0.6 + 0.4 * pop;
        const rot = rot0 * (1 - p) + endRot * p;
        clone.style.transform = `translate(${x}px, ${y}px) rotate(${rot}deg) scale(${scale})`;

        if (t < 1) {
            requestAnimationFrame(step);
        } else {
            land();
        }
    }

    requestAnimationFrame(step);
}

function flyThrowIn(layer, token, slot, from, delay) {
    flyCardOnto(layer, token, slot.querySelector('.attack-card'), from, delay, 0);
}

function flyCover(layer, token, slot, from, delay) {
    flyCardOnto(layer, token, slot.querySelector('.defence-card'), from, delay, 7);
}

function clearAnim() {
    animToken++;

    if (animLayer) {
        animLayer.remove();
        animLayer = null;
    }
}

const reducedMotionMql = window.matchMedia
    ? window.matchMedia('(prefers-reduced-motion: reduce)')
    : null;

function prefersReducedMotion() {
    return !!reducedMotionMql && reducedMotionMql.matches;
}

function rectOf(el) {
    if (!el) {
        return null;
    }

    const r = el.getBoundingClientRect();

    if (r.width === 0 && r.height === 0) {
        return null;
    }

    return { x: r.left + r.width / 2, y: r.top + r.height / 2 };
}

function badge(gameIndex) {
    return root.querySelector(`.player[data-player-index="${gameIndex}"]`);
}

function faceCard(rank, suit) {
    const el = document.createElement('div');
    el.className = 'anim-card anim-card-face';

    const red = suit === 1 || suit === 2;
    if (red) {
        el.classList.add('red');
    }

    el.textContent = `${rankText(rank)}${suitText(suit)}`;
    return el;
}

function backCard() {
    const el = document.createElement('div');
    el.className = 'anim-card anim-card-back';
    return el;
}

function rankText(rank) {
    switch (rank) {
        case 11: return 'J';
        case 12: return 'Q';
        case 13: return 'K';
        case 14: return 'A';
        default: return String(rank);
    }
}

function suitText(suit) {
    switch (suit) {
        case 0: return '♣';
        case 1: return '♦';
        case 2: return '♥';
        case 3: return '♠';
        default: return '?';
    }
}

function ease(t) {
    return t < 0.5 ? 2 * t * t : 1 - Math.pow(-2 * t + 2, 2) / 2;
}

function easeOutBack(t) {
    const c1 = 1.70158;
    const c3 = c1 + 1;
    return 1 + c3 * Math.pow(t - 1, 3) + c1 * Math.pow(t - 1, 2);
}
