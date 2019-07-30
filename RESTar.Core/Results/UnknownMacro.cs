namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar cannot locate a macro based on some search string
    /// </summary>
    internal class UnknownMacro : NotFound
    {
        internal UnknownMacro(string searchString) : base(ErrorCodes.UnknownMacro,
            $"RESTar could not locate any valid macro by '{searchString}'. To list macros, use the 'RESTar.Admin.Macro' resource") { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar cannot locate a macro based on some search string
    /// </summary>
    internal class MacrosNotSupported : NotFound
    {
        internal MacrosNotSupported() : base(ErrorCodes.UnknownMacro,
            $"This version of RESTar does not support macros")
        { }
    }
}