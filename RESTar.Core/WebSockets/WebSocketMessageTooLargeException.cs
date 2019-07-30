namespace RESTar.WebSockets
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar gets a call to IWebSocket.SendResult() with a dataset larger than 16 megabytes.
    /// </summary>
    public class WebSocketMessageTooLargeException : RESTarException
    {
        internal WebSocketMessageTooLargeException() : base(ErrorCodes.WebSocketMessageTooLarge,
            "A dataset larger than 16 megabytes cannot be sent over a single WebSocket message. Use WebSocket streaming instead") { }
    }
}