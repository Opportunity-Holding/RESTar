using System.Collections.Generic;

namespace RESTar.Requests
{
    /// <inheritdoc />
    /// <summary>
    /// Represents a collection of cookies
    /// </summary>
    public class Cookies : HashSet<Cookie>
    {
        /// <inheritdoc />
        public Cookies() { }

        /// <inheritdoc />
        public Cookies(IEnumerable<string> cookieStrings)
        {
            foreach (var cookieString in cookieStrings)
            {
                if (Cookie.TryParse(cookieString, out var cookie))
                    Add(cookie);
            }
        }
    }
}