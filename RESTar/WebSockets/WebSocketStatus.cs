namespace RESTar.WebSockets
{
    /// <summary>
    /// The status of a RESTar WebSocket connection
    /// </summary>
    public enum WebSocketStatus
    {
        /// <summary>
        /// The WebSocket is not yet opened. Messages cannot be sent or received.
        /// </summary>
        Waiting,

        /// <summary>
        /// The WebSocket is open. Messages can be sent and received.
        /// </summary>
        Open,

        /// <summary>
        /// The WebSocket is pending and will soon close. Messages cannot be sent. Disconnect calls are
        /// ignored.
        /// </summary>
        PendingClose,

        /// <summary>
        /// The WebSocket is closed. Messages cannot be sent or received.
        /// </summary>
        Closed,

        /// <summary>
        /// The WebSocket is suspended during the course of a streaming session.
        /// </summary>
        Suspended
    }
}