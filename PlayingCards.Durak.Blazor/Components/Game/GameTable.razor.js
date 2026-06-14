let dnet = null;
let root = null;
let drag = null;

const THRESHOLD = 6;

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
