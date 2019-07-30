using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RESTar.ContentTypeProviders;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
using Starcounter.Nova;

namespace RESTar.Meta.Internal
{
    /// <inheritdoc />
    /// <summary>
    /// Special properties are properties that are not strictly members, but still
    /// important parts of class definitions. For example Starcounter ObjectID and 
    /// ObjectNo.
    /// </summary>
    internal class SpecialProperty : DeclaredProperty
    {
        internal JsonProperty JsonProperty => new JsonProperty
        {
            PropertyType = Type,
            PropertyName = Name,
            Readable = IsReadable,
            Writable = IsWritable,
            ValueProvider = new DefaultValueProvider(this),
            ObjectCreationHandling = ReplaceOnUpdate ? ObjectCreationHandling.Replace : ObjectCreationHandling.Reuse,
            NullValueHandling = HiddenIfNull ? NullValueHandling.Ignore : NullValueHandling.Include,
            Order = Order
        };

        private SpecialProperty(int metadataToken, string name, string actualName, Type type, int? order, bool isScQueryable,
            bool hidden, bool hiddenIfNull, Type owner, Getter getter) : base
        (
            metadataToken: metadataToken,
            name: name,
            actualName: actualName,
            type: type,
            order: order,
            isScQueryable: isScQueryable,
            attributes: new[] {new KeyAttribute()},
            skipConditions: false,
            hidden: hidden,
            hiddenIfNull: hiddenIfNull,
            isEnum: false,
            allowedConditionOperators: Operators.All,
            customDateTimeFormat: null,
            getter: getter,
            owner: owner,
            setter: null
        ) { }

        internal static IEnumerable<SpecialProperty> GetObjectNo(bool flag, Type declaredIn)
        {
            if (flag) yield return FlaggedObjectNo(declaredIn);
            else yield return ObjectNo(declaredIn);
        }

        // ReSharper disable PossibleNullReferenceException

        private static readonly int ObjectNoMetadataToken =
            typeof(Db).GetMethod(nameof(Db.GetOid), new[] {typeof(object)}).MetadataToken;


        // ReSharper restore PossibleNullReferenceException

        /// <summary>
        /// A property describing the ObjectNo of a class
        /// </summary>
        private static SpecialProperty ObjectNo(Type declaredIn) => new SpecialProperty
        (
            metadataToken: ObjectNoMetadataToken,
            name: "ObjectNo",
            actualName: "ObjectNo",
            type: typeof(ulong),
            order: int.MaxValue - 1,
            isScQueryable: true,
            hidden: false,
            hiddenIfNull: false,
            owner: declaredIn,
            getter: t => Do.TryAndThrow(() => Db.GetOid(t), "Could not get ObjectNo from non-Starcounter resource.")
        );

        /// <summary>
        /// A property describing the ObjectNo of a class
        /// </summary>
        private static SpecialProperty FlaggedObjectNo(Type declaredIn) => new SpecialProperty
        (
            metadataToken: ObjectNoMetadataToken,
            name: "$ObjectNo",
            actualName: "ObjectNo",
            type: typeof(ulong),
            order: int.MaxValue - 1,
            isScQueryable: true,
            hidden: false,
            hiddenIfNull: false,
            owner: declaredIn,
            getter: t => Do.TryAndThrow(() => Db.GetOid(t), "Could not get ObjectNo from non-Starcounter resource.")
        );
    }
}