namespace RESTar
{
    /// <summary>
    /// RESTar supports windows and linux line endings when writing JSON. To change
    /// the line endings format, include a <see cref="LineEndings"/> instance in the 'lineEndings'
    /// parameter in the call to <see cref="RESTarConfig.Init"/>.
    /// </summary>
    public enum LineEndings
    {
        /// <summary>
        /// Line endings are written as \r\n
        /// </summary>
        Windows,

        /// <summary>
        /// Line endings are written as \n
        /// </summary>
        Linux
    }
}