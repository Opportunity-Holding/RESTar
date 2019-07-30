using System.Collections.Generic;
using System.Linq;
using RESTar.Internal;
using RESTar.Meta;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when a condition operator was not supported for a given resource property (expressed in the default protocol)
    /// search string.
    /// </summary>
    internal class BadConditionOperator : BadRequest
    {
        internal BadConditionOperator(ITerminalResource terminal, Operator found) : base(ErrorCodes.InvalidConditionOperator,
            $"Invalid operator '{found.Common}' in condition to terminal resource '{terminal.Name}'. Only \'=\' is valid in terminal conditions.") { }

        internal BadConditionOperator(string c, ITarget target, Operator found, Term term, IEnumerable<Operator> allowed)
            : base(ErrorCodes.InvalidConditionOperator, $"Forbidden operator for condition '{c}'. '{found}' is not allowed when " +
                                                        $"comparing against '{term.Key}' in type '{target.Name}'. Allowed operators" +
                                                        $": {string.Join(", ", allowed.Select(a => $"'{a.Common}'"))}") { }
    }
}