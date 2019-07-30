namespace RESTar.Resources.Operations
{
    /// <summary>
    /// By implementing the <see cref="IValidator{T}"/> interface, entity resources can add custom validation
    /// validation logic that will be called by RESTar each time an entity is inserted or updated in the resource.
    /// </summary>
    public interface IValidator<T> where T : class
    {
        /// <summary>
        /// Validates the entity given as input. If invalid, include a reason in the out parameter to inform the
        /// user of the validation error. Return true if and only if the entity is valid.
        /// </summary>
        bool IsValid(T entity, out string invalidReason);
    }
}