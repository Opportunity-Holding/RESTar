namespace RESTar.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// Dynamic terminals are terminals that can accept arbitrary assignments.
    /// </summary>
    public interface IDynamicTerminal : ITerminal
    {
        /// <summary>
        /// The indexer used when assigning terminal properties not known at compile time
        /// </summary>
        object this[string key] { set; }
    }
}