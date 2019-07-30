namespace RESTar.Admin
{
    /// <summary>
    /// Contains a description of a resource size in memory
    /// </summary>
    public struct ResourceSize
    {
        /// <summary>
        /// The size in bytes
        /// </summary>
        public readonly long Bytes;

        /// <summary>
        /// The size in kilobytes
        /// </summary>
        public readonly decimal KB;

        /// <summary>
        /// The size in megabytes
        /// </summary>
        public readonly decimal MB;

        /// <summary>
        /// The size in gigabytes
        /// </summary>
        public readonly decimal GB;

        /// <summary>
        /// Creates a new ResourceSize instance, encoding the given bytes
        /// </summary>
        public ResourceSize(long bytes)
        {
            Bytes = bytes;
            var decimalBytes = (decimal) bytes;
            GB = decimal.Round(decimalBytes / 1_000_000_000, 6);
            MB = decimal.Round(decimalBytes / 1_000_000, 6);
            KB = decimal.Round(decimalBytes / 1_000, 6);
        }
    }
}