export function start(el, endTimeMs, kind) {
    stop(el);

    const tick = () => {
        if (!el.isConnected) {
            clearInterval(el.__cdTimer);
            el.__cdTimer = 0;
            return;
        }

        const secs = Math.max(0, Math.round((endTimeMs - Date.now()) / 1000));
        el.textContent = format(secs, kind);
    };

    tick();
    el.__cdTimer = setInterval(tick, 1000);
}

export function stop(el) {
    if (el && el.__cdTimer) {
        clearInterval(el.__cdTimer);
        el.__cdTimer = 0;
    }
}

function format(n, kind) {
    switch (kind) {
        case 'afk': return `осталось ${n} ${secNom(n)}`;
        case 'take': return `я забираю через ${n} ${secAcc(n)}`;
        case 'beat': return `отбито через ${n} ${secAcc(n)}`;
        default: return String(n);
    }
}

function secAcc(n) {
    const d100 = n % 100;
    const d10 = n % 10;
    if (d100 >= 11 && d100 <= 14) return 'секунд';
    if (d10 === 1) return 'секунду';
    if (d10 >= 2 && d10 <= 4) return 'секунды';
    return 'секунд';
}

function secNom(n) {
    const d100 = n % 100;
    const d10 = n % 10;
    if (d100 >= 11 && d100 <= 14) return 'секунд';
    if (d10 === 1) return 'секунда';
    if (d10 >= 2 && d10 <= 4) return 'секунды';
    return 'секунд';
}
