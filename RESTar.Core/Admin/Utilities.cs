using System.Collections.Generic;
using RESTar.Resources;
using RESTar.Resources.Templates;

namespace RESTar.Admin
{
    /// <inheritdoc cref="OptionsTerminal" />
    /// <summary>
    /// Provides access to commmon admin tasks
    /// </summary>
    [RESTar(Description = description)]
    internal class Utilities : OptionsTerminal
    {
        private const string description = "The Utilities resource gives access to commonly used " +
                                           "tools for administrating a RESTar instance, in form of " +
                                           "views that can be used as static methods.";

        protected override IEnumerable<Option> GetOptions() => new[]
        {
            new Option
            (
                command: "ReloadConfigFile",
                description: "Reloads the configuration file and updates the access rights for API keys",
                action: args => RESTarConfig.UpdateConfiguration()
            )
        };
    }
}