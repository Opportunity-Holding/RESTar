using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Linq;
using RESTar.Meta;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
using static RESTar.Method;

namespace RESTar.Admin
{
    /// <inheritdoc />
    /// <summary>
    /// Gets all error codes used by RESTar
    /// </summary>
    [RESTar(GET, Description = "The error codes used by RESTar.")]
    public class ErrorCode : ISelector<ErrorCode>
    {
        /// <summary>
        /// The name of the error
        /// </summary>
        public ErrorCodes Name { get; private set; }

        /// <summary>
        /// The numeric code of the error
        /// </summary>
        public int Code { get; private set; }

        /// <inheritdoc />
        public IEnumerable<ErrorCode> Select(IRequest<ErrorCode> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return EnumMember<ErrorCodes>
                .GetMembers()
                .Select(m => new ErrorCode {Name = m.Value, Code = m.NumericValue})
                .Where(request.Conditions);
        }
    }
}