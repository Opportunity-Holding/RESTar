namespace RESTar.ContentTypeProviders
{
    /// <summary>
    /// A static class that provides access to a static JsonProvider and ExcelProvider instances
    /// </summary>
    public static class Providers
    {
        /// <summary>
        /// A statically accessable JsonContentProvider
        /// </summary>
        public static JsonProvider Json { get; }

        /// <summary>
        /// A statically accessable ExcelContentProvider
        /// </summary>
        public static ExcelProvider Excel { get; }

        static Providers()
        {
            Json = new JsonProvider();
            Excel = new ExcelProvider();
        }
    }
}