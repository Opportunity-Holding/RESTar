﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTar.Linq;
using RESTar.Resources;

namespace RESTar.Meta.Internal
{
    internal class TerminalResourceProvider
    {
        internal void RegisterTerminalTypes(List<Type> terminalTypes)
        {
            terminalTypes.OrderBy(t => t.RESTarTypeName()).ForEach(type =>
            {
                var resource = (IResource) BuildTerminalMethod.MakeGenericMethod(type).Invoke(this, null);
                RESTarConfig.AddResource(resource);
            });
            Shell.TerminalResource = Meta.TerminalResource<Shell>.Get;
        }

        internal TerminalResourceProvider() => BuildTerminalMethod =
            typeof(TerminalResourceProvider).GetMethod(nameof(MakeTerminalResource), BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly MethodInfo BuildTerminalMethod;
        private IResource MakeTerminalResource<T>() where T : class, ITerminal => new TerminalResource<T>();
    }
}