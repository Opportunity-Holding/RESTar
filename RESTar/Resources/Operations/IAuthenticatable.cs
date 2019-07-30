using RESTar.Requests;

namespace RESTar.Resources.Operations
{
    /// <inheritdoc />
    /// <summary>
    /// Interface used to register an authenticator for a given resource type.
    /// Authenticators are executed once for each REST request to this resource.
    /// </summary>
    public interface IAuthenticatable<T> : IOperationsInterface where T : class
    {
        /// <summary>
        /// The delete method for this IDeleter instance. Defines the Delete
        /// operation for a given resource.
        /// </summary>
        AuthResults Authenticate(IRequest<T> request);
    }
}