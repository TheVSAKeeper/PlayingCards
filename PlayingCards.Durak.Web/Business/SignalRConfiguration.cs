namespace PlayingCards.Durak.Web.Business;

public class SignalRConfiguration
{
    public SignalRTransportType TransportType { get; set; } = SignalRTransportType.ServerSentEventsOnly;

    public bool AllowPollingFallback { get; set; } = true;

    public int MaxConnections { get; set; } = 1000;

    public int ConnectionTimeoutSeconds { get; set; } = 60;

    public int KeepAliveIntervalSeconds { get; set; } = 15;

    public int MaxMessageSize { get; set; } = 32 * 1024;

    public int StreamBufferCapacity { get; set; } = 10;

    public bool EnableDetailedErrors { get; set; } = false;

    public int[] ReconnectDelays { get; set; } = [0, 2000, 10000, 30000];

    public int MaxReconnectAttempts { get; set; } = 10;

    public bool EnableLogging { get; set; } = true;

    // (0=Trace, 1=Debug, 2=Information, 3=Warning, 4=Error, 5=Critical, 6=None).
    public int LogLevel { get; set; } = 2; // Information

    public string HubPath { get; set; } = "/gameHub";

    public bool EnableCors { get; set; } = false;

    public string[] CorsOrigins { get; set; } = [];

    public object GetClientConfiguration()
    {
        return new
        {
            transportType = TransportType.ToString(),
            allowPollingFallback = AllowPollingFallback,
            hubPath = HubPath,
            connectionTimeoutSeconds = ConnectionTimeoutSeconds,
            keepAliveIntervalSeconds = KeepAliveIntervalSeconds,
            reconnectDelays = ReconnectDelays,
            maxReconnectAttempts = MaxReconnectAttempts,
            enableLogging = EnableLogging,
            logLevel = LogLevel,
        };
    }
}
