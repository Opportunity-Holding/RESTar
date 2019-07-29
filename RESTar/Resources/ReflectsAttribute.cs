using System;

// ReSharper disable All
#pragma warning disable 1591

namespace RESTar.Resources
{
    /// <summary>
    /// Used to indiate that some other property/properties are a reflection of
    /// the state of this property
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DefinesAttribute : Attribute
    {
        internal string[] Terms { get; }

        public DefinesAttribute(params string[] terms) => Terms = terms;
    }
}