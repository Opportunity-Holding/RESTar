﻿using RESTar.Internal;

namespace RESTar.Results.Error.NotFound
{
    public class AmbiguousResource : NotFound
    {
        internal AmbiguousResource(string searchString) : base(ErrorCodes.AmbiguousResource,
            $"RESTar could not uniquely identify a resource by '{searchString}'. Try qualifying the name further. ") { }
    }
}