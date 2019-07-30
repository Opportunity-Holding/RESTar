using System;
using System.IO;
using Newtonsoft.Json;

namespace RESTar.ContentTypeProviders.NativeJsonProtocol
{
    internal class FormatWriter : JsonTextWriter
    {
        internal int Depth { get; private set; }
        public FormatWriter(TextWriter textWriter) : base(textWriter) { }

        protected override void WriteIndent()
        {
            if (Formatting != Formatting.Indented) return;
            WriteWhitespace(Environment.NewLine);
            var currentIndentCount = Top * Indentation;
            if (Depth < currentIndentCount)
                Depth = currentIndentCount;
            for (var i = 0; i < currentIndentCount; i++)
                WriteIndentSpace();
        }
    }
}