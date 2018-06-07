﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Newtonsoft.Json.Linq;
using RESTar.Linq;
using RESTar.Meta.IL;
using RESTar.Resources;
using RESTar.Resources.Operations;
using Starcounter;

namespace RESTar.Meta.Internal
{
    internal static class ResourceValidator
    {
        internal static void ValidateRuntimeInsertion(Type type, string fullName, RESTarAttribute attribute)
        {
            string name;
            if (fullName != null)
            {
                if (fullName.StartsWith("RESTar.", StringComparison.OrdinalIgnoreCase) && !type.Assembly.Equals(typeof(ResourceValidator).Assembly))
                    throw new InvalidResourceDeclarationException($"Cannot add resource '{fullName}'. A resource name cannot start with 'RESTar'");
                name = fullName;
            }
            else name = type.FullName;
            if (name == null)
                throw new InvalidResourceDeclarationException(
                    "Encountered an unknown type. No further information is available.");
            if (RESTarConfig.ResourceByType.ContainsKey(type))
                throw new InvalidResourceDeclarationException(
                    $"Cannot add resource '{name}'. A resource with the same type ('{type.FullName}') has already been added to RESTar");
            if (RESTarConfig.ResourceByName.ContainsKey(name))
                throw new InvalidResourceDeclarationException(
                    $"Cannot add resource '{name}'. A resource with the same name has already been added to RESTar");
            attribute = attribute ?? type.GetCustomAttribute<RESTarAttribute>();
            if (attribute == null)
                throw new InvalidResourceDeclarationException(
                    $"Cannot add resource '{name}'. The type was not decorated with the RESTarAttribute attribute, and " +
                    "no additional attribute instance was included in the insertion.");
            Validate(type);
        }

