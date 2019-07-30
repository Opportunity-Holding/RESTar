using System.IO;
using Newtonsoft.Json;
using RESTar.Admin;

#pragma warning disable 1591

namespace RESTar.ContentTypeProviders.NativeJsonProtocol
{
    internal class RESTarJsonWriter : JsonTextWriter
    {
        private readonly string NewLine;
        private int BaseIndentation;
        private int CurrentDepth;
        public ulong ObjectsWritten { get; private set; }

        public override void WriteStartObject()
        {
            if (CurrentDepth == 0)
            {
                ObjectsWritten += 1;
                if (ObjectsWritten % 15000 == 0)
                    Flush();
            }
            CurrentDepth += 1;
            base.WriteStartObject();
        }

        public override void WriteEndObject()
        {
            CurrentDepth -= 1;
            base.WriteEndObject();
        }

        public RESTarJsonWriter(TextWriter textWriter, int baseIndentation) : base(textWriter)
        {
            BaseIndentation = baseIndentation;
            switch (Settings._LineEndings)
            {
                case LineEndings.Windows:
                    NewLine = "\r\n";
                    break;
                case LineEndings.Linux:
                    NewLine = "\n";
                    break;
                default: return;
            }
        }

        protected override void WriteIndent()
        {
            // if (Formatting != Formatting.Indented) return;
            WriteWhitespace(NewLine);
            var currentIndentCount = Top * Indentation + BaseIndentation;
            for (var i = 0; i < currentIndentCount; i++)
                WriteIndentSpace();
        }

        protected override void Dispose(bool disposing)
        {
            CurrentDepth = 0;
            ObjectsWritten = 0;
            BaseIndentation = 0;
            base.Dispose(disposing);
        }
    }
}