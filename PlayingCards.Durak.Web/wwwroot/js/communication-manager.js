(function () {
    'use strict';

    window.initializeCommunication = async function () {
        if (!window.user || !window.user.secret) {
            console.log('User not authenticated, skipping communication initialization');
            return;
        }

        try {
            await window.checkSignalRSupport();

            if (window.signalRConfig && window.signalRClient) {
                await window.initializeSignalR();
            } else {
                setTimeout(async () => {
                    if (window.signalRConfig && window.signalRClient) {
                        try {
                            await window.initializeSignalR();
                        } catch (error) {
                            console.error('Failed to initialize SignalR after delay:', error);
                            window.showConnectionError('SignalR connection failed. Please refresh the page to try again.');
                        }
                    } else {
                        console.log('SignalR not available');
                        window.showConnectionError('Real-time communication is not available. Please refresh the page to try again.');
                    }
                }, 100);
            }
        } catch (error) {
            console.error('Failed to check SignalR support:', error);
            window.showConnectionError('Failed to initialize real-time communication. Please refresh the page to try again.');
        }
    };

    window.checkSignalRSupport = async function () {
        try {
            const response = await fetch('/Home/SignalRSupport');
            window.signalRConfig = await response.json();
            console.log('SignalR configuration:', window.signalRConfig);
        } catch (error) {
            console.error('Failed to check SignalR support:', error);
            window.signalRConfig = {
                allowPollingFallback: true,
                transportType: 'Auto'
            };
        }
    };

    window.initializeSignalR = async function () {
        try {
            console.log('Initializing SignalR communication...');

            window.connectionRetryCount = 0;
            window.lastSignalRError = null;

            if (window.signalRConfig) {
                window.signalRClient.updateConfig(window.signalRConfig);
            }

            window.setupSignalREventHandlers();

            await Promise.race([
                window.signalRClient.start(),
                new Promise((_, reject) =>
                    setTimeout(() => reject(new Error('SignalR connection timeout')), 10000)
                )
            ]);

            console.log('SignalR communication initialized successfully');

            window.startConnectionHealthMonitoring();

            await window.ensureTableGroupMembership();

            console.log('SignalR initialization complete - waiting for real-time updates');

        } catch (error) {
            console.error('SignalR initialization failed:', error);
            window.lastSignalRError = error;
            window.connectionRetryCount++;

            window.showConnectionError('Failed to establish real-time connection. Please refresh the page to try again.');
            throw error;
        }
    };

    window.showConnectionError = function (message) {
        console.error('Connection error:', message);

        window.updateConnectionStatus({
            status: 'Failed',
            message,
            transport: 'None',
            isRealTime: false
        });

        if (window.showErrorMessage) {
            window.showErrorMessage(message, 'connection-error');
        }
    };

    window.ensureTableGroupMembership = async function () {
        if (!window.signalRClient || !window.signalRClient.isConnected || !window.user || !window.user.secret) {
            return;
        }

        try {
            if (window.gameStatus && window.gameStatus.table && !window.signalRClient.hasJoinedTable) {
                await window.signalRClient.joinTable(window.user.secret);
                window.signalRClient.hasJoinedTable = true;
                console.log('Joined table group for real-time updates');
            }
        } catch (error) {
            console.error('Failed to join table group:', error);
        }
    };

    window.startConnectionHealthMonitoring = function () {
        window.stopConnectionHealthMonitoring();

        console.log('Starting connection health monitoring');

        window.connectionHealthMonitor = setInterval(() => {
            if (!window.signalRClient || !window.signalRClient.isConnected) {
                console.warn('SignalR connection lost, attempting recovery');
                window.handleConnectionLoss();
            }
        }, 5000);
    };

    window.stopConnectionHealthMonitoring = function () {
        if (window.connectionHealthMonitor) {
            clearInterval(window.connectionHealthMonitor);
            window.connectionHealthMonitor = null;
            console.log('Stopped connection health monitoring');
        }
    };

    window.handleConnectionLoss = async function () {
        console.log('Handling SignalR connection loss');

        console.log('SignalR connection lost, relying on built-in reconnection');

        window.showErrorMessage('Connection lost, attempting to reconnect...', 'warning');

        window.updateConnectionStatus({
            status: 'Reconnecting',
            message: 'Attempting to reconnect...',
            transport: 'None',
            isRealTime: false
        });
    };

    console.log('Communication manager module loaded');
})();
