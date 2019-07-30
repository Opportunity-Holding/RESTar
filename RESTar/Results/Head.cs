using RESTar.Requests;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// A result that contains only status code, description and headers
    /// </summary>
    public class Head : OK
    {
        /// <summary>
        /// The number of entities contained in this result
        /// </summary>
        public ulong EntityCount { get; }

        internal Head(IRequest request, long count) : base(request) => EntityCount = (ulong) count;

        /// <inheritdoc />
        public override string Metadata => $"{nameof(Head)};{Request.Resource};{EntityCount}";
    }
}