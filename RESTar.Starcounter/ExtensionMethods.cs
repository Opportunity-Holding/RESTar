using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Requests;
using static RESTar.Requests.Operators;

namespace RESTar.Starcounter
{
    internal static class ExtensionMethods
    {
        internal static (string WhereString, object[] Values) MakeWhereClause<T>
        (
            this IEnumerable<Condition<T>> conds,
            string orderByIndexName,
            out Dictionary<int, int> valuesAssignments,
            out bool useOrderBy
        ) where T : class
        {
            var _valuesAssignments = new Dictionary<int, int>();
            var literals = new List<object>();
            var hasOtherIndex = true;
            var clause = string.Join(" AND ", conds.Where(c => !c.Skip).Select((c, index) =>
            {
                var (key, op, value) = (c.Term.DbKey.Fnuttify(), c.InternalOperator.SQL, (object) c.Value);
                if (value == null)
                {
                    switch (c.Operator)
                    {
                        case EQUALS:
                            op = "IS NULL";
                            break;
                        case NOT_EQUALS:
                            op = "IS NOT NULL";
                            break;
                        default: throw new Exception($"Operator '{op}' is not valid for comparison with NULL");
                    }
                    return $"t.{key} {op}";
                }
                literals.Add(c.Value);
                hasOtherIndex = false;
                _valuesAssignments[index] = literals.Count - 1;
                return $"t.{key} {c.InternalOperator.SQL} ? ";
            }));
            useOrderBy = !hasOtherIndex;
            if (clause.Length == 0)
            {
                valuesAssignments = null;
                return (null, null);
            }
            valuesAssignments = _valuesAssignments;
            return ($"WHERE {clause}", literals.ToArray());
        }

        internal static (string WhereString, object[] Values) MakeWhereClause<T>(this IEnumerable<Condition<T>> conds, string orderByIndexName,
            out bool useOrderBy) where T : class
        {
            var literals = new List<object>();
            var hasOtherIndex = false;
            var clause = string.Join(" AND ", conds.Where(c => !c.Skip).Select(c =>
            {
                var (key, op, value) = (c.Term.DbKey.Fnuttify(), c.InternalOperator.SQL, (object) c.Value);
                if (value == null)
                {
                    switch (c.Operator)
                    {
                        case EQUALS:
                            op = "IS NULL";
                            break;
                        case NOT_EQUALS:
                            op = "IS NOT NULL";
                            break;
                        default: throw new Exception($"Operator '{op}' is not valid for comparison with NULL");
                    }
                    return $"t.{key} {op}";
                }
                literals.Add(c.Value);
                hasOtherIndex = false;
                return $"t.{key} {c.InternalOperator.SQL} ? ";
            }));
            useOrderBy = !hasOtherIndex;
            return clause.Length > 0 ? ($"WHERE {clause} ", literals.ToArray()) : (null, null);
        }


        internal static bool HasSQL<T>(this IEnumerable<Condition<T>> conds, out IEnumerable<Condition<T>> sql)
            where T : class
        {
            sql = conds.Where(c => c.ScQueryable).ToList();
            return sql.Any();
        }

        public static IEnumerable<Condition<T>> GetSQL<T>(this IEnumerable<Condition<T>> conds) where T : class
        {
            return conds.Where(c => c.ScQueryable);
        }

        internal static bool HasPost<T>(this IEnumerable<Condition<T>> conds, out IEnumerable<Condition<T>> post)
            where T : class
        {
            post = conds.Where(c => !c.ScQueryable || c.IsOfType<string>() && c.Value != null).ToList();
            return post.Any();
        }
    }
}