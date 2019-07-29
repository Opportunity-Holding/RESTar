using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RESTar.Requests
{
    /// <inheritdoc cref="IEquatable{T}" />
    /// <summary>
    /// Represents a cookie, as used in RESTar requests and results. Two cookies with the same name are
    /// considered equal, and new values will always replace existing ones in hash collections.
    /// </summary>
    public struct Cookie : IEquatable<Cookie>
    {
        /// <summary>
        /// The name of the cookie
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The value of the cookie
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// The date and time when the cookie expires, as universal time
        /// </summary>
        public DateTime? Expires { get; }

        /// <summary>
        /// The time in seconds for when a cookie will be deleted
        /// </summary>
        public int? MaxAge { get; }

        /// <summary>
        /// The domain of the cookie
        /// </summary>
        public string Domain { get; }

        /// <summary>
        /// HTTP-only cookies aren't accessible via JavaScript through the Document.cookie
        /// property or the XMLHttpRequest API
        /// </summary>
        public bool HttpOnly { get; }

        /// <summary>
        /// A secure cookie will only be sent to the server when a request is made using SSL
        /// </summary>
        public bool Secure { get; }

        /// <summary>
        /// The path that this cookie is placed at
        /// </summary>
        public string Path { get; }

        #region In future versions

        // public SameSitePolicy SameSite { get; }

        #endregion

        /// <summary>
        /// Used internally to create a cookie with a given name, without validation
        /// </summary>
        internal Cookie(string name) : this() => Name = name;

        /// <summary>
        /// Creates a new Cookie instance
        /// </summary>
        /// <param name="name">The name of the cookie to create. can be any US-ASCII characters except
        /// control characters (CTLs), spaces, or tabs. It also must not contain a separator character
        /// like the following: ( ) &lt; &gt; @ , ; : \ " /  [ ] ? = { }.
        /// </param>
        /// <param name="value">The value of the cookie to create. Can optionally be set in double quotes
        /// and any US-ASCII characters excluding CTLs, whitespace, double quotes, comma, semicolon, and
        /// backslash are allowed</param>
        /// <param name="expires">The maximum lifetime of the cookie as an DateTime</param>
        /// <param name="maxAge">Number of seconds until the cookie expires. A zero or negative number will expire the cookie immediately</param>
        /// <param name="domain">Specifies those hosts to which the cookie will be sent. If not specified, defaults to the host portion of
        /// the current document location (but not including subdomains)</param>
        /// <param name="httpOnly">HTTP-only cookies aren't accessible via JavaScript through the Document.cookie property or the XMLHttpRequest API</param>
        /// <param name="secure">A secure cookie will only be sent to the server when a request is made using SSL</param>
        /// <param name="path">The path that this cookie will be placed at</param>
        public Cookie
        (
            string name,
            string value,
            DateTime? expires = null,
            int? maxAge = null,
            string domain = null,
            bool httpOnly = false,
            bool secure = false,
            string path = null
        )
        {
            if (!Regex.IsMatch(name, RegEx.CookieName))
                throw new ArgumentException($"Invalid cookie name: {name}");
            Name = name;
            Value = value;
            Expires = expires;
            MaxAge = maxAge;
            Domain = domain;
            HttpOnly = httpOnly;
            Secure = secure;
            Path = path;
        }

        /// <summary>
        /// Parses a cookie from a cookie string, for example Name=Value; Expires=Wed, 21 Oct 2015 07:28:00 GMT
        /// </summary>
        public static Cookie Parse(string cookieString)
        {
            try
            {
                string name = null;
                string value = null;
                DateTime? expires = null;
                int? maxAge = null;
                string domain = null;
                var httpOnly = false;
                var secure = false;
                string path = null;

                var parts = cookieString
                    .Split(';')
                    .Select(x => x.Split('='));
                var nameAndValueParsed = false;

                foreach (var part in parts)
                {
                    var (keyPart, valuePart) = (part[0].Trim(), part.ElementAtOrDefault(1)?.Trim());
                    if (!nameAndValueParsed)
                    {
                        name = keyPart;
                        value = valuePart;
                        nameAndValueParsed = true;
                    }
                    else
                    {
                        switch (keyPart)
                        {
                            case var _ when keyPart.EqualsNoCase("Expires") && !string.IsNullOrWhiteSpace(valuePart):
                                expires = DateTime.Parse(valuePart).ToUniversalTime();
                                break;
                            case var _ when keyPart.EqualsNoCase("Max-Age") && !string.IsNullOrWhiteSpace(valuePart):
                                maxAge = int.Parse(valuePart);
                                break;
                            case var _ when keyPart.EqualsNoCase("Domain") && !string.IsNullOrWhiteSpace(valuePart):
                                domain = valuePart;
                                break;
                            case var _ when keyPart.EqualsNoCase("HttpOnly"):
                                httpOnly = true;
                                break;
                            case var _ when keyPart.EqualsNoCase("Secure"):
                                secure = true;
                                break;
                            case var _ when keyPart.EqualsNoCase("Path") && !string.IsNullOrWhiteSpace(valuePart):
                                path = valuePart;
                                break;
                        }
                    }
                }

                if (!nameAndValueParsed) throw new ArgumentException();
                return new Cookie(name, value, expires, maxAge, domain, httpOnly, secure, path);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Could not parse cookie string {cookieString}. Invalid cookie syntax. {e}");
            }
        }

        /// <summary>
        /// Tries to parse a cookie from a cookie string, for example Name=Value; Expires=Wed, 21 Oct 2015 07:28:00 GMT;
        /// and returns wether this operation was successful.
        /// </summary>
        public static bool TryParse(string cookieString, out Cookie value)
        {
            try
            {
                value = Parse(cookieString);
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        /// <inheritdoc />
        public bool Equals(Cookie other)
        {
            return string.Equals(Name, other.Name);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is Cookie cookie && Equals(cookie);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var writer = new StringBuilder();
            writer.Append(Name);
            writer.Append("=");
            writer.Append(Value);
            if (MaxAge.HasValue)
            {
                writer.Append("; Max-Age=");
                writer.Append(MaxAge.Value);
            }
            else if (Expires.HasValue)
            {
                writer.Append("; Expires=");
                writer.Append(Expires.Value.ToString("R"));
            }
            if (Domain != null)
            {
                writer.Append("; Domain= ");
                writer.Append(Domain);
            }
            if (HttpOnly)
            {
                writer.Append("; HttpOnly");
            }
            if (Secure)
            {
                writer.Append("; Secure");
            }
            if (Path != null)
            {
                writer.Append("; Path=");
                writer.Append(Path);
            }
            return writer.ToString();
        }
    }
}