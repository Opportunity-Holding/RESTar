﻿using RESTar.Internal;
using RESTar.Resources;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar cannot locate a view by some search string
    /// </summary>
    public class UnknownView : NotFound
    {
        internal UnknownView(string searchString, ITarget resource) : base(ErrorCodes.UnknownResourceView,
            $"RESTar could not locate any resource view in '{resource.Name}' by '{searchString}'.") { }
    }
}