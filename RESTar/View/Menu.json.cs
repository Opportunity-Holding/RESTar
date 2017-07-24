using System.Linq;
using Starcounter;
using RESTar.Internal;
using RESTar.Linq;

#pragma warning disable 1591

namespace RESTar.View
{
    partial class Menu : RESTarView
    {
        internal override void SetMessage(string message, ErrorCodes errorCode, MessageTypes messageType)
        {
            Message = message;
            ErrorCode = (long) errorCode;
            MessageType = messageType.ToString();
            HasMessage = true;
        }

        public Menu Populate()
        {
            Html = "/menu.html";
            MetaResourcePath = $"/{Application.Current.Name}/{typeof(Resource).FullName}";
            RESTarConfig
                .Resources
                .Select(r => new
                {
                    Name = r.AliasOrName,
                    Url = $"/{Application.Current.Name}/{r.AliasOrName}"
                }.Serialize())
                .ForEach(str => Resources.Add(new Json(str)));
            return this;
        }
    }
}