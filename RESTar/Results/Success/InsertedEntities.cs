﻿using System;
using System.Net;

namespace RESTar.Results.Success
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on successful insertion of entities
    /// </summary>
    public class InsertedEntities : Result
    {
        /// <summary>
        /// The number of inserted entities
        /// </summary>
        public int InsertedCount { get; }

        /// <inheritdoc />
        public override TimeSpan TimeElapsed { get; protected set; }

        internal InsertedEntities(int count, IRequest trace) : base(trace)
        {
            InsertedCount = count;
            StatusCode = count < 1 ? HttpStatusCode.OK : HttpStatusCode.Created;
            StatusDescription = StatusCode.ToString();
            Headers["RESTar-info"] = $"{count} entities inserted into '{trace.Resource.Name}'";
            TimeElapsed = trace.TimeElapsed;
        }
    }
}