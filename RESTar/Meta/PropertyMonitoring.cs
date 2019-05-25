namespace RESTar.Meta
{
    /// <summary>
    /// Type used for generating property monitoring trees
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class PropertyMonitoring<T> where T : class
    {
        private static PropertyMonitoringTree Cache { get; set; }

        /// <summary>
        /// Creates a monitoring tree for the given type
        /// </summary>
        /// <param name="outputTermComponentSeparator">The component separator to use in output terms</param>
        /// <param name="handleObservedChange">The handler of output terms and new and old values</param>
        public static PropertyMonitoringTree CreateMonitoringTree
        (
            string outputTermComponentSeparator,
            ObservedChangeHandler handleObservedChange
        )
        {
            var tree = Cache ?? (Cache = new PropertyMonitoringTree(typeof(T), outputTermComponentSeparator, handleObservedChange));
            tree.Activate();
            return tree;
        }
    }
}