using System;

namespace RESTar.ContentTypeProviders
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error when writing to Excel
    /// </summary>
    public class ExcelFormatException : Exception
    {
        internal ExcelFormatException(string message, Exception ie) : base($"RESTar was unable to write entities to excel. {message}. ", ie) { }
    }
}