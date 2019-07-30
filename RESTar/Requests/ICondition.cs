using RESTar.Meta;

namespace RESTar.Requests
{
    /// <summary>
    /// A non-generic interface for conditions
    /// </summary>
    public interface ICondition
    {
        /// <summary>
        /// The key of the condition
        /// </summary>
        string Key { get; }

        /// <summary>
        /// The term describing the property to compare with
        /// </summary>
        Term Term { get; }

        /// <summary>
        /// Converts the condition to a new target type and (optionally) a new key
        /// </summary>
        Condition<T> Redirect<T>(string newKey = null) where T : class;

        /// <summary>
        /// Tries to converts the condition to a new target type and (optionally) a new key, and
        /// returns true if the operation is successful.
        /// </summary>
        bool TryRedirect<T>(out Condition<T> condition, string newKey = null) where T : class;
    }
}