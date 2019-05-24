using System;

// ReSharper disable All
#pragma warning disable 1591

namespace RESTar.Resources
{
    /// <summary>
    /// Used to indiate that a property reflects the state of another property
    /// </summary>
    public class ReflectsAttribute : Attribute
    {
        internal string[] ReflectedPropertyNames { get; }

        public ReflectsAttribute(params string[] reflectedPropertyNames)
        {
            ReflectedPropertyNames = reflectedPropertyNames;
        }
    }
}