﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using RESTar.Results;
using Starcounter;
using static RESTar.Method;
using IResource = RESTar.Resources.IResource;

namespace RESTar.Admin
{
    /// <summary>
    /// The ResourceAlias resource is used to assign an alias to a resource, making 
    /// it possible to reference the resource with only the alias.
    /// </summary>
    [Database, RESTar(GET, DELETE, Description = description)]
    public class ResourceAlias
    {
        private const string description = "The ResourceAlias resource is used to assign an " +
                                           "alias to a resource, making it possible to reference " +
                                           "the resource with only the alias.";

        internal const string All = "SELECT t FROM RESTar.Admin.ResourceAlias t";
        internal const string ByAlias = All + " WHERE t.Alias =?";
        internal const string ByResource = All + " WHERE t.Resource =?";

        internal ResourceAlias() { }

        private string alias;

        /// <summary>
        /// The alias string
        /// </summary>
        public string Alias
        {
            get => alias;
            set
            {
                if (value[0] == '$')
                    throw new Exception($"Invalid Alias '{value}'. Aliases cannot begin with '$'");
                alias = value;
            }
        }

        internal string _resource;

        /// <summary>
        /// The name of the resource to bind the alias to
        /// </summary>
        public string Resource
        {
            get => _resource;
            set
            {
                try
                {
                    var r = RESTarConfig.ResourceByName[value];
                    _resource = r.Name;
                }
                catch (KeyNotFoundException)
                {
                    this.Delete();
                    throw new UnknownResourceForAlias(value, RESTar.Resource.Find(value));
                }
                catch
                {
                    this.Delete();
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the resource denoted by this alias
        /// </summary>
        [IgnoreDataMember] public IResource IResource => RESTarConfig.ResourceByName[Resource];

        /// <summary>
        /// Gets a ResourceAlias by its alias (case insensitive)
        /// </summary>
        public static ResourceAlias GetByAlias(string alias) => Db
            .SQL<ResourceAlias>(ByAlias, alias).FirstOrDefault();

        /// <summary>
        /// Gets a ResourceAlias by its resource name
        /// </summary>
        public static ResourceAlias GetByResource(string resourceName) => Db
            .SQL<ResourceAlias>(ByResource, resourceName).FirstOrDefault();

        /// <summary>
        /// Returns true if and only if there is an alias with this name
        /// </summary>
        public static bool Exists(string alias, out ResourceAlias resourceAlias)
        {
            resourceAlias = GetByAlias(alias);
            return resourceAlias != null;
        }

        /// <summary>
        /// </summary>
        public override string ToString() => Alias;
    }
}