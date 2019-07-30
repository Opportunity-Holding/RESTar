using System.IO;

namespace RESTar.Results
{
    /// <inheritdoc cref="ResultWrapper" />
    /// <inheritdoc cref="ISerializedResult" />
    /// <summary>
    /// Wraps a result and maps operations to its members
    /// </summary>    /// <summary>
    /// Wraps a serialized result and maps operations to its members
    /// </summary>
    public abstract class SerializedResultWrapper : ResultWrapper, ISerializedResult
    {
        /// <summary>
        /// The wrapped result
        /// </summary>
        protected ISerializedResult SerializedResult { get; }

        /// <inheritdoc />
        protected SerializedResultWrapper(ISerializedResult result) : base(result)
        {
            SerializedResult = result;
        }

        /// <inheritdoc />
        public Stream Body
        {
            get => SerializedResult.Body;
            set => SerializedResult.Body = value;
        }
    }
}