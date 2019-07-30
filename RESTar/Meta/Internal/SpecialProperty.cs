using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RESTar.ContentTypeProviders;
using RESTar.Requests;
using RESTar.Resources;

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
    }
}