namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters a content stream that is unreadable
    /// </summary>
    internal class UnreadableContentStream : Internal
    {
        internal UnreadableContentStream(Content content) : base(ErrorCodes.UnreadableContentStream,
            $"RESTar encountered an unreadable content stream from resource '{content.Request.Resource}'") { }
    }
}