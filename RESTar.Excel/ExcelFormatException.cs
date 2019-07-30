using System;

namespace RESTar.Excel
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