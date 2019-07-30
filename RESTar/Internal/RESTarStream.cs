using System;
using System.IO;
using System.Threading.Tasks;
using RESTar.Requests;

namespace RESTar.Internal
{
    /// <inheritdoc />
    /// <summary>
    /// The main stream class used for streams in RESTar. Will automatically save to disk if larger than
    /// 16 megabytes.
    /// </summary>
    internal class RESTarStream : Stream
    {
        private const int MaxMemoryContentLength = 1 << 24;
        private bool Swapped;

        private Stream Stream { get; set; }

        internal ContentType ContentType { get; set; }

        internal bool CanClose { private get; set; }

        internal RESTarStream Rewind()
        {
            Stream.Seek(0, SeekOrigin.Begin);
            return this;
        }

        internal byte[] GetBytes()
        {
            if (!Stream.CanRead) return new byte[0];
            try
            {
                return Stream.ToByteArray();
            }
            finally
            {
                Rewind();
            }
        }

        internal async Task<byte[]> GetBytesAsync()
        {
            if (!Stream.CanRead) return new byte[0];
            Rewind();
            try
            {
                return await Stream.ToByteArrayAsync();
            }
            finally
            {
                Rewind();
            }
        }

        internal RESTarStream(ContentType contentType, byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            ContentType = contentType;
            Stream = new MemoryStream(buffer, true);
        }

        internal RESTarStream(ContentType contentType, Stream stream = null)
        {
            ContentType = contentType;
            ResolveStream(stream);
        }

        private void ResolveStream(Stream existing)
        {
            switch (existing)
            {
                case null:
                    Stream = new MemoryStream(1024);
                    break;
                case RESTarStream rsc:
                    ResolveStream(rsc.Stream);
                    break;
                case MemoryStream ms:
                    Stream = ms;
                    break;
                case FileStream fs:
                    Swapped = true;
                    Stream = fs;
                    break;
                case var other:
                    Stream = new MemoryStream(1024);
                    using (other) other.CopyTo(this);
                    break;
            }
        }

        private void Swap()
        {
            Position = 0;
            var fileStream = RESTarConfig.MakeTempFile();
            using (var memoryStream = (MemoryStream) Stream)
                memoryStream.WriteTo(fileStream);
            Stream = fileStream;
            Swapped = true;
        }

        /// <inheritdoc />
        public override void Flush() => Stream.Flush();

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => Stream.Seek(offset, origin);

        /// <inheritdoc />
        public override void SetLength(long value) => Stream.SetLength(value);

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count) => Stream.Read(buffer, offset, count);

        public override void Close()
        {
            if (!CanClose) return;
            Stream.Close();
            base.Close();
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            var p = Stream.Position;
            if (!Swapped && p + count > MaxMemoryContentLength) Swap();
            Stream.Write(buffer, offset, count);
        }

        /// <inheritdoc />
        public override bool CanRead => Stream.CanRead;

        /// <inheritdoc />
        public override bool CanSeek => Stream.CanSeek;

        /// <inheritdoc />
        public override bool CanWrite => Stream.CanWrite;

        /// <inheritdoc />
        public override long Length => Stream.CanSeek ? Stream.Length : -1;

        /// <inheritdoc />
        public override long Position
        {
            get => Stream.Position;
            set => Stream.Position = value;
        }

        public void Deconstruct(out RESTarStream stream, out ContentType contentType, out long contentLength)
        {
            stream = this;
            contentType = ContentType;
            contentLength = CanSeek ? Length : -1;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (!CanClose) return;
            Stream.Dispose();
            base.Dispose(disposing);
        }
    }
}