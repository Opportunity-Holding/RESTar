using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RESTar.Resources
{
    /// <inheritdoc cref="ICollection{T}" />
    /// <summary>
    /// A thread-safe set of terminals.
    /// </summary>
    public class TerminalSet<T> : ICollection<T> where T : class, ITerminal
    {
        private readonly IDictionary<T, byte> terminals;

        /// <summary>
        /// Creats a new <see cref="TerminalSet{T}"/>
        /// </summary>
        public TerminalSet() => terminals = new ConcurrentDictionary<T, byte>();

        /// <inheritdoc />
        public void Add(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            terminals[item] = byte.MinValue;
        }

        /// <inheritdoc />
        public bool Contains(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            return terminals.ContainsKey(item);
        }

        /// <inheritdoc />
        public bool Remove(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            return terminals.Remove(item);
        }

        /// <inheritdoc />
        public int Count => terminals.Count;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator() => terminals.Keys.GetEnumerator();

        /// <inheritdoc />
        public bool IsReadOnly => terminals.IsReadOnly;

        /// <inheritdoc />
        public void Clear() => terminals.Clear();

        /// <inheritdoc />
        public void CopyTo(T[] array, int arrayIndex) => terminals
            .CopyTo(array.Select(a => new KeyValuePair<T, byte>(a, byte.MinValue)).ToArray(), arrayIndex);
    }
}