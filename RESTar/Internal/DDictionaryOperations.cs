﻿using System.Collections.Generic;
using System.Linq;
using Dynamit;
using RESTar.Operations;
using Starcounter;

namespace RESTar.Internal
{
    /// <summary>
    /// The default operations for classes inheriting from DDictionary
    /// </summary>
    public static class DDictionaryOperations<T> where T : class
    {
        private static IEnumerable<T> EqualitySQL(Condition c, string kvp)
        {
            var SQL = $"SELECT CAST(t.Dictionary AS {typeof(T).FullName}) " +
                      $"FROM {kvp} t WHERE t.Key =? AND t.ValueHash {c.Operator.SQL}?";
            return Db.SQL<T>(SQL, c.Key, c.Value.GetHashCode());
        }

        private static QueryResultRows<T> AllSQL => Db.SQL<T>($"SELECT t FROM {typeof(T).FullName} t");

        /// <summary>
        /// Selects DDictionary entites
        /// </summary>
        public static Selector<T> Select => r =>
        {
            var equalityConditions = r.Conditions?.Equality;
            if (equalityConditions?.Any() != true)
                return AllSQL.Filter(r.Conditions);
            var kvpTable = TableInfo<T>.KvpTable;
            var results = new HashSet<T>();
            equalityConditions.ForEach((cond, index) =>
            {
                if (index == 0) results.UnionWith(EqualitySQL(cond, kvpTable));
                else results.IntersectWith(EqualitySQL(cond, kvpTable));
            });
            return results.Filter(r.Conditions.Compare).ToList();
        };

        /// <summary>
        /// Inserter for DDictionary entites (used by RESTar internally, don't use)
        /// </summary>
        public static Inserter<T> Insert => StarcounterOperations<T>.Insert;

        /// <summary>
        /// Updater for DDictionary entites (used by RESTar internally, don't use)
        /// </summary>
        public static Updater<T> Update => StarcounterOperations<T>.Update;

        /// <summary>
        /// Deleter for DDictionary entites (used by RESTar internally, don't use)
        /// </summary>
        public static Deleter<T> Delete => StarcounterOperations<T>.Delete;
    }
}