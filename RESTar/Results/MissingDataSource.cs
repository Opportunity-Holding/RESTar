using RESTar.Requests;

namespace RESTar.Results
{
    /// <inheritdoc />
    internal class MissingDataSource : BadRequest
    {
        /// <inheritdoc />
        public MissingDataSource(IRequest request) : base(ErrorCodes.NoDataSource, $"Missing data source for method {request.Method.ToString()}") { }
    }
}