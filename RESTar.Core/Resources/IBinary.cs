using System.IO;
using RESTar.Requests;

namespace RESTar.Resources
{
    /// <summary>
    /// Defines the operations of a binary resource
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IBinary<T> where T : class, IBinary<T>
    {
        /// <summary>
        /// Generates a binary stream and content type for a request
        /// </summary>
        (Stream stream, ContentType contentType) Select(IRequest<T> request);
    }
}