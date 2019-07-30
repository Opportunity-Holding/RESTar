namespace RESTar
{
    /// <summary>
    /// Contains some information about a resource view
    /// </summary>
    public class ViewInfo
    {
        /// <summary>
        /// The name of the view
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The description of the view
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Creates an instance of the ViewInfo type
        /// </summary>
        public ViewInfo(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}