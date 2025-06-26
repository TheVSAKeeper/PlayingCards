(function () {
    'use strict';

    window.setupSignalREventHandlers = function () {
        if (!window.signalRClient) {
            return;
        }

        console.log('Setting up SignalR event handlers');

        window.signalRClient.on('gameStateChanged', gameState => {
            console.log('Received game state update via SignalR', gameState);
            window.updateGameState(gameState);

            if (gameState && gameState.table && window.user && window.user.secret) {
                window.ensureTableGroupMembership();
            }
        });

        window.signalRClient.on('playerJoined', notification => {
            console.log('Player joined:', notification.PlayerName, 'at table:', notification.TableId);
            window.showPlayerNotification(`${notification.PlayerName} joined the table`, 'info');
        });

        window.signalRClient.on('playerLeft', notification => {
            console.log('Player left:', notification.PlayerName, 'from table:', notification.TableId);
            window.showPlayerNotification(`${notification.PlayerName} left the table`, 'warning');
        });

        window.signalRClient.on('timerUpdate', timerData => {
            console.log('Timer update:', timerData.Type, timerData.RemainingSeconds, 'seconds');
            window.updateTimerDisplay(timerData);

            if (timerData.Type === 0 && timerData.RemainingSeconds <= 10 && timerData.PlayerSecret === window.user?.secret) { // TimerType.Afk = 0
                window.showAfkWarning(timerData.RemainingSeconds);
            }
        });

        window.signalRClient.on('tablesUpdated', tablesState => {
            console.log('Tables updated via SignalR');
            window.updateGameState(tablesState);
        });

        window.signalRClient.on('error', errorMessage => {
            console.error('SignalR server error:', errorMessage);
            window.showErrorMessage(errorMessage, 'error');
        });

        window.signalRClient.on('gameError', errorMessage => {
            console.error('SignalR game error:', errorMessage);
            window.showErrorMessage(errorMessage, 'game-error');
        });

        window.signalRClient.on('connectionStatusChanged', statusData => {
            console.log('Connection status:', statusData.status, statusData.message);
            window.updateConnectionStatus(statusData);

            handleConnectionStatusChange(statusData);
        });

        console.log('SignalR event handlers setup complete');
    };

    window.handleConnectionStatusChange = function (statusData) {
        switch (statusData.status) {
            case 'Connected':
                window.lastSignalRError = null;
                window.connectionRetryCount = 0;
                break;

            case 'Disconnected':
            case 'Failed':
                console.log('SignalR connection failed, handling connection loss');
                setTimeout(() => window.handleConnectionLoss(), 1000);
                break;

            case 'Reconnecting':
                console.log('SignalR is attempting to reconnect');
                break;
        }
    };

    console.log('Event handlers module loaded');
})();
