namespace RESTar.WebSockets
{
    internal class UnknownWebSocketIdException : RESTarException
    {
        public UnknownWebSocketIdException(string info) : base(ErrorCodes.UnknownWebSocketId, info) { }
    }
}