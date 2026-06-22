let dnet = null;
let root = null;
let drag = null;

const THRESHOLD = 6;

let animToken = 0;
let animLayer = null;
const ANIM_MAX_CLONES = 14;

export function init(rootEl, dotNetRef) {
    root = rootEl;
    dnet = dotNetRef;
    root.addEventListener('pointerdown', onPointerDown);
}

export function dispose() {
    if (root) {
        root.removeEventListener('pointerdown', onPointerDown);
    }

    detachWindow();
    cleanup();
    clearAnim();
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
        handIndex: parseInt(slot.dataset.handIndex, 10),
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
    const fieldIndex = zone
        ? (drag.mode === 'defence' ? parseInt(zone.dataset.fieldIndex, 10) : -1)
        : null;

    if (zone && fieldIndex !== null) {
        let ok = false;

        try {
            ok = await dnet.invokeMethodAsync('OnCardDropped', drag.handIndex, fieldIndex);
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

    dnet.invokeMethodAsync('GetDropTargets', drag.handIndex).then(t => {
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

export function animate(diff) {
    if (!diff || prefersReducedMotion() || !root) {
        return;
    }

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
        flyBetween(layer, token, node, from.x + jx, from.y, to.x, to.y, i * 45, true);
    }

    return budget - n;
}

function flyDraws(layer, token, draws, deck, budget) {
    for (const d of draws) {
        if (budget <= 0) {
            break;
        }

        const targets = drawTargets(d);
        const count = Math.min(d.count, targets.length || d.count);
        const n = Math.min(count, budget);

        for (let i = 0; i < n; i++) {
            const tr = targets[i] || targets[targets.length - 1] || null;

            if (!tr) {
                continue;
            }

            flyBetween(layer, token, backCard(), deck.x, deck.y, tr.x, tr.y, i * 80, false);
            budget--;
        }
    }

    return budget;
}

function drawTargets(d) {
    if (d.toType === 'hand') {
        const slots = [...root.querySelectorAll('.hand-slot')];
        const tail = slots.slice(Math.max(0, slots.length - d.count));
        return tail.map(rectOf).filter(Boolean);
    }

    const r = rectOf(badge(d.badgeIndex));
    return r ? [r] : [];
}

function flyBetween(layer, token, node, fromX, fromY, toX, toY, delay, fadeOut) {
    node.style.position = 'fixed';
    node.style.left = '0';
    node.style.top = '0';
    node.style.margin = '0';
    node.style.pointerEvents = 'none';
    node.style.opacity = fadeOut ? '1' : '0';
    node.style.transform = `translate(${fromX}px, ${fromY}px) translate(-50%, -50%) scale(0.82)`;
    node.style.willChange = 'transform, opacity';
    layer.appendChild(node);

    const start = performance.now() + delay;
    const dur = 360;

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
        const x = fromX + (toX - fromX) * p;
        const y = fromY + (toY - fromY) * p;
        const scale = 0.82 + 0.18 * pop;
        node.style.transform = `translate(${x}px, ${y}px) translate(-50%, -50%) scale(${scale})`;

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
    clone.classList.remove('active', 'dimmed', 'dnd-ghost');
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
    layer.appendChild(clone);

    cardEl.style.visibility = 'hidden';

    const dx = from.x - cx;
    const dy = from.y - cy;

    const start = performance.now() + delay;
    const dur = 360;
    let done = false;

    function land() {
        if (done) {
            return;
        }

        done = true;
        cardEl.style.visibility = '';
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
        const x = dx * (1 - p);
        const y = dy * (1 - p);
        const scale = 0.6 + 0.4 * pop;
        const rot = -8 * (1 - p) + endRot * p;
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