        internal static (List<Type> regularTypes, List<Type> wrapperTypes, List<Type> terminalTypes, List<Type> binaryTypes)
            Validate(params Type[] types)
        {
            var entityTypes = types
                .Where(t => !typeof(ITerminal).IsAssignableFrom(t) && !t.ImplementsGenericInterface(typeof(Resources.IBinaryResource<>)))
                .ToList();
            var regularTypes = entityTypes
                .Where(t => !typeof(IResourceWrapper).IsAssignableFrom(t))
                .ToList();
            var wrapperTypes = entityTypes
                .Where(t => typeof(IResourceWrapper).IsAssignableFrom(t))
                .ToList();
            var terminalTypes = types
                .Where(t => typeof(ITerminal).IsAssignableFrom(t))
                .ToList();
            var binaryTypes = types
                .Where(t => t.ImplementsGenericInterface(typeof(Resources.IBinaryResource<>)))
                .ToList();

            void ValidateCommon(Type type)
            {
                #region Check general stuff

                if (type.FullName == null)
                    throw new InvalidResourceDeclarationException(
                        "Encountered an unknown type. No further information is available.");

                if (type.IsGenericTypeDefinition)
                    throw new InvalidResourceDeclarationException(
                        $"Found a generic resource type '{type.FullName}'. RESTar resource types must be non-generic");

                if (type.FullName.Count(c => c == '+') >= 2)
                    throw new InvalidResourceDeclarationException($"Invalid resource '{type.RESTarTypeName()}'. " +
                                                                  "Inner resources cannot have their own inner resources");

                if (type.HasAttribute<RESTarViewAttribute>())
                    throw new InvalidResourceDeclarationException(
                        $"Invalid resource type '{type.RESTarTypeName()}'. Resource types cannot be " +
                        "decorated with the 'RESTarViewAttribute'");
                if (type.Namespace == null)
                    throw new InvalidResourceDeclarationException($"Invalid type '{type.RESTarTypeName()}'. Unknown namespace");

                if (RESTarConfig.ReservedNamespaces.Contains(type.Namespace.ToLower()) &&
                    type.Assembly != typeof(RESTarConfig).Assembly)
                    throw new InvalidResourceDeclarationException(
                        $"Invalid namespace for resource type '{type.RESTarTypeName()}'. Namespace '{type.Namespace}' is reserved by RESTar");

                if ((!type.IsClass || !type.IsPublic && !type.IsNestedPublic) && type.Assembly != typeof(Resource).Assembly)
                    throw new InvalidResourceDeclarationException(
                        $"Invalid type '{type.RESTarTypeName()}'. Resource types must be public classes");

                if (type.HasAttribute<RESTarAttribute>(out var a) && a.Interface is Type interfaceType)
                {
                    if (!interfaceType.IsInterface)
                        throw new InvalidResourceDeclarationException(
                            $"Invalid Interface of type '{interfaceType.RESTarTypeName()}' assigned to resource '{type.RESTarTypeName()}'. " +
                            "Type is not an interface");

                    if (interfaceType.GetProperties()
                        .Select(p => p.Name)
                        .ContainsDuplicates(StringComparer.OrdinalIgnoreCase, out var interfacePropDupe))
                        throw new InvalidResourceMemberException(
                            $"Invalid Interface of type '{interfaceType.RESTarTypeName()}' assigned to resource '{type.RESTarTypeName()}'. " +
                            $"Interface contained properties with duplicate names matching '{interfacePropDupe}' (case insensitive).");

                    var interfaceName = interfaceType.RESTarTypeName();
                    type.GetInterfaceMap(interfaceType).TargetMethods.ForEach(method =>
                    {
                        if (!method.IsSpecialName) return;
                        var interfaceProperty = interfaceType
                            .GetProperties()
                            .First(p => p.GetGetMethod()?.Name is string getname && method.Name.EndsWith(getname) ||
                                        p.GetSetMethod()?.Name is string setname && method.Name.EndsWith(setname));

                        Type propertyType = null;
                        if (method.IsPrivate && method.Name.StartsWith($"{interfaceName}.get_") || method.Name.StartsWith("get_"))
                            propertyType = method.ReturnType;
                        else if (method.IsPrivate && method.Name.StartsWith($"{interfaceName}.set_") || method.Name.StartsWith("set_"))
                            propertyType = method.GetParameters()[0].ParameterType;

                        if (propertyType == null)
                            throw new InvalidResourceDeclarationException(
                                $"Invalid implementation of interface '{interfaceType.RESTarTypeName()}' assigned to resource '{type.FullName}'. " +
                                $"Unable to determine the type for interface property '{interfaceProperty.Name}'");

                        PropertyInfo calledProperty;
                        if (method.Name.StartsWith($"{interfaceName}.get_"))
                        {
                            calledProperty = method.GetInstructions()
                                .Select(i => i.OpCode == OpCodes.Call && i.Operand is MethodInfo calledMethod && method.IsSpecialName
                                    ? type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                        .FirstOrDefault(p => p.GetGetMethod() == calledMethod)
                                    : null)
                                .LastOrDefault(p => p != null);
                        }
                        else if (method.Name.StartsWith($"{interfaceName}.set_"))
                        {
                            calledProperty = method.GetInstructions()
                                .Select(i => i.OpCode == OpCodes.Call && i.Operand is MethodInfo calledMethod && method.IsSpecialName
                                    ? type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                        .FirstOrDefault(p => p.GetSetMethod() == calledMethod)
                                    : null)
                                .LastOrDefault(p => p != null);
                        }
                        else return;

                        if (calledProperty == null)
                            throw new InvalidResourceDeclarationException(
                                $"Invalid implementation of interface '{interfaceType.RESTarTypeName()}' assigned to resource '{type.RESTarTypeName()}'. " +
                                $"RESTar was unable to determine which property of '{type.RESTarTypeName()}' that is exposed by interface " +
                                $"property '{interfaceProperty.Name}'. For getters, RESTar will look for the last IL instruction " +
                                "in the method body that fetches a property value from the resource type. For setters, RESTar will look " +
                                "for the last IL instruction in the method body that sets a property value in the resource type.");

                        if (calledProperty.PropertyType != propertyType)
                            throw new InvalidResourceDeclarationException(
                                $"Invalid implementation of interface '{interfaceType.RESTarTypeName()}' assigned to resource '{type.RESTarTypeName()}'. " +
                                $"RESTar matched interface property '{interfaceProperty.Name}' with resource property '{calledProperty.Name}' " +
                                "using the interface property matching rules, but these properties have a type mismatch. Expected " +
                                $"'{calledProperty.PropertyType.RESTarTypeName()}' but found '{propertyType.RESTarTypeName()}' in interface");
                    });
                }

                #endregion

                #region Check for invalid IDictionary implementation

                var validTypes = new[] {typeof(string), typeof(object)};
                if (type.ImplementsGenericInterface(typeof(IDictionary<,>), out var typeParams)
                    && !type.IsSubclassOf(typeof(JObject))
                    && !typeParams.SequenceEqual(validTypes))
                    throw new InvalidResourceDeclarationException(
                        $"Invalid resource declaration for type '{type.RESTarTypeName()}'. All resource types implementing " +
                        "the generic 'System.Collections.Generic.IDictionary`2' interface must either be subclasses of " +
                        "Newtonsoft.Json.Linq.JObject or have System.String as first type parameter and System.Object as " +
                        $"second type parameter. Found {typeParams[0].RESTarTypeName()} and {typeParams[1].RESTarTypeName()}");

                #endregion

                #region Check for invalid IEnumerable implementation

                if ((type.ImplementsGenericInterface(typeof(IEnumerable<>)) || typeof(IEnumerable).IsAssignableFrom(type)) &&
                    !type.ImplementsGenericInterface(typeof(IDictionary<,>)))
                    throw new InvalidResourceDeclarationException(
                        $"Invalid resource declaration for type '{type.RESTarTypeName()}'. The type has an invalid imple" +
                        $"mentation of an IEnumerable interface. The resource '{type.RESTarTypeName()}' (or any of its base types) " +
                        "cannot implement the \'System.Collections.Generic.IEnumerable`1\' or \'System.Collections.IEnume" +
                        "rable\' interfaces without also implementing the \'System.Collections.Generic.IDictionary`2\' interface."
                    );

                #endregion

                #region Check for public instance fields

                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                if (fields.Any())
                    throw new InvalidResourceMemberException(
                        $"A RESTar resource cannot have public instance fields, only properties. Resource: '{type.RESTarTypeName()}' had " +
                        $"fields: {string.Join(", ", fields.Select(f => $"'{f.Name}'"))} in resource '{type.RESTarTypeName()}'"
                    );

                #endregion

                #region Check for properties with duplicate case insensitive names

                if (type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => !p.RESTarIgnored())
                    .Where(p => !(p.DeclaringType.ImplementsGenericInterface(typeof(IDictionary<,>)) && p.Name == "Item"))
                    .Select(p => p.RESTarMemberName().ToLower())
                    .ContainsDuplicates(out var duplicate))
                    throw new InvalidResourceMemberException(
                        $"Invalid properties for resource '{type.RESTarTypeName()}'. Names of public instance properties declared " +
                        $"for a virtual resource must be unique (case insensitive). Two or more property names evaluated to {duplicate}."
                    );

                #endregion
            }

