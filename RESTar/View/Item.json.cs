using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar.Internal;
using Starcounter;
using Request = RESTar.Requests.Request;

namespace RESTar.View
{
    partial class Item : RESTarView<object>
    {
        protected override string HtmlMatcher => $"{Resource.Name}-item.html";
        protected override void SetHtml(string html) => Html = html;
        protected override void SetResourceName(string resourceName) => ResourceName = resourceName;
        protected override void SetResourcePath(string resourcePath) => ResourcePath = resourcePath;
        private bool IsTemplate;

        public override void SetMessage(string message, ErrorCodes errorCode, MessageType messageType)
        {
            Message = message;
            ErrorCode = (long) errorCode;
            MessageType = messageType.ToString();
            HasMessage = true;
        }

        private static readonly Regex regex = new Regex(@"\@RESTar\((?<content>[^\(\)]*)\)");

        public void Handle(Input.Save action)
        {
            var entityJson = Entity.ToJson().Replace(@"$"":", @""":");
            var json = regex.Replace(entityJson, "${content}");
            if (IsTemplate) POST(json);
            else PATCH(json);
            if (IsTemplate && Success)
                RedirectUrl = !string.IsNullOrWhiteSpace(RedirectUrl) ? RedirectUrl : ResourcePath;
            Success = false;
        }

        public void Handle(Input.Close action)
        {
            RedirectUrl = !string.IsNullOrWhiteSpace(RedirectUrl)
                ? RedirectUrl
                : Resource.Singleton
                    ? $"/{Application.Current.Name}"
                    : ResourcePath;
        }

        public void Handle(Input.AddElementTo action)
        {
            try
            {
                var parts = action.Value.Split(',');
                var path = parts[0];
                var array = (Arr<Json>) path
                    .Replace("$", "")
                    .Split('.')
                    .Aggregate(Entity, (json, key) =>
                        int.TryParse(key, out int index)
                            ? (Json) json[index]
                            : (Json) json[key]);

                if (parts.Length == 1)
                    array.Add();
                else
                {
                    var value = JToken.Parse(regex.Replace(parts[1], "${content}"));
                    switch (value.Type)
                    {
                        case JTokenType.Integer:
                            array.Add().IntegerValue = value.Value<int>();
                            return;
                        case JTokenType.Float:
                            array.Add().DecimalValue = value.Value<decimal>();
                            return;
                        case JTokenType.String:
                            array.Add().StringValue = value.Value<string>();
                            return;
                        case JTokenType.Boolean:
                            array.Add().BoolValue = value.Value<bool>();
                            return;
                        default: throw new ArgumentOutOfRangeException();
                    }
                }
                action.Cancel();
            }
            catch (JsonReaderException)
            {
                throw new Exception($"Could not add element to '{action.Value}'. Invalid syntax.");
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new Exception($"Could not add element to '{action.Value}'. Invalid syntax.");
            }
            catch (Exception)
            {
                throw new Exception($"Could not add element to '{action.Value}'. Not an array.");
            }
        }

        internal override RESTarView<object> Populate(Request request, object restarData)
        {
            var template = request.Resource.MakeViewModelTemplate();
            var jsonTemplate = template.SerializeVmJsonTemplate();
            Entity = new Json {Template = Starcounter.Templates.Template.CreateFromJson(jsonTemplate)};
            if (restarData == null)
            {
                IsTemplate = true;
                base.Populate(request, template);
            }
            else
            {
                base.Populate(request, restarData);
                var json = restarData.SerializeStaticResourceToViewModel();
                Entity.PopulateFromJson(json);
            }
            return this;
        }

        private static readonly MemberComparer Comparer = new MemberComparer();

        private class MemberComparer : IEqualityComparer<KeyValuePair<string, JToken>>
        {
            private readonly JTokenEqualityComparer comparer = new JTokenEqualityComparer();

            public bool Equals(KeyValuePair<string, JToken> x, KeyValuePair<string, JToken> y)
            {
                return x.Key == y.Key && JToken.DeepEquals(x.Value, y.Value);
            }

            public int GetHashCode(KeyValuePair<string, JToken> obj)
            {
                return $"{obj.Key}_{comparer.GetHashCode(obj.Value)}".GetHashCode();
            }
        }
    }
}