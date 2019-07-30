using System;
using System.Collections;
using System.Collections.Generic;
using RESTar.Requests;

namespace RESTar.Results
{
    /// <inheritdoc cref="OK" />
    /// <summary>
    /// A result that contains a set of entities
    /// </summary>
    internal class Entities<T> : Content, IEntities<T> where T : class
    {
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator() => Content.GetEnumerator();

        /// <summary>
        /// The entities contained in the result
        /// </summary>
        private IEnumerable<T> Content { get; }

        public Type EntityType => typeof(T);

        /// <inheritdoc />
        /// <summary>
        /// The number of entities contained in this result
        /// </summary>
        public ulong EntityCount { get; set; }

        /// <inheritdoc />
        public bool IsPaged => Content != null && EntityCount > 0 && (long) EntityCount == Request.MetaConditions.Limit;

        internal Entities(IRequest request, IEnumerable<T> enumerable) : base(request) => Content = enumerable ?? new T[0];

        /// <inheritdoc />
        public override string Metadata => $"{nameof(Entities<T>)};{Request.Resource};{EntityType}";
    }
}