            void ValidateEntityDeclarations(List<Type> regularResources)
            {
                foreach (var type in regularResources)
                    ValidateCommon(type);
            }

            void ValidateWrapperDeclaration(List<Type> wrappers)
            {
                if (wrappers.Select(w => w.GetWrappedType()).ContainsDuplicates(out var wrapperDupe))
                    throw new InvalidResourceWrapperException("RESTar found multiple RESTar.ResourceWrapper declarations for " +
                                                              $"type '{wrapperDupe.RESTarTypeName()}'. A type can only be wrapped once.");
                foreach (var wrapper in wrappers)
                {
                    var members = wrapper.GetMembers(BindingFlags.Public | BindingFlags.Instance);
                    if (members.OfType<PropertyInfo>().Any() || members.OfType<FieldInfo>().Any())
                        throw new InvalidResourceWrapperException(
                            $"Invalid RESTar.ResourceWrapper '{wrapper.RESTarTypeName()}'. ResourceWrapper " +
                            "classes cannot contain public instance properties or fields");
                    ValidateCommon(wrapper);
                    var wrapped = wrapper.GetWrappedType();
                    if (wrapped.HasResourceProviderAttribute())
                        throw new InvalidResourceWrapperException(
                            $"Invalid RESTar.ResourceWrapper '{wrapper.RESTarTypeName()}' for wrapped " +
                            $"type '{wrapped.RESTarTypeName()}'. Type decorated with a resource provider's " +
                            "attribute cannot be wrapped. Resource provider attributes should be " +
                            "placed on the wrapper type.");
                    if (wrapper.GetInterfaces()
                        .Where(i => typeof(IOperationsInterface).IsAssignableFrom(i))
                        .Any(i => i.IsGenericType && i.GenericTypeArguments[0] != wrapped))
                        throw new InvalidResourceWrapperException(
                            $"Invalid RESTar.ResourceWrapper '{wrapper.RESTarTypeName()}'. This wrapper " +
                            "cannot implement operations interfaces for types other than " +
                            $"'{wrapped.RESTarTypeName()}'.");
                    if (wrapped.FullName?.Contains("+") == true)
                        throw new InvalidResourceWrapperException($"Invalid RESTar.ResourceWrapper '{wrapper.RESTarTypeName()}'. Cannot " +
                                                                  "wrap types that are declared within the scope of some other class.");
                    if (wrapped.HasAttribute<RESTarAttribute>())
                        throw new InvalidResourceWrapperException("RESTar found a RESTar.ResourceWrapper declaration for type " +
                                                                  $"'{wrapped.RESTarTypeName()}', a type that is already a RESTar " +
                                                                  "resource type. Only non-resource types can be wrapped.");
                    if (wrapper.Namespace == null)
                        throw new InvalidResourceDeclarationException($"Invalid type '{wrapper.RESTarTypeName()}'. Unknown namespace");
                    if (wrapper.Assembly == typeof(RESTarConfig).Assembly)
                        throw new InvalidResourceWrapperException("RESTar found an invalid RESTar.ResourceWrapper declaration for " +
                                                                  $"type '{wrapped.RESTarTypeName()}'. RESTar types cannot be wrapped.");
                }
            }

