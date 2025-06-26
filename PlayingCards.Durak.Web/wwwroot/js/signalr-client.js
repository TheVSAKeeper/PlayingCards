class SignalRClient {
    constructor(config = null) {
        this.connection = null;
        this.isConnected = false;
        this.isConnecting = false;
        this.connectionState = 'Disconnected';
        this.reconnectAttempts = 0;
        this.eventHandlers = new Map();

        this.config = config || {
            hubPath: '/gameHub',
            reconnectDelays: [0, 2000, 10000, 30000],
            maxReconnectAttempts: 10,
            connectionTimeoutSeconds: 60,
            keepAliveIntervalSeconds: 15,
            enableLogging: true,
            logLevel: 2
        };

        this.maxReconnectAttempts = this.config.maxReconnectAttempts || 10;
        this.hubUrl = this.config.hubPath || '/gameHub';
        this.reconnectDelays = this.config.reconnectDelays || [0, 2000, 10000, 30000];
    }

    updateConfig(config) {
        this.config = { ...this.config, ...config };
        this.maxReconnectAttempts = this.config.maxReconnectAttempts || 10;
        this.hubUrl = this.config.hubPath || '/gameHub';
        this.reconnectDelays = this.config.reconnectDelays || [0, 2000, 10000, 30000];

        // Log transport configuration
        const transportType = this.config.transportType || 'Auto';
        console.log(`SignalR transport configured: ${transportType}`);

        if (!this.connection) {
            this.initializeConnection();
        }
    }

    getTransportType() {
        if (this.config.transportType) {
            switch (this.config.transportType) {
                case 'Auto':
                    return null;
                case 'WebSocketOnly':
                    return signalR.HttpTransportType.WebSockets;
                case 'ServerSentEventsOnly':
                    return signalR.HttpTransportType.ServerSentEvents;
                case 'LongPollingOnly':
                    return signalR.HttpTransportType.LongPolling;
                default:
                    console.warn('Unknown transport type:', this.config.transportType);
                    return null;
            }
        }

        return null;
    }

    initializeConnection() {
        try {
            const logLevel = this.config.enableLogging ?
                this.config.logLevel || 2 : signalR.LogLevel.None;

            const connectionOptions = {
                accessTokenFactory: () => window.user?.secret || ''
            };

            const transport = this.getTransportType();
            if (transport) {
                connectionOptions.transport = transport;
            }

            this.connection = new signalR.HubConnectionBuilder()
                .withUrl(this.hubUrl, connectionOptions)
                .withAutomaticReconnect(this.reconnectDelays)
                .configureLogging(logLevel)
                .build();

            this.setupEventHandlers();
            this.setupConnectionEvents();
        } catch (error) {
            console.error('Failed to initialize SignalR connection:', error);
            this.notifyConnectionStatus('Failed', 'Failed to initialize connection');
        }
    }

    setupConnectionEvents() {
        if (!this.connection) {
            return;
        }

        this.connection.onclose(error => {
            this.isConnected = false;
            this.connectionState = 'Disconnected';
            console.log('SignalR connection closed:', error);
            this.notifyConnectionStatus('Disconnected', error?.message);
        });

        this.connection.onreconnecting(error => {
            this.isConnected = false;
            this.connectionState = 'Reconnecting';
            this.reconnectAttempts++;
            console.log(`SignalR reconnecting (attempt ${this.reconnectAttempts}):`, error);
            this.notifyConnectionStatus('Reconnecting', `Reconnecting... (attempt ${this.reconnectAttempts})`);
        });

        this.connection.onreconnected(connectionId => {
            this.isConnected = true;
            this.connectionState = 'Connected';
            this.reconnectAttempts = 0;
            console.log('SignalR reconnected:', connectionId);
            this.notifyConnectionStatus('Connected', 'Reconnected successfully');

            if (window.user?.secret) {
                this.joinTable(window.user.secret);
            }
        });
    }

    setupEventHandlers() {
        if (!this.connection) {
            return;
        }

        this.connection.on('GameStateChanged', gameState => {
            console.log('Game state changed:', gameState);
            this.emit('gameStateChanged', gameState);
        });

        this.connection.on('PlayerJoined', notification => {
            console.log('Player joined:', notification);
            this.emit('playerJoined', notification);
        });

        this.connection.on('PlayerLeft', notification => {
            console.log('Player left:', notification);
            this.emit('playerLeft', notification);
        });

        this.connection.on('TimerUpdate', timerData => {
            console.log('Timer update:', timerData);
            this.emit('timerUpdate', timerData);
        });

        this.connection.on('TablesUpdated', tablesState => {
            console.log('Tables updated:', tablesState);
            this.emit('tablesUpdated', tablesState);
        });

        this.connection.on('Error', message => {
            console.error('SignalR error:', message);
            this.emit('error', { message, type: 'general' });
        });

        this.connection.on('GameError', message => {
            console.error('Game error:', message);
            this.emit('error', { message, type: 'game' });
        });
    }

    async start() {
        if (!this.connection || this.isConnecting || this.isConnected) {
            return;
        }

        try {
            this.isConnecting = true;
            this.connectionState = 'Connecting';
            this.notifyConnectionStatus('Connecting', 'Establishing connection...');

            await this.connection.start();

            this.isConnected = true;
            this.isConnecting = false;
            this.connectionState = 'Connected';
            this.reconnectAttempts = 0;

            const transport = this.getActiveTransport();
            console.log(`SignalR connected successfully using ${transport} transport`);
            this.notifyConnectionStatus('Connected', `Connected via ${transport}`);

            if (window.user?.secret) {
                await this.joinTable(window.user.secret);
            }

        } catch (error) {
            this.isConnecting = false;
            this.connectionState = 'Failed';
            console.error('Failed to start SignalR connection:', error);
            this.notifyConnectionStatus('Failed', error.message);
            throw error;
        }
    }

    async stop() {
        if (!this.connection || !this.isConnected) {
            return;
        }

        try {
            await this.connection.stop();
            this.isConnected = false;
            this.connectionState = 'Disconnected';
            console.log('SignalR connection stopped');
            this.notifyConnectionStatus('Disconnected', 'Connection stopped');
        } catch (error) {
            console.error('Error stopping SignalR connection:', error);
        }
    }

    async joinTable(playerSecret) {
        if (!this.isConnected || !playerSecret) {
            return;
        }

        try {
            await this.connection.invoke('JoinTable', playerSecret);
            console.log('Joined table group for player:', playerSecret);
        } catch (error) {
            console.error('Failed to join table:', error);
        }
    }

    async leaveTable(playerSecret) {
        if (!this.isConnected || !playerSecret) {
            return;
        }

        try {
            await this.connection.invoke('LeaveTable', playerSecret);
            console.log('Left table group for player:', playerSecret);
        } catch (error) {
            console.error('Failed to leave table:', error);
        }
    }

    async playCards(playerSecret, cardIndexes, attackCardIndex = null) {
        if (!this.isConnected) {
            throw new Error('SignalR not connected');
        }

        try {
            await this.connection.invoke('PlayCards', playerSecret, cardIndexes, attackCardIndex);
            console.log('Cards played via SignalR');
        } catch (error) {
            console.error('Failed to play cards:', error);
            throw error;
        }
    }

    async takeCards(playerSecret) {
        if (!this.isConnected) {
            throw new Error('SignalR not connected');
        }

        try {
            await this.connection.invoke('TakeCards', playerSecret);
            console.log('Cards taken via SignalR');
        } catch (error) {
            console.error('Failed to take cards:', error);
            throw error;
        }
    }

    async startGame(playerSecret) {
        if (!this.isConnected) {
            throw new Error('SignalR not connected');
        }

        try {
            await this.connection.invoke('StartGame', playerSecret);
            console.log('Game started via SignalR');
        } catch (error) {
            console.error('Failed to start game:', error);
            throw error;
        }
    }

    async createTable(playerSecret, playerName) {
        if (!this.isConnected) {
            throw new Error('SignalR not connected');
        }

        try {
            const tableId = await this.connection.invoke('CreateTable', playerSecret, playerName);
            console.log('Table created via SignalR:', tableId);
            return tableId;
        } catch (error) {
            console.error('Failed to create table:', error);
            throw error;
        }
    }

    async joinExistingTable(tableId, playerSecret, playerName) {
        if (!this.isConnected) {
            throw new Error('SignalR not connected');
        }

        try {
            await this.connection.invoke('JoinExistingTable', tableId, playerSecret, playerName);
            console.log('Joined existing table via SignalR:', tableId);
        } catch (error) {
            console.error('Failed to join existing table:', error);
            throw error;
        }
    }

    on(eventName, handler) {
        if (!this.eventHandlers.has(eventName)) {
            this.eventHandlers.set(eventName, []);
        }
        this.eventHandlers.get(eventName).push(handler);
    }

    off(eventName, handler) {
        if (this.eventHandlers.has(eventName)) {
            const handlers = this.eventHandlers.get(eventName);
            const index = handlers.indexOf(handler);
            if (index > -1) {
                handlers.splice(index, 1);
            }
        }
    }

    emit(eventName, data) {
        if (this.eventHandlers.has(eventName)) {
            this.eventHandlers.get(eventName).forEach(handler => {
                try {
                    handler(data);
                } catch (error) {
                    console.error(`Error in event handler for ${eventName}:`, error);
                }
            });
        }
    }

    getActiveTransport() {
        if (!this.connection || !this.isConnected) {
            return 'None';
        }

        try {
            const transport = this.connection.connection?.transport;
            if (transport) {
                if (transport.constructor.name.includes('WebSocket')) {
                    return 'WebSocket';
                } else if (transport.constructor.name.includes('ServerSentEvents')) {
                    return 'Server-Sent Events';
                } else if (transport.constructor.name.includes('LongPolling')) {
                    return 'SignalR Long Polling';
                }
            }
        } catch (error) {
            console.debug('Could not detect transport type:', error);
        }

        return 'Unknown';
    }

    isRealTimeTransport() {
        const transport = this.getActiveTransport();
        return transport === 'WebSocket' || transport === 'Server-Sent Events' || transport === 'SignalR Long Polling';
    }

    notifyConnectionStatus(status, message) {
        const statusData = {
            status,
            message,
            timestamp: new Date(),
            transport: this.getActiveTransport(),
            isRealTime: this.isRealTimeTransport()
        };
        this.emit('connectionStatusChanged', statusData);
    }
}

window.signalRClient = null;

document.addEventListener('DOMContentLoaded', function () {
    window.signalRClient = new SignalRClient();

    console.log('SignalR client initialized');
});

window.startSignalRIfAuthenticated = function () {
    if (window.user && window.user.secret && window.signalRClient) {
        window.signalRClient.start().catch(error => console.error('Failed to start SignalR connection:', error));
    }
};
