﻿using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an invalid terminal resource declaration
    /// </summary>
    public class InvalidTerminalDeclaration : Error
    {
        internal InvalidTerminalDeclaration(string info) : base(ErrorCodes.InvalidTerminalDeclaration, info) { }
    }
}
