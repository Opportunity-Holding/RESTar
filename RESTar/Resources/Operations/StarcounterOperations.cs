using System.Collections.Generic;
using System.Linq;
using RESTar.Admin;
using RESTar.Linq;
using RESTar.Meta;
using RESTar.Requests;
using RESTar.Requests.Filters;
using Starcounter;
using Starcounter.Metadata;
using static System.StringComparison;

namespace RESTar.Resources.Operations
{
    /// <summary>
    /// The default operations for static Starcounter database resources
    /// </summary>
    public static class StarcounterOperations<T> where T : class
    {
        private const string ColumnByTable = "SELECT t FROM Starcounter.Metadata.Column t WHERE t.Table.Fullname =?";
        private static readonly string TableName = typeof(T).RESTarTypeName();
        private static readonly string select = $"SELECT t FROM {TableName.Fnuttify()} t ";
        private const string ObjectNo = nameof(ObjectNo);
        private const string ObjectID = nameof(ObjectID);

        /// <summary>
        /// Selects entities from a Starcounter table
        /// </summary>
        public static IEnumerable<T> Select(IRequest<T> request)
        {
            switch (request.Conditions.Count)
            {
                case 0:
                    var sql = $"{select}{GetOrderbyString(request, out _)}";
                    var result = Db.SQL<T>(sql);
                    QueryConsole.Publish(sql, null, () => result.GetEnumerator());
                    return result;
                case 1 when request.Conditions[0] is var only && only.Operator == Operators.EQUALS:
                    if (string.Equals(ObjectNo, only.Key, OrdinalIgnoreCase))
                        return GetFromObjectNo(only.SafeSelect(o => (ulong) only.Value));
                    if (string.Equals(ObjectID, only.Key, OrdinalIgnoreCase))
                        return GetFromObjectNo(only.SafeSelect(o => DbHelper.Base64DecodeObjectID((string) only.Value)));
                    else goto default;
                default:
                    var orderBy = GetOrderbyString(request, out var orderByIndexName);
                    var (where, values) = request.Conditions.GetSQL().MakeWhereClause(orderByIndexName, out var useOrderBy);
                    sql = useOrderBy ? $"{select}{where}{orderBy}" : $"{select}{where}";
                    result = Db.SQL<T>(sql, values);
                    QueryConsole.Publish(sql, values, () => result.GetEnumerator());
                    return !request.Conditions.HasPost(out var post) ? result : result.Where(post);
            }
        }

        private static IEnumerable<T> GetFromObjectNo(ulong objectNo)
        {
            QueryConsole.Publish<T>($"FROMID {objectNo}", null, null);
            if (objectNo == 0) return null;
            return Db.FromId(objectNo) is T t ? new[] {t} : null;
        }

        private static string GetOrderbyString(IRequest request, out string indexedName)
        {
            if (request.MetaConditions.OrderBy is OrderBy orderBy
                && orderBy.Term.Count == 1
                && orderBy.Term.First is DeclaredProperty prop
                && prop.ScIndexesWhereFirst?.FirstOrDefault()?.Name is string _indexName)
            {
                indexedName = _indexName;
                if (prop.Type != typeof(string)) orderBy.Skip = true;
                return orderBy is OrderByAscending
                    ? $"ORDER BY t.\"{prop.ActualName}\" ASC"
                    : $"ORDER BY t.\"{prop.ActualName}\" DESC";
            }
            indexedName = null;
            return null;
        }

        /// <summary>
        /// Inserts entities into a Starcounter table. Since 
        /// </summary>
        public static int Insert(IRequest<T> request)
        {
            var count = 0;
            Db.TransactAsync(() => count = request.GetInputEntities().Count());
            return count;
        }

        /// <summary>
        /// Updates entities in a Starcounter table. 
        /// </summary>
        public static int Update(IRequest<T> request)
        {
            var count = 0;
            Db.TransactAsync(() => count = request.GetInputEntities().Count());
            return count;
        }

        /// <summary>
        /// Deletes entities from a Starcounter table
        /// </summary>
        public static int Delete(IRequest<T> request)
        {
            var count = 0;
            Db.TransactAsync(() => request.GetInputEntities().ForEach(entity =>
            {
                entity.Delete();
                count += 1;
            }));
            return count;
        }

        /// <summary>
        /// Creates profiles for Starcounter tables
        /// </summary>
        public static ResourceProfile Profile(IEntityResource<T> resource) => ResourceProfile.Make(resource, rows =>
        {
            var resourceSQLName = typeof(T).RESTarTypeName();
            var scColumns = Db.SQL<Column>(ColumnByTable, resourceSQLName).Select(c => c.Name).ToList();
            var columns = resource.Members.Values.Where(p => scColumns.Contains(p.Name)).ToList();
            return rows.Sum(e => columns.Sum(p => p.ByteCount(e)) + 16);
        });

        internal static bool IsValid(IEntityResource resource, out string reason)
        {
            if (resource.InterfaceType != null)
            {
                var interfaceName = resource.InterfaceType.RESTarTypeName();
                var members = resource.InterfaceType.GetDeclaredProperties();
                if (members.ContainsKey("objectno"))
                {
                    reason = $"Invalid Interface '{interfaceName}' assigned to resource '{resource.Name}'. " +
                             "Interface contained a property with a reserved name: 'ObjectNo'";
                    return false;
                }
                if (members.ContainsKey("objectid"))
                {
                    reason = $"Invalid Interface '{interfaceName}' assigned to resource '{resource.Name}'. " +
                             "Interface contained a property with a reserved name: 'ObjectID'";
                    return false;
                }
            }

            if (resource.Type.ImplementsGenericInterface(typeof(IProfiler<>)))
            {
                reason = $"Invalid IProfiler interface implementation for resource type '{resource.Name}'. " +
                         "Starcounter database resources use their default profilers, and cannot implement IProfiler";
                return false;
            }
            reason = null;
            return true;
        }
    }
}