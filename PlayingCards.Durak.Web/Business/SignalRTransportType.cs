namespace PlayingCards.Durak.Web.Business;

public enum SignalRTransportType
{
    // WebSocket → SSE → Long Polling.
    Auto = 0,
    WebSocketOnly = 1,
    ServerSentEventsOnly = 2,
    LongPollingOnly = 3,
}
