using RESTar.Requests;

namespace RESTar.Resources.Operations
{
    /// <inheritdoc />
    /// <summary>
    /// Interface used to register an Inserter for a given resource type
    /// </summary>
    public interface IInserter<T> : IOperationsInterface where T : class
    {
        /// <summary>
        /// The insert method for this IInserter instance. Defines the Insert
        /// operation for a given resource.
        /// </summary>
        int Insert(IRequest<T> request);
    }
}