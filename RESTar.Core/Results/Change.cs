using RESTar.Requests;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// A result that encodes a change in a resource, for example an update or insert
    /// </summary>
    public abstract class Change : OK
    {
        /// <inheritdoc />
        protected Change(IRequest request) : base(request) { }
    }
}