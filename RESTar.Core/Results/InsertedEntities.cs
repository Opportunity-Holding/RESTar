using System.Net;
using RESTar.Requests;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on successful insertion of entities
    /// </summary>
    public class InsertedEntities : Change
    {
        /// <summary>
        /// The number of inserted entities
        /// </summary>
        public int InsertedCount { get; }

        internal InsertedEntities(IRequest request, int count) : base(request)
        {
            InsertedCount = count;
            StatusCode = count < 1 ? HttpStatusCode.OK : HttpStatusCode.Created;
            StatusDescription = StatusCode.ToString();
            Headers.Info = $"{count} entities inserted into '{request.Resource}'";
        }

        /// <inheritdoc />
        public override string Metadata => $"{nameof(InsertedEntities)};{Request.Resource};{InsertedCount}";
    }
}