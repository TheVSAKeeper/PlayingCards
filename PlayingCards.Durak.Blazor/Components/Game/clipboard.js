export async function copyText(text) {
    if (navigator.clipboard && window.isSecureContext) {
        try {
            await navigator.clipboard.writeText(text);
            return true;
        } catch {
        }
    }

    return legacyCopy(text);
}

function legacyCopy(text) {
    const ta = document.createElement('textarea');
    ta.value = text;
    ta.setAttribute('readonly', '');
    ta.style.position = 'fixed';
    ta.style.top = '-1000px';
    ta.style.opacity = '0';
    document.body.appendChild(ta);

    let ok = false;

    try {
        ta.select();
        ta.setSelectionRange(0, text.length);
        ok = document.execCommand('copy');
    } catch {
        ok = false;
    }

    ta.remove();
    return ok;
}
