using System;

namespace RESTar.Resources
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    internal class MethodNotImplementedAttribute : Attribute { }
}