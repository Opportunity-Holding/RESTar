using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Resources;
using Starcounter;

namespace RESTar.Dynamic
{
    /// <inheritdoc />
    /// <summary>
    /// Creates and structures all the dynamic resources for this RESTar instance
    /// </summary>
    [Database]
    public class DynamicResource : IProceduralEntityResource
    {
        internal const string All = "SELECT t FROM RESTar.Dynamic.DynamicResource t";
        internal const string ByTableName = All + " WHERE t.TableName =?";

        /// <inheritdoc />
        /// <summary>
        /// The name of this resource
        /// </summary>
        public string Name { get; }

        /// <inheritdoc />
        /// <summary>
        /// The description for this resource
        /// </summary>
        public string Description { get; internal set; }

        /// <inheritdoc />
        /// <summary>
        /// The available methods for this resource
        /// </summary>
        public Method[] Methods
        {
            get => AvailableMethodsString.ToMethodsArray();
            internal set => AvailableMethodsString = value.ToMethodsString();
        }

        /// <summary>
        /// The name of the dynamic table (used internally)
        /// </summary>
        public string TableName { get; }

        /// <summary>
        /// A string representation of the available REST methods
        /// </summary>
        [RESTarMember(ignore: true)] public string AvailableMethodsString { get; private set; }

        /// <inheritdoc />
        public Type Type => DynamitControl.GetByTableName(TableName);

        internal DynamicResource(string name, Type table, IEnumerable<Method> availableMethods, string description = null)
        {
            Name = name;
            TableName = table.RESTarTypeName();
            Description = description;
            Methods = availableMethods.ResolveMethodRestrictions().ToArray();
        }
    }
}