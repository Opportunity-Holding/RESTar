using RESTar.Requests;

namespace RESTar.Resources.Operations
{
    /// <inheritdoc />
    /// <summary>
    /// Interface used to register an Updater for a given resource type
    /// </summary>
    public interface IUpdater<T> : IOperationsInterface where T : class
    {
        /// <summary>
        /// The update method for this IUpdater instance. Defines the Update
        /// operation for a given resource.
        /// </summary>
        int Update(IRequest<T> request);
    }
}