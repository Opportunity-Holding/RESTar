using System;

namespace RESTar.Excel
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error when reading from Excel
    /// </summary>
    public class ExcelInputException : Exception
    {
        internal ExcelInputException(string message) : base(
            "There was a format error in the excel input. Check that the file is being transmitted properly. In " +
            "curl, make sure the flag '--data-binary' is used and not '--data' or '-d'. Message: " + message) { }
    }
}