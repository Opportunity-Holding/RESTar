﻿using System;
using RESTar.Internal;

namespace RESTar.Sc
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error with the Starcounter database
    /// search string.
    /// </summary>
    public class ScDatabaseError : Results.Internal
    {
        internal ScDatabaseError(Exception e) : base(ErrorCodes.DatabaseError, e.Message, e)
        {
            Headers.Info = "The Starcounter database encountered an error: " + (e.InnerException?.Message ?? e.Message);
        }
    }
}