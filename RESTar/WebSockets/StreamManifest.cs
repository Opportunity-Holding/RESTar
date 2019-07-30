using System;
using System.Linq;
using Newtonsoft.Json;
using RESTar.Results;

#pragma warning disable 414

namespace RESTar.WebSockets
{
    internal class StreamManifest : IDisposable
    {
        internal Content Content { get; }
        internal int CurrentMessageIndex { get; set; }
        internal int BufferSize { get; }
        internal int LastIndex { get; }
        public long TotalLength { get; }
        public long BytesRemaining { get; internal set; }
        public long BytesStreamed { get; internal set; }
        public int NrOfMessages { get; }
        public int MessagesRemaining { get; internal set; }
        public int MessagesStreamed { get; internal set; }
        public string ContentType { get; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly string EntityType;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly ulong EntityCount;

        public StreamManifestMessage[] Messages { get; }
        public readonly StreamCommand[] Commands = _Commands;

        private static readonly StreamCommand[] _Commands =
        {
            new StreamCommand
            {
                Command = "GET",
                Description = "Streams all messages"
            },
            new StreamCommand
            {
                Command = "NEXT",
                Description = "Streams the next message"
            },
            new StreamCommand
            {
                Command = "NEXT <integer>",
                Description = "Streams the next <n> messages where <n> is an integer"
            },
            new StreamCommand
            {
                Command = "MANIFEST",
                Description = "Prints the manifest"
            },
            new StreamCommand
            {
                Command = "CLOSE",
                Description = "Closes the stream and returns to the previous terminal resource"
            }
        };

        internal StreamManifest(Content content, int bufferSize)
        {
            Content = content;
            ContentType = content.Headers.ContentType?.ToString();
            TotalLength = content.Body.Length;
            BufferSize = bufferSize;
            BytesRemaining = content.Body.Length;
            if (content is IEntities entities)
            {
                EntityType = entities.EntityType.RESTarTypeName();
                EntityCount = entities.EntityCount;
            }
            var dataLength = content.Body.Length;
            var nrOfMessages = dataLength / bufferSize;
            var last = dataLength % bufferSize;
            if (last > 0) nrOfMessages += 1;
            else last = bufferSize;
            var messages = new StreamManifestMessage[nrOfMessages];
            long startIndex = 0;
            for (var i = 0; i < messages.Length; i += 1)
            {
                messages[i] = new StreamManifestMessage
                {
                    StartIndex = startIndex,
                    Length = bufferSize
                };
                startIndex += bufferSize;
            }
            messages.Last().Length = last;
            NrOfMessages = (int) nrOfMessages;
            MessagesRemaining = (int) nrOfMessages;
            LastIndex = NrOfMessages - 1;
            Messages = messages;
        }

        public void Dispose() => Content.Body.Dispose();
    }
}