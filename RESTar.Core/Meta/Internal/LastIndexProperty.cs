using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RESTar.Requests;

namespace RESTar.Meta.Internal
{
    internal class LastIndexProperty : DeclaredProperty
    {
        internal LastIndexProperty(Type type, bool collectionReadonly, Type owner) : base
        (
            metadataToken: "-".GetHashCode(),
            name: "-",
            actualName: "-",
            type: type,
            order: null,
            isScQueryable: false,
            attributes: new Attribute[0],
            skipConditions: false,
            hidden: true,
            hiddenIfNull: false,
            isEnum: type.IsEnum,
            customDateTimeFormat: null,
            allowedConditionOperators: Operators.All,
            owner: owner,
            getter: target =>
            {
                try
                {
                    switch (target)
                    {
                        case IEnumerable<object> ie: return ie.LastOrDefault();
                        case string str: return str.Last();
                        case IList l:
                            var count = l.Count;
                            return count == 0 ? null : l[l.Count - 1];
                        case IEnumerable e: return e.Cast<object>().LastOrDefault();
                    }
                }
                catch { }
                return null;
            },
            setter: collectionReadonly
                ? default
                : new Setter((target, value) =>
                {
                    switch (target)
                    {
                        case IList l:
                            try
                            {
                                var count = l.Count;
                                if (count == 0)
                                    l.Add(value);
                                else l[l.Count - 1] = value;
                            }
                            catch { }
                            break;
                        default:
                            try
                            {
                                // we know that it is IList<T> of something (which does not make it IList!)
                                // so it should have an indexer and a Count property.
                                dynamic l = target;
                                int count = l.Count;
                                if (count == 0)
                                    l.Add(value);
                                else l[l.Count - 1] = value;
                            }
                            catch { }
                            break;
                    }
                })
        ) { }
    }
}