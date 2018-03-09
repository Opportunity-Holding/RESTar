﻿using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters a request evaluation that cannot be traced
    /// </summary>
    public class Untraceable : RESTarError
    {
        internal Untraceable() : base(ErrorCodes.Untraceable, "An attempt was made to evaluate a request that could not be traced to an " +
                                                              "initial external or internal TCP connection or call site.") { }
    }
}