using System;
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
            else name = type.GetRESTarTypeName();
            if (name == null)
                throw new InvalidResourceDeclarationException(
                    "Encountered an unknown type. No further information is available.");
            if (RESTarConfig.ResourceByType.ContainsKey(type))
                throw new InvalidResourceDeclarationException(
                    $"Cannot add resource '{name}'. A resource with the same type ('{type.GetRESTarTypeName()}') has already been added to RESTar");
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

        internal static (List<Type> regular, List<Type> wrappers, List<Type> terminals, List<Type> binaries, List<Type> events)
            Validate(params Type[] types)
        {
            var entityTypes = types
                .Where(t => !typeof(ITerminal).IsAssignableFrom(t) &&
                            !typeof(IEvent).IsAssignableFrom(t) &&
                            !t.ImplementsGenericInterface(typeof(IBinary<>)))
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
                .Where(t => t.ImplementsGenericInterface(typeof(IBinary<>)))
                .ToList();
            var eventTypes = types
                .Where(t => !t.IsAbstract && typeof(IEvent).IsAssignableFrom(t))
                .ToList();

            void ValidateCommon(Type type)
            {
                #region Check general stuff

                if (type.FullName == null)
                    throw new InvalidResourceDeclarationException(
                        "Encountered an unknown type. No further information is available.");

                if (type.IsGenericTypeDefinition)
                    throw new InvalidResourceDeclarationException(
                        $"Found a generic resource type '{type.GetRESTarTypeName()}'. RESTar resource types must be non-generic");

                if (type.FullName.Count(c => c == '+') >= 2)
                    throw new InvalidResourceDeclarationException($"Invalid resource '{type.GetRESTarTypeName()}'. " +
                                                                  "Inner resources cannot have their own inner resources");

                if (type.HasAttribute<RESTarViewAttribute>())
                    throw new InvalidResourceDeclarationException(
                        $"Invalid resource type '{type.GetRESTarTypeName()}'. Resource types cannot be " +
                        "decorated with the 'RESTarViewAttribute'");

                if (type.Namespace == null)
                    throw new InvalidResourceDeclarationException($"Invalid type '{type.GetRESTarTypeName()}'. Unknown namespace");

                if (RESTarConfig.ReservedNamespaces.Contains(type.Namespace.ToLower()) &&
                    type.Assembly != typeof(RESTarConfig).Assembly)
                    throw new InvalidResourceDeclarationException(
                        $"Invalid namespace for resource type '{type.GetRESTarTypeName()}'. Namespace '{type.Namespace}' is reserved by RESTar");

                if ((!type.IsClass || !type.IsPublic && !type.IsNestedPublic) && type.Assembly != typeof(Resource).Assembly)
                    throw new InvalidResourceDeclarationException(
                        $"Invalid type '{type.GetRESTarTypeName()}'. Resource types must be public classes");

                if (type.GetRESTarInterfaceType() is Type interfaceType)
                {
                    if (!interfaceType.IsInterface)
                        throw new InvalidResourceDeclarationException(
                            $"Invalid Interface of type '{interfaceType.GetRESTarTypeName()}' assigned to resource '{type.GetRESTarTypeName()}'. " +
                            "Type is not an interface");

                    if (interfaceType.GetProperties()
                        .Select(p => p.Name)
                        .ContainsDuplicates(StringComparer.OrdinalIgnoreCase, out var interfacePropDupe))
                        throw new InvalidResourceMemberException(
                            $"Invalid Interface of type '{interfaceType.GetRESTarTypeName()}' assigned to resource '{type.GetRESTarTypeName()}'. " +
                            $"Interface contained properties with duplicate names matching '{interfacePropDupe}' (case insensitive).");

                    var interfaceName = interfaceType.GetRESTarTypeName();
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
                                $"Invalid implementation of interface '{interfaceType.GetRESTarTypeName()}' assigned to resource '{type.GetRESTarTypeName()}'. " +
                                $"Unable to determine the type for interface property '{interfaceProperty.Name}'");

                        PropertyInfo projectedProperty;
                        if (method.Name.StartsWith($"{interfaceName}.get_"))
                        {
                            projectedProperty = method.GetInstructions()
                                .Select(i => i.OpCode == OpCodes.Call && i.Operand is MethodInfo calledMethod && method.IsSpecialName
                                    ? type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                        .FirstOrDefault(p => p.GetGetMethod() == calledMethod)
                                    : null)
                                .LastOrDefault(p => p != null);
                        }
                        else if (method.Name.StartsWith($"{interfaceName}.set_"))
                        {
                            projectedProperty = method.GetInstructions()
                                .Select(i => i.OpCode == OpCodes.Call && i.Operand is MethodInfo calledMethod && method.IsSpecialName
                                    ? type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                        .FirstOrDefault(p => p.GetSetMethod() == calledMethod)
                                    : null)
                                .LastOrDefault(p => p != null);
                        }
                        else return;

                        if (projectedProperty == null)
                            throw new InvalidResourceDeclarationException(
                                $"Invalid implementation of interface '{interfaceType.GetRESTarTypeName()}' assigned to resource '{type.GetRESTarTypeName()}'. " +
                                $"RESTar was unable to determine which property of '{type.GetRESTarTypeName()}' that is exposed by interface " +
                                $"property '{interfaceProperty.Name}'. For getters, RESTar will look for the last IL instruction " +
                                "in the method body that fetches a property value from the resource type. For setters, RESTar will look " +
                                "for the last IL instruction in the method body that sets a property value in the resource type.");

                        if (projectedProperty.PropertyType != propertyType)
                            throw new InvalidResourceDeclarationException(
                                $"Invalid implementation of interface '{interfaceType.GetRESTarTypeName()}' assigned to resource '{type.GetRESTarTypeName()}'. " +
                                $"RESTar matched interface property '{interfaceProperty.Name}' with resource property '{projectedProperty.Name}' " +
                                "using the interface property matching rules, but these properties have a type mismatch. Expected " +
                                $"'{projectedProperty.PropertyType.GetRESTarTypeName()}' but found '{propertyType.GetRESTarTypeName()}' in interface");
                    });
                }

                #endregion

                #region Check for invalid IDictionary implementation

                var validTypes = new[] {typeof(string), typeof(object)};
                if (type.ImplementsGenericInterface(typeof(IDictionary<,>), out var typeParams)
                    && !type.IsSubclassOf(typeof(JObject))
                    && !typeParams.SequenceEqual(validTypes))
                    throw new InvalidResourceDeclarationException(
                        $"Invalid resource declaration for type '{type.GetRESTarTypeName()}'. All resource types implementing " +
                        "the generic 'System.Collections.Generic.IDictionary`2' interface must either be subclasses of " +
                        "Newtonsoft.Json.Linq.JObject or have System.String as first type parameter and System.Object as " +
                        $"second type parameter. Found {typeParams[0].GetRESTarTypeName()} and {typeParams[1].GetRESTarTypeName()}");

                #endregion

                #region Check for invalid IEnumerable implementation

                if ((type.ImplementsGenericInterface(typeof(IEnumerable<>)) || typeof(IEnumerable).IsAssignableFrom(type)) &&
                    !type.ImplementsGenericInterface(typeof(IDictionary<,>)))
                    throw new InvalidResourceDeclarationException(
                        $"Invalid resource declaration for type '{type.GetRESTarTypeName()}'. The type has an invalid imple" +
                        $"mentation of an IEnumerable interface. The resource '{type.GetRESTarTypeName()}' (or any of its base types) " +
                        "cannot implement the \'System.Collections.Generic.IEnumerable`1\' or \'System.Collections.IEnume" +
                        "rable\' interfaces without also implementing the \'System.Collections.Generic.IDictionary`2\' interface."
                    );

                #endregion

                #region Check for public instance fields

                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                if (fields.Any())
                    throw new InvalidResourceMemberException(
                        $"A RESTar resource cannot have public instance fields, only properties. Resource: '{type.GetRESTarTypeName()}' had " +
                        $"fields: {string.Join(", ", fields.Select(f => $"'{f.Name}'"))} in resource '{type.GetRESTarTypeName()}'"
                    );

                #endregion

                #region Check for properties with duplicate case insensitive names

                if (type.FindAndParseDeclaredProperties().ContainsDuplicates(DeclaredProperty.NameComparer, out var duplicate))
                    throw new InvalidResourceMemberException(
                        $"Invalid properties for resource '{type.GetRESTarTypeName()}'. Names of public instance properties must " +
                        $"be unique (case insensitive). Two or more property names were equivalent to '{duplicate.Name}'."
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
                if (wrappers.Select(type => (type, wrapped: type.GetWrappedType())).ContainsDuplicates(pair => pair.wrapped, out var dupe))
                    throw new InvalidResourceWrapperException(dupe, "must wrap unique types. Found multiple wrapper declarations for " +
                                                                    $"wrapped type '{dupe.wrapped.GetRESTarTypeName()}'.");

                foreach (var wrapper in wrappers)
                {
                    var wrapped = wrapper.GetWrappedType();
                    var _types = (wrapper, wrapped);
                    var members = wrapper.GetMembers(BindingFlags.Public | BindingFlags.Instance);
                    if (members.OfType<PropertyInfo>().Any() || members.OfType<FieldInfo>().Any())
                        throw new InvalidResourceWrapperException(_types, "cannot contain public instance properties or fields");
                    ValidateCommon(wrapper);
                    if (wrapper.GetInterfaces()
                        .Where(i => typeof(IOperationsInterface).IsAssignableFrom(i))
                        .Any(i => i.IsGenericType && i.GenericTypeArguments[0] != wrapped))
                        throw new InvalidResourceWrapperException(_types, "cannot implement operations interfaces for types other than " +
                                                                          $"'{wrapped.GetRESTarTypeName()}'.");
                    if (wrapped.FullName?.Contains("+") == true)
                        throw new InvalidResourceWrapperException(_types, "cannot wrap types that are declared within the scope of some other class.");
                    if (wrapped.HasAttribute<RESTarAttribute>())
                        throw new InvalidResourceWrapperException(_types, "cannot wrap types already decorated with the 'RESTarAttribute' attribute");
                    if (wrapper.Assembly == typeof(RESTarConfig).Assembly)
                        throw new InvalidResourceWrapperException(_types, "cannot wrap RESTar types");
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
                    if (terminal.IsStarcounterDatabaseType())
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
                    if (binary.IsStarcounterDatabaseType())
                        throw new InvalidBinaryDeclarationException(binary, "must not be decorated with the 'Starcounter.DatabaseAttribute' attribute");
                    if (typeof(IOperationsInterface).IsAssignableFrom(binary))
                        throw new InvalidBinaryDeclarationException(binary, "must not implement any other RESTar operations interfaces");
                }
            }

            void ValidateEventDeclarations(List<Type> events)
            {
                foreach (var @event in events)
                {
                    ValidateCommon(@event);
                    if (!typeof(IEvent).IsAssignableFrom(@event))
                        throw new InvalidEventDeclarationException(@event, "must inherit from 'RESTar.Resources.Event<T>'");
                    if (@event.ImplementsGenericInterface(typeof(IEnumerable<>)))
                        throw new InvalidEventDeclarationException(@event, "must not be collections");
                    if (@event.HasResourceProviderAttribute())
                        throw new InvalidEventDeclarationException(@event, "must not be decorated with a resource provider attribute");
                    if (@event.IsStarcounterDatabaseType())
                        throw new InvalidEventDeclarationException(@event, "must not be decorated with the 'Starcounter.DatabaseAttribute' attribute");
                    if (typeof(IOperationsInterface).IsAssignableFrom(@event))
                        throw new InvalidEventDeclarationException(@event, "must not implement any RESTar operations interfaces");
                }
            }

            ValidateEntityDeclarations(entityTypes);
            ValidateWrapperDeclaration(wrapperTypes);
            ValidateTerminalDeclarations(terminalTypes);
            ValidateBinaryDeclarations(binaryTypes);
            ValidateEventDeclarations(eventTypes);

            return (regularTypes, wrapperTypes, terminalTypes, binaryTypes, eventTypes);
        }
    }
}