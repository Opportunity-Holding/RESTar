using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using RESTar.Internal.Auth;
using RESTar.Linq;
using RESTar.Meta;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;

namespace RESTar
{
    /// <summary>
    /// The levels of metadata that can be used by the Metadata resource
    /// </summary>
    public enum MetadataLevel
    {
        /// <summary>
        /// Only resource lists (EntityResources and TerminalResources) are populated
        /// </summary>
        OnlyResources,

        /// <summary>
        /// Resource lists and type lists (including members) are populated. Type lists cover the 
        /// entire type tree (types used in resource types, etc.)
        /// </summary>
        Full
    }

    /// <inheritdoc />
    /// <summary>
    /// A resource that creates metadata for the types and resources of the RESTar instance,
    /// using the types included in the RESTar.Meta namespace.
    /// </summary>
    [RESTar(Method.GET, GETAvailableToAll = true)]
    public class Metadata : ISelector<Metadata>
    {
        /// <summary>
        /// The access scope for the current client
        /// </summary>
        public IDictionary<IResource, Method[]> CurrentAccessScope { get; private set; }

        /// <summary>
        /// The entity resources within the access scope
        /// </summary>
        public IEntityResource[] EntityResources { get; private set; }

        /// <summary>
        /// The terminal resources within the access scope
        /// </summary>
        public ITerminalResource[] TerminalResources { get; private set; }

        /// <summary>
        /// The type list containing all entity resource types
        /// </summary>
        public IReadOnlyDictionary<Type, Member[]> EntityResourceTypes { get; private set; }

        /// <summary>
        /// The type list containing all peripheral types, types that are 
        /// referenced by some entity resource type or peripheral type.
        /// </summary>
        public IReadOnlyDictionary<Type, Member[]> PeripheralTypes { get; private set; }

        /// <inheritdoc />
        public IEnumerable<Metadata> Select(IRequest<Metadata> request)
        {
            var accessrights = request.Context.Client.AccessRights;
            return new[] {Make(MetadataLevel.Full, accessrights)};
        }

        /// <summary>
        /// Generates metadata according to a given metadata level
        /// </summary>
        public static Metadata Get(MetadataLevel level) => Make(level, null);

        internal static Metadata Make(MetadataLevel level, AccessRights rights)
        {
            var domain = rights?.Keys ?? RESTarConfig.Resources;
            var entityResources = domain
                .OfType<IEntityResource>()
                .Where(r => r.IsGlobal)
                .OrderBy(r => r.Name)
                .ToList();
            var terminalResources = domain
                .OfType<ITerminalResource>()
                .ToList();

            if (level == MetadataLevel.OnlyResources)
                return new Metadata
                {
                    CurrentAccessScope = new Dictionary<IResource, Method[]>(rights ?? AccessRights.Root),
                    EntityResources = entityResources.ToArray(),
                    TerminalResources = terminalResources.ToArray()
                };

            var checkedTypes = new HashSet<Type>();

            void parseType(Type type)
            {
                switch (type)
                {
                    case var _ when type.IsEnum:
                        checkedTypes.Add(type);
                        break;
                    case var _ when type.IsNullable(out var baseType):
                        parseType(baseType);
                        break;
                    case var _ when type.ImplementsGenericInterface(typeof(IEnumerable<>), out var param) && param.Any():
                        if (param[0].ImplementsGenericInterface(typeof(IEnumerable<>)))
                            break;
                        parseType(param[0]);
                        break;

                    case var _ when type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>):
                    case var _ when IsPrimitive(type):
                    case var _ when type == typeof(object):
                        break;

                    case var _ when checkedTypes.Add(type):
                        type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => !p.RESTarIgnored())
                            .Select(p => p.FieldType)
                            .ForEach(parseType);
                        type.GetDeclaredProperties().Values
                            .Where(p => !p.Hidden)
                            .Select(p => p.Type)
                            .ForEach(parseType);
                        break;
                }
            }

            var entityTypes = entityResources.Select(r => r.Type).ToList();
            var terminalTypes = terminalResources.Select(r => r.Type).ToList();
            entityTypes.ForEach(parseType);
            checkedTypes.ExceptWith(entityTypes);
            terminalTypes.ForEach(parseType);
            checkedTypes.ExceptWith(terminalTypes);

            return new Metadata
            {
                CurrentAccessScope = new Dictionary<IResource, Method[]>(rights ?? AccessRights.Root),
                EntityResources = entityResources.ToArray(),
                TerminalResources = terminalResources.ToArray(),
                EntityResourceTypes = new ReadOnlyDictionary<Type, Member[]>(entityTypes.ToDictionary(t => t, type =>
                    type.GetDeclaredProperties().Values.Cast<Member>().ToArray())),
                PeripheralTypes = new ReadOnlyDictionary<Type, Member[]>(checkedTypes.ToDictionary(t => t, type =>
                {
                    var props = type.GetDeclaredProperties().Values;
                    var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => !p.RESTarIgnored())
                        .Select(f => new Field(f));
                    return props.Union<Member>(fields).ToArray();
                }))
            };
        }

        private Metadata() { }

        private static bool IsPrimitive(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Empty:
                case TypeCode.DBNull:
                case TypeCode.Boolean:
                case TypeCode.Char:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                case TypeCode.DateTime:
                case TypeCode.String: return true;
                default: return false;
            }
        }
    }
}