            void ValidateTerminalDeclarations(List<Type> terminals)
            {
                foreach (var terminal in terminals)
                {
                    ValidateCommon(terminal);

                    if (terminal.ImplementsGenericInterface(typeof(IEnumerable<>)))
                        throw new InvalidTerminalDeclarationException(terminal, "must not be collections");
                    if (terminal.HasResourceProviderAttribute())
                        throw new InvalidTerminalDeclarationException(terminal, "must not be decorated with a resource provider attribute");
                    if (terminal.HasAttribute<DatabaseAttribute>())
                        throw new InvalidTerminalDeclarationException(terminal,
                            "must not be decorated with the Starcounter.DatabaseAttribute attribute");
                    if (typeof(IOperationsInterface).IsAssignableFrom(terminal))
                        throw new InvalidTerminalDeclarationException(terminal, "must not implement any other RESTar operations interfaces");
                    if (terminal.GetConstructor(Type.EmptyTypes) == null)
                        throw new InvalidTerminalDeclarationException(terminal, "must define a public parameterless constructor");
                }
            }

            void ValidateBinaryDeclarations(List<Type> binaries)
            {
                foreach (var binary in binaries)
                {
                    ValidateCommon(binary);
                    if (binary.ImplementsGenericInterface(typeof(IEnumerable<>)))
                        throw new InvalidBinaryDeclarationException(binary, "must not be collections");
                    if (binary.HasResourceProviderAttribute())
                        throw new InvalidBinaryDeclarationException(binary, "must not be decorated with a resource provider attribute");
                    if (binary.HasAttribute<DatabaseAttribute>())
                        throw new InvalidBinaryDeclarationException(binary,
                            "must not be decorated with the Starcounter.DatabaseAttribute attribute");
                    if (typeof(IOperationsInterface).IsAssignableFrom(binary))
                        throw new InvalidBinaryDeclarationException(binary, "must not implement any other RESTar operations interfaces");
                }
            }

            ValidateEntityDeclarations(entityTypes);
            ValidateWrapperDeclaration(wrapperTypes);
            ValidateTerminalDeclarations(terminalTypes);
            ValidateBinaryDeclarations(binaryTypes);

            return (regularTypes, wrapperTypes, terminalTypes, binaryTypes);
        }
    }
}