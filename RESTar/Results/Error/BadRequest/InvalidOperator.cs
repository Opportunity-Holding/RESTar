﻿using RESTar.Internal;

namespace RESTar.Results.Fail.BadRequest
{
    internal class InvalidOperator : InvalidSyntax
    {
        internal InvalidOperator(string c) : base(ErrorCodes.InvalidConditionOperator,
            $"Invalid or missing operator or separator ('&') for condition '{c}'. Always URI encode all equals ('=' -> '%3D') " +
            "and exclamation marks ('!' -> '%21') in condition literals to avoid capture with reserved characters.") { }
    }
}