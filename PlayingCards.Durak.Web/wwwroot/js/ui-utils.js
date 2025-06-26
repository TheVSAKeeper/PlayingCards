(function () {
    'use strict';

    window.showErrorMessage = function (message, type = 'error') {
        let displayMessage = message;
        if (typeof message === 'object' && message !== null) {
            displayMessage = message.message || JSON.stringify(message);
        }

        console.error('Showing error message:', displayMessage, 'type:', type);

        let errorEl = document.getElementById('error-notification');
        if (!errorEl) {
            errorEl = document.createElement('div');
            errorEl.id = 'error-notification';
            errorEl.className = 'error-notification notification';
            document.body.appendChild(errorEl);
        }

        errorEl.textContent = displayMessage;
        errorEl.className = `error-notification notification ${type}`;
        errorEl.style.display = 'block';

        setTimeout(() => {
            if (errorEl) {
                errorEl.style.display = 'none';
            }
        }, 5000);

        console.error(`[${type.toUpperCase()}] ${displayMessage}`);
    };

    window.showPlayerNotification = function (message, type = 'info') {
        console.log('Player notification:', message, 'type:', type);

        let notificationEl = document.getElementById('player-notification');
        if (!notificationEl) {
            notificationEl = document.createElement('div');
            notificationEl.id = 'player-notification';
            notificationEl.className = 'player-notification notification';
            document.body.appendChild(notificationEl);
        }

        notificationEl.textContent = message;
        notificationEl.className = `player-notification notification ${type}`;
        notificationEl.style.display = 'block';

        setTimeout(() => {
            if (notificationEl) {
                notificationEl.style.display = 'none';
            }
        }, 3000);
    };

    window.setPlayerActionLoading = function (isLoading) {
        const actionButtons = document.querySelectorAll('#playCards, #takeCards, #startGame');

        actionButtons.forEach(button => {
            if (isLoading) {
                button.disabled = true;
                button.classList.add('loading');

                if (!button.dataset.originalText) {
                    button.dataset.originalText = button.textContent;
                }
                button.textContent = 'Loading...';
            } else {
                button.disabled = false;
                button.classList.remove('loading');

                if (button.dataset.originalText) {
                    button.textContent = button.dataset.originalText;
                }
            }
        });
    };

    window.clearSelectedCards = function () {
        const selectedCards = document.querySelectorAll('.play-card.active, .attack-card.active');
        selectedCards.forEach(card => card.classList.remove('active'));

        const playCardsButton = document.getElementById('playCards');
        if (playCardsButton) {
            playCardsButton.classList.add('hidden');
        }

        console.log('Cleared selected cards');
    };

    window.updateConnectionStatus = function (statusData) {
        console.log('Connection status changed:', statusData.status);

        let statusEl = document.getElementById('connection-status');
        if (!statusEl) {
            statusEl = document.createElement('div');
            statusEl.id = 'connection-status';
            statusEl.className = 'connection-status';
            document.body.appendChild(statusEl);
        }

        const statusText = getConnectionStatusText(statusData.status, statusData);
        statusEl.textContent = statusText;

        const isDevelopment = window.signalRConfig?.isDevelopment || false;
        const baseClasses = `connection-status status-${statusData.status.toLowerCase()}`;
        const devClass = isDevelopment ? ' dev-mode' : '';
        statusEl.className = baseClasses + devClass;
        statusEl.title = statusData.message || statusText;

        if (statusData.status === 'Connected') {
            statusEl.style.display = 'block';

            const isDevelopment = window.signalRConfig?.isDevelopment || false;
            if (!isDevelopment) {
                setTimeout(() => {
                    if (statusEl && statusEl.textContent === statusText) {
                        statusEl.style.display = 'none';
                    }
                }, 3000);
            }
        } else {
            statusEl.style.display = 'block';
        }
    };

    window.getConnectionStatusText = function (status, statusData = null) {
        const isDevelopment = window.signalRConfig?.isDevelopment || false;

        switch (status) {
            case 'Connected':
                if (isDevelopment) {
                    let mode = 'Polling';
                    if (statusData && statusData.isRealTime) {
                        const transport = statusData.transport;
                        if (transport === 'WebSocket') {
                            mode = 'WebSocket';
                        } else if (transport === 'Server-Sent Events') {
                            mode = 'SSE';
                        } else {
                            mode = 'SignalR';
                        }
                    } else if (window.useSignalR) {
                        mode = 'SignalR';
                    }
                    const retries = window.connectionRetryCount > 0 ? ` (${window.connectionRetryCount} retries)` : '';
                    return `🟢 ${mode}${retries}`;
                }

                if (statusData && statusData.isRealTime) {
                    const transport = statusData.transport;
                    if (transport === 'WebSocket') {
                        return '🟢 WebSocket';
                    } else if (transport === 'Server-Sent Events') {
                        return '🟢 Real-time';
                    } else if (transport === 'SignalR Long Polling') {
                        return '🟢 SignalR';
                    } else {
                        return '🟢 Real-time';
                    }
                }
                return window.useSignalR ? '🟢 Real-time' : '🔴 Disconnected';
            case 'Connecting':
                return '🟡 Connecting...';
            case 'Reconnecting':
                if (isDevelopment) {
                    const attempt = window.connectionRetryCount || 0;
                    return `🟡 Reconnecting... (${attempt + 1})`;
                }
                return '🟡 Reconnecting...';
            case 'Disconnected':
                return isDevelopment ? '🔴 Disconnected (DEV)' : '🔴 Disconnected';
            case 'Failed':
                return isDevelopment ? '🔴 Connection Failed (DEV)' : '🔴 Connection Failed';
            default:
                return isDevelopment ? `${status} (DEV)` : status;
        }
    };

    window.showAfkWarning = function (remainingSeconds) {
        if (remainingSeconds <= 0) {
            return;
        }

        let warningEl = document.getElementById('afk-warning');
        if (!warningEl) {
            warningEl = document.createElement('div');
            warningEl.id = 'afk-warning';
            warningEl.className = 'afk-warning notification';
            document.body.appendChild(warningEl);
        }

        warningEl.textContent = `⚠️ You will be kicked for inactivity in ${remainingSeconds} seconds!`;
        warningEl.style.display = 'block';

        setTimeout(() => {
            if (warningEl && remainingSeconds <= 0) {
                warningEl.style.display = 'none';
            }
        }, 3000);
    };

    console.log('UI utilities module loaded');
})();
