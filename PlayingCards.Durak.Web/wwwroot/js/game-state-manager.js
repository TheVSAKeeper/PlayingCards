(function () {
    'use strict';

    window.updateGameState = function (status) {
        if (window.gameStatus !== null && window.gameStatus.version === status.version) {
            return;
        }

        window.gameStatus = status;
        window.renderGameState();
    };

    window.getCommunicationStatus = function () {
        return {
            mode: 'SignalR',
            connected: window.signalRClient?.isConnected || false,
            transport: window.signalRClient?.getActiveTransport() || 'None',
            lastError: window.lastSignalRError?.message || null,
            retryCount: window.connectionRetryCount
        };
    };

    console.log('Game state manager module loaded');
})();
