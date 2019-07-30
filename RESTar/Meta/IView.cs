namespace RESTar.Meta
{
    /// <inheritdoc />
    /// <summary>
    /// A non-generic interface for RESTar resource views
    /// </summary>
    public interface IView : ITarget
    {
        /// <summary>
        /// The resource of the view
        /// </summary>
        IEntityResource EntityResource { get; }
    }
}