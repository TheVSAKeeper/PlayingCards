(function () {
    'use strict';

    window.user = null;
    window.gameStatus = null;
    window.speakTimerIntervalId = null;

    window.authCookieName = 'auth_name';
    window.authCookieSecret = 'auth_secret';

    window.signalRConfig = null;

    window.connectionHealthMonitor = null;
    window.lastSignalRError = null;
    window.connectionRetryCount = 0;

    window.gameStatusList = {
        waitPlayers: 0,
        readyToStart: 1,
        inProcess: 2,
        finish: 3
    };

    window.stopRoundStatus = {
        take: 0,
        successDefence: 1
    };

    window.ranks = {
        '6': '6',
        '7': '7',
        '8': '8',
        '9': '9',
        '10': '10',
        '11': 'J',
        '12': 'Q',
        '13': 'K',
        '14': 'A'
    };

    window.suits = {
        '0': '♣',
        '1': '♦',
        '2': '♥',
        '3': '♠'
    };

    console.log('Game configuration module loaded');
})();
