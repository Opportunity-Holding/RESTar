using System.Collections.Generic;
using RESTar.Requests;

namespace RESTar.Resources
{
    /// <summary>
    /// Decorate terminal resources with this interface to define a custom instance resolver
    /// that is called before the constructor, when setting up new terminals.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ITerminalInstanceResolver<out T> where T : class
    {
        /// <summary>
        /// Returns an instance of this terminal resource type
        /// </summary>
        /// <param name="assignments">Assignments contained in the upgrade or navigation request</param>
        /// <param name="headers">The headers of the upgrade request or existing websocket</param>
        /// <param name="cookies">The cookies of the upgrade request or existing websocket</param>
        /// <returns></returns>
        T GetInstance(IDictionary<string, object> assignments, Headers headers, ReadonlyCookies cookies);
    }
}