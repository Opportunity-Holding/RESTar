﻿using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    public class MissingDataSource : BadRequest
    {
        /// <inheritdoc />
        public MissingDataSource(IRequest request) : base(ErrorCodes.NoDataSource, $"Missing data source for method {request.Method.ToString()}") { }
    }
}