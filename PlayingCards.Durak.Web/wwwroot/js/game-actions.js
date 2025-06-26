(function () {
    'use strict';

    window.createTable = async function () {
        if (!window.user || !window.user.secret || !window.user.name) {
            console.error('User not authenticated');
            window.showAlert('Ошибка', 'Пользователь не авторизован', 3);
            return;
        }

        if (!window.signalRClient || !window.signalRClient.isConnected) {
            console.error('SignalR not connected');
            window.showAlert('Ошибка', 'Нет соединения с сервером', 3);
            return;
        }

        try {
            console.log('Creating table via SignalR');
            const tableId = await window.signalRClient.createTable(window.user.secret, window.user.name);
            console.log('Table created successfully via SignalR:', tableId);
            window.gameStatus = null;
        } catch (error) {
            console.error('Failed to create table:', error);
            window.showAlert('Ошибка', 'Не удалось создать стол', 3);
        }
    };

    window.joinToTableClick = function (elem) {
        const tableId = elem.closest('.table').getAttribute('table-id');
        joinToTable(tableId);
    };

    window.joinToTable = async function (tableId) {
        if (!window.signalRClient || !window.signalRClient.isConnected) {
            console.error('SignalR not connected');
            window.showAlert('Ошибка', 'Нет соединения с сервером', 3);
            return;
        }

        try {
            console.log('Joining table via SignalR:', tableId);
            await window.signalRClient.joinExistingTable(tableId, window.user.secret, window.user.name);
            console.log('Joined table successfully via SignalR');
            window.gameStatus = null;
        } catch (error) {
            console.error('Failed to join table:', error);
            window.showAlert('Ошибка', 'Не удалось присоединиться к столу', 3);
        }
    };

    window.leaveFromTable = async function () {
        window.setPlayerActionLoading(true);

        try {
            if (!window.signalRClient || !window.signalRClient.isConnected) {
                throw new Error('SignalR not connected');
            }

            console.log('Leaving table via SignalR');
            await window.signalRClient.leaveTable(window.user.secret);
            console.log('Left table successfully via SignalR');

        } catch (error) {
            console.error('Failed to leave table:', error);
            window.showErrorMessage('Failed to leave table. Please try again.');

            window.setPlayerActionLoading(false);
        } finally {
            setTimeout(() => window.setPlayerActionLoading(false), 500);
        }
    };

    window.startGame = async function () {
        if (!window.gameStatus || !window.gameStatus.table) {
            console.warn('Cannot start game - no table found');
            window.showErrorMessage('No table found to start game');
            return;
        }

        window.setPlayerActionLoading(true);

        try {
            if (!window.signalRClient || !window.signalRClient.isConnected) {
                throw new Error('SignalR not connected');
            }

            console.log('Starting game via SignalR');
            await window.signalRClient.startGame(window.user.secret);
            console.log('Game started successfully via SignalR');

        } catch (error) {
            console.error('Failed to start game:', error);
            window.showErrorMessage('Failed to start game. Please try again.');

            window.setPlayerActionLoading(false);
        } finally {
            setTimeout(() => window.setPlayerActionLoading(false), 500);
        }
    };

    window.play = async function () {
        const defenceCardIndexes = window.getHandActiveCardIndexes();
        const attackCardIndexes = window.getFieldActiveCardIndexes();

        if (!window.canPlayCards(defenceCardIndexes, attackCardIndexes)) {
            console.warn('Cannot play cards - invalid move');
            return;
        }

        window.setPlayerActionLoading(true);

        try {
            if (!window.signalRClient || !window.signalRClient.isConnected) {
                throw new Error('SignalR not connected');
            }

            console.log('Playing cards via SignalR:', { defenceCardIndexes, attackCardIndexes });

            await window.signalRClient.playCards(
                window.user.secret,
                defenceCardIndexes,
                attackCardIndexes.length > 0 ? attackCardIndexes[0] : null
            );

            console.log('Cards played successfully via SignalR');
            window.clearSelectedCards();

        } catch (error) {
            console.error('Failed to play cards:', error);
            window.showErrorMessage('Failed to play cards. Please try again.');

            window.setPlayerActionLoading(false);
        } finally {
            setTimeout(() => window.setPlayerActionLoading(false), 500);
        }
    };

    window.take = async function () {
        window.setPlayerActionLoading(true);

        try {
            if (!window.signalRClient || !window.signalRClient.isConnected) {
                throw new Error('SignalR not connected');
            }

            console.log('Taking cards via SignalR');
            await window.signalRClient.takeCards(window.user.secret);
            console.log('Cards taken successfully via SignalR');

        } catch (error) {
            console.error('Failed to take cards:', error);
            window.showErrorMessage('Failed to take cards. Please try again.');

            window.setPlayerActionLoading(false);
        } finally {
            setTimeout(() => window.setPlayerActionLoading(false), 500);
        }
    };

    console.log('Game actions module loaded');
})();
