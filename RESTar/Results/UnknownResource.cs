using System;
using System.Text.RegularExpressions;
using RESTar.Meta;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar cannot locate a resource by some search string
    /// </summary>
    public class UnknownResource : NotFound
    {
        /// <inheritdoc />
        public UnknownResource(ErrorCodes code, string info, Exception ie) : base(code, info, ie) { }

        /// <inheritdoc />
        public UnknownResource(string searchString) : base(ErrorCodes.UnknownResource,
            $"RESTar could not locate any resource by '{searchString}'. " +
            (Regex.IsMatch(searchString, @"[^\w\d\.]+") ? " Check request URI syntax." : "")) { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar cannot locate a resource of a certain kind by some search string, but found
    /// a resource of a different kind.
    /// </summary>
    internal class WrongResourceKind : NotFound
    {
        internal WrongResourceKind(string searchString, ResourceKind expected, ResourceKind found) : base(ErrorCodes.WrongResourceKind,
            $"RESTar expected to find a resource of kind {expected} by '{searchString}', but instead found a resource of kind {found}.") { }
    }
}