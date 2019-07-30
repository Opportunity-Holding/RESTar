using System;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable 1591

namespace RESTar.Requests.Filters
{
    /// <inheritdoc cref="IFilter" />
    /// <summary>
    /// Encodes a numeric limit used in requests. Can be implicitly converted from int.
    /// </summary>
    public struct Limit : IFilter
    {
        public readonly long Number;
        public bool Equals(Limit other) => Number == other.Number;
        public override bool Equals(object obj) => obj is Limit limit && Equals(limit);
        public override int GetHashCode() => Number.GetHashCode();
        public override string ToString() => Number.ToString();
        public static Limit NoLimit => (Limit) (-1);
        public static explicit operator Limit(long nr) => new Limit(nr);
        public static explicit operator long(Limit limit) => limit.Number;
        public static bool operator ==(Limit l, long i) => l.Number == i;
        public static bool operator !=(Limit l, long i) => l.Number != i;
        public static bool operator ==(long i, Limit l) => l.Number == i;
        public static bool operator !=(long i, Limit l) => l.Number != i;
        public static bool operator <(Limit l, long i) => l.Number < i;
        public static bool operator >(Limit l, long i) => l.Number > i;
        public static bool operator <=(Limit l, long i) => l.Number <= i;
        public static bool operator >=(Limit l, long i) => l.Number >= i;

        private Limit(long nr) => Number = nr;

        /// <summary>
        /// Applies the limiting to an IEnumerable of entities
        /// </summary>
        public IEnumerable<T> Apply<T>(IEnumerable<T> entities) where T: class => Number > -1 ? entities.Take((int) Number) : entities;
    }

    /// <inheritdoc cref="IFilter" />
    /// <summary>
    /// Encodes a numeric offset used in requests. Can be implicitly converted from int.    
    /// </summary>
    public struct Offset : IFilter
    {
        public readonly long Number;
        public bool Equals(Offset other) => Number == other.Number;
        public override bool Equals(object obj) => obj is Offset offset && Equals(offset);
        public override int GetHashCode() => Number.GetHashCode();
        public override string ToString() => Number.ToString();
        public static Offset NoOffset => (Offset) 0;
        public static explicit operator Offset(int nr) => new Offset(nr);
        public static explicit operator long(Offset limit) => limit.Number;
        public static bool operator ==(Offset o, long i) => o.Number == i;
        public static bool operator !=(Offset o, long i) => o.Number != i;
        public static bool operator <(Offset o, long i) => o.Number < i;
        public static bool operator >(Offset o, long i) => o.Number > i;
        public static bool operator <=(Offset o, long i) => o.Number <= i;
        public static bool operator >=(Offset o, long i) => o.Number >= i;

        private Offset(long nr) => Number = nr;

        /// <summary>
        /// Applies the offset to an IEnumerable of entities
        /// </summary>
        public IEnumerable<T> Apply<T>(IEnumerable<T> entities) where T : class
        {
            switch ((int) Number)
            {
                case int.MaxValue:
                case int.MinValue: return new T[0];
                case 0: return entities;
                case var positive when positive > 0: return entities.Skip(positive);
                case var negative: return NegativeSkip(entities, -negative);
            }
        }

        /// <summary>
        /// Returns the last items in the IEnumerable (with just one pass over the IEnumerable)
        /// </summary>
        private static IEnumerable<T> NegativeSkip<T>(IEnumerable<T> source, int count)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            var queue = new Queue<T>(count);
            foreach (var element in source)
            {
                queue.Enqueue(element);
                if (queue.Count > count)
                    queue.Dequeue();
            }
            return queue;
        }
    }
}