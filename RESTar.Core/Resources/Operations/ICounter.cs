using RESTar.Requests;

namespace RESTar.Resources.Operations
{
    /// <inheritdoc />
    /// <summary>
    /// Interface used to register a Counter for a given resource type
    /// </summary>
    public interface ICounter<T> : IOperationsInterface where T : class
    {
        /// <summary>
        /// The delete method for this IDeleter instance. Defines the Delete
        /// operation for a given resource.
        /// </summary>
        long Count(IRequest<T> request);
    }
}