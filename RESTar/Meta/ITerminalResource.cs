namespace RESTar.Meta
{
    /// <inheritdoc />
    /// <summary>
    /// A common non-generic interface for terminal resources
    /// </summary>
    public interface ITerminalResource : IResource { }

    /// <inheritdoc />
    /// <summary>
    /// A common non-generic interface for terminal resources
    /// </summary>
    public interface ITerminalResource<T> : ITerminalResource where T : class { }
}   