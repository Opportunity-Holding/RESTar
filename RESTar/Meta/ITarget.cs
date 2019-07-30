using System;
using System.Collections.Generic;
using RESTar.Requests;

namespace RESTar.Meta
{
    /// <summary>
    /// Describes the most general object of RESTar. Resources and views are ITargets
    /// </summary>
    public interface ITarget
    {
        /// <summary>
        /// The name of the target
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Descriptions are visible in the AvailableMethods resource
        /// </summary>
        string Description { get; }

        /// <summary>
        /// The target type
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// The binding rule to use when binding condition terms for this target
        /// </summary>
        TermBindingRule ConditionBindingRule { get; }

        /// <summary>
        /// The members of this target, in a case insensitive dictionary.
        /// </summary>
        IReadOnlyDictionary<string, DeclaredProperty> Members { get; }
    }

    /// <inheritdoc />
    /// <summary>
    /// The most general object of RESTar, with a generic parameter describing the 
    /// target type.
    /// </summary>
    public interface ITarget<T> : ITarget where T : class
    {
        /// <summary>
        /// RESTar selector (don't use)
        /// </summary>
        IEnumerable<T> Select(IRequest<T> request);
    }
}