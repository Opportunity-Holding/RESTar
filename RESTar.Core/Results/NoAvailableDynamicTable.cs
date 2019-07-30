namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar cannot allocate a dynamic table for resource insertion
    /// </summary>
    internal class NoAvailableDynamicTable : BadRequest
    {
        internal NoAvailableDynamicTable() : base(ErrorCodes.NoAvalailableDynamicTable,
            "RESTar have no more unallocated dynamic tables. Remove an existing table and try again.") { }
    }
}