﻿using System;
using RESTar.Internal;

namespace RESTar.Results.Error.NotFound
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar cannot find an entity resource by some search string
    /// </summary>
    public class UnknownEntityResource : NotFound
    {
        /// <inheritdoc />
        public UnknownEntityResource(ErrorCodes code, string message, Exception ie) : base(code, message, ie) { }

        internal UnknownEntityResource(string searchString) : base(ErrorCodes.UnknownResource,
            $"RESTar could not locate any entity resource by '{searchString}'.") { }
    }
}