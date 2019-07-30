using RESTar.Requests;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on successful deletion of entities
    /// </summary>
    public class DeletedEntities : Change
    {
        /// <summary>
        /// The number of entities deleted
        /// </summary>
        public int DeletedCount { get; }

        internal DeletedEntities(IRequest request, int count) : base(request)
        {
            DeletedCount = count;
            Headers.Info = $"{count} entities deleted from '{request.Resource}'";
        }

        /// <inheritdoc />
        public override string Metadata => $"{nameof(DeletedEntities)};{Request.Resource};{DeletedCount}";
    }
}