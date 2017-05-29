using Newtonsoft.Json.Serialization;
using RESTar.Operations;
using Starcounter;

namespace RESTar.View.Serializer
{
    public class ObjectIDProvider : IValueProvider
    {
        public object GetValue(object target) => Do.Try(target.GetObjectID, "");

        public void SetValue(object target, object value)
        {
        }
    }
}