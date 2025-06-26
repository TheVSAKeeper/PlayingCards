(function () {
    'use strict';

    window.login = function () {
        const name = document.getElementById('nameInput').value;
        const id = uuidv4();
        setCookie(window.authCookieName, name, 1);
        setCookie(window.authCookieSecret, id, 1);
        window.init();
    };

    window.logout = function () {
        if (window.gameStatus != null && window.gameStatus.table != null) {
            window.leaveFromTable();
        }
        deleteCookie(window.authCookieName);
        deleteCookie(window.authCookieSecret);
        window.init();
    };

    window.setCookie = function (name, value, days) {
        let expires = '';
        if (days) {
            const date = new Date();
            date.setTime(date.getTime() + days * 24 * 60 * 60 * 1000);
            expires = '; expires=' + date.toUTCString();
        }
        document.cookie = name + '=' + (value || '') + expires + '; path=/';
    };

    window.deleteCookie = function (name) {
        document.cookie = name + '=; Path=/; Expires=Thu, 01 Jan 1970 00:00:01 GMT;';
    };

    window.getCookie = function (name) {
        const nameEQ = name + '=';
        const ca = document.cookie.split(';');
        for (let i = 0; i < ca.length; i++) {
            let c = ca[i];
            while (c.charAt(0) === ' ') {
                c = c.substring(1, c.length);
            }
            if (c.indexOf(nameEQ) === 0) {
                return c.substring(nameEQ.length, c.length);
            }
        }
        return null;
    };

    window.uuidv4 = function () {
        return '10000000-1000-4000-8000-100000000000'.replace(/[018]/g, c =>
            (+c ^ crypto.getRandomValues(new Uint8Array(1))[0] & 15 >> +c / 4).toString(16)
        );
    };

    window.initializeAuth = function () {
        window.user = null;
        const cookieName = getCookie(window.authCookieName);
        const cookieSecret = getCookie(window.authCookieSecret);

        if (cookieName === null || cookieSecret === null) {
            document.getElementById('nameLabel').classList.add('hidden');
            document.getElementById('logoutBtn').classList.add('hidden');
            document.getElementById('loginBlock').classList.remove('hidden');
            document.getElementById('main').classList.add('hidden');
            document.getElementById('tableMain').classList.add('hidden');
            document.getElementById('accountContainer').classList.add('not-auth');
        } else {
            window.user = {
                name: cookieName,
                secret: cookieSecret
            };
            document.getElementById('nameLabel').classList.remove('hidden');
            document.getElementById('logoutBtn').classList.remove('hidden');
            document.getElementById('loginBlock').classList.add('hidden');
            document.getElementById('main').classList.remove('hidden');
            document.getElementById('tableMain').classList.remove('hidden');
            document.getElementById('accountContainer').classList.remove('not-auth');
        }

        if (window.user !== null) {
            document.getElementById('nameLabel').innerHTML = window.user.name;
        } else {
            document.getElementById('nameLabel').innerHTML = '';
        }
    };

    console.log('Authentication manager module loaded');
})();
