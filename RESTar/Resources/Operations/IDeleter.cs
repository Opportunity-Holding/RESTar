using RESTar.Requests;

namespace RESTar.Resources.Operations
{
    /// <inheritdoc />
    /// <summary>
    /// Interface used to register a Deleter for a given resource type
    /// </summary>
    public interface IDeleter<T> : IOperationsInterface where T : class
    {
        /// <summary>
        /// The delete method for this IDeleter instance. Defines the Delete
        /// operation for a given resource.
        /// </summary>
        int Delete(IRequest<T> request);
    }
}