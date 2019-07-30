using System.Collections.Generic;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;

namespace RESTar
{
    /// <inheritdoc />
    /// <summary>
    /// Prints a link to the RESTar documentation
    /// </summary>
    [RESTar(Method.GET, Description = "Prints a link to the RESTar documentation")]
    public class Help : ISelector<Help>
    {
        /// <summary>
        /// The URL to the RESTar documentation
        /// </summary>
        public const string DocumentationUrl = "https://develop.mopedo.com/RESTar";

        /// <summary>
        /// The property holding the documentation URL
        /// </summary>
        public string DocumentationAvailableAt { get; set; }

        /// <inheritdoc />
        public IEnumerable<Help> Select(IRequest<Help> request) => new[] {new Help {DocumentationAvailableAt = DocumentationUrl}};
    }
}