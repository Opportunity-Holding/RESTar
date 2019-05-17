using System;
using RESTar.Requests;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// A 200 result that encodes a content
    /// </summary>
    public abstract class Content : OK
    {
        /// <summary>
        /// Us this content locked, for example due to it being tied to a certain
        /// Websocket streaming operation?
        /// </summary>
        internal bool IsLocked { get; set; }

        /// <summary>
        /// Sets the ContentDisposition header to a unique file name of a given extension
        /// </summary>
        public void SetContentDisposition(string extension)
        {
            if (extension == null) return;
            Headers["Content-Disposition"] =
                $"attachment;filename={Request.Resource}_{DateTime.UtcNow:yyMMddHHmmssfff}{extension}";
        }

        /// <inheritdoc />
        protected Content(IRequest request) : base(request) { }
    }
}