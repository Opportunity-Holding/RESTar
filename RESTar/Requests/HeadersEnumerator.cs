using System.Collections;
using System.Collections.Generic;

namespace RESTar.Requests
{
    internal class HeadersEnumerator : IEnumerator<KeyValuePair<string, string>>
    {
        private HeadersMembers CurrentMember;
        private readonly IHeaders Headers;
        private readonly IEnumerator<KeyValuePair<string, string>> DictEnumerator;
        public void Dispose() => Reset();
        public void Reset() => CurrentMember = HeadersMembers.nil;
        object IEnumerator.Current => Current;
        public KeyValuePair<string, string> Current { get; private set; }

        public bool MoveNext()
        {
            switch (CurrentMember += 1)
            {
                case HeadersMembers.Accept:
                    Current = new KeyValuePair<string, string>(nameof(Headers.Accept), Headers.Accept?.ToString());
                    break;
                case HeadersMembers.ContentType:
                    Current = new KeyValuePair<string, string>("Content-Type", Headers.ContentType?.ToString());
                    break;
                case HeadersMembers.Source:
                    Current = new KeyValuePair<string, string>(nameof(Headers.Source), Headers.Source);
                    break;
                case HeadersMembers.Destination:
                    Current = new KeyValuePair<string, string>(nameof(Headers.Destination), Headers.Destination);
                    break;
                case HeadersMembers.Authorization:
                    var value = Headers.Authorization == null ? null : "*******";
                    Current = new KeyValuePair<string, string>(nameof(Headers.Authorization), value);
                    break;
                case HeadersMembers.Origin:
                    Current = new KeyValuePair<string, string>(nameof(Headers.Origin), Headers.Origin);
                    break;
                default:
                    if (!DictEnumerator.MoveNext())
                        return false;
                    Current = DictEnumerator.Current;
                    return true;
            }
            return Current.Value != null || MoveNext();
        }

        public HeadersEnumerator(IHeaders headers, IEnumerator<KeyValuePair<string, string>> dictEnumerator)
        {
            Headers = headers;
            DictEnumerator = dictEnumerator;
        }

        private enum HeadersMembers
        {
            nil = 0,
            Accept,
            ContentType,
            Source,
            Destination,
            Authorization,
            Origin
        }
    }
}