namespace RESTar
{
    /// <summary>
    /// Message types used in RESTar
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// Input in the form of a regular HTTP request
        /// </summary>
        HttpInput,

        /// <summary>
        /// Output in the form of a regular HTTP response
        /// </summary>
        HttpOutput,

        /// <summary>
        /// WebSocket input
        /// </summary>
        WebSocketInput,

        /// <summary>
        /// WebSocket output
        /// </summary>
        WebSocketOutput,

        /// <summary>
        /// A WebSocket was opened
        /// </summary>
        WebSocketOpen,

        /// <summary>
        /// A WebSocket was closed
        /// </summary>
        WebSocketClose
    }
}