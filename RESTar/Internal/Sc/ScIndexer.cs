using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Admin;
using RESTar.Linq;
using RESTar.Meta;
using RESTar.Requests;
using RESTar.Resources;
using Starcounter;
using Starcounter.Metadata;
using Resource = RESTar.Meta.Resource;

namespace RESTar.Internal.Sc
{
    internal class ScIndexer : IDatabaseIndexer
    {
        private const string AllIndexes = "SELECT t FROM Starcounter.Metadata.\"Index\" t";

        private const string ColumnByIndex = "SELECT t FROM Starcounter.Metadata.IndexedColumn t " +
                                             "WHERE t.\"Index\" =? ORDER BY t.Position";

        /// <inheritdoc />
        public IEnumerable<DatabaseIndex> Select(IRequest<DatabaseIndex> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return Db.SQL<Index>(AllIndexes)
                .Where(index => !index.Table.FullName.StartsWith("Starcounter."))
                .Where(index => !index.Table.FullName.StartsWith("RESTar.Admin."))
                .Where(index => !index.Name.StartsWith("DYNAMIT_GENERATED_INDEX"))
                .Select(index => (resource: Resource.ByTypeName(index.Table.FullName), index))
                .Where(pair => pair.resource != null)
                .Select(pair =>
                {
                    var properties = pair.resource.Type
                        .GetDeclaredProperties()
                        .Values
                        .Where(p => p.ScIndexableColumn != null)
                        .ToLookup(p => p.ScIndexableColumn);
                    return new DatabaseIndex(pair.resource.Name)
                    {
                        Name = pair.index.Name,
                        Columns = Db.SQL<IndexedColumn>(ColumnByIndex, pair.index)
                            .Select(c =>
                            {
                                var name = properties[c.Column].FirstOrDefault()?.Name ?? c.Column.Name;
                                return new ColumnInfo(name, c.Ascending == 0);
                            })
                            .ToArray()
                    };
                })
                .Where(request.Conditions)
                .ToList();
        }

        /// <inheritdoc />
        public int Insert(IRequest<DatabaseIndex> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            foreach (var index in request.GetInputEntities())
            {
                if (index.Resource == null)
                    throw new Exception("Found no resource to register index on");
                try
                {
                    CreateIndex(index);
                    count += 1;
                }
                catch (DbException e)
                {
                    throw new Exception($"Could not create index '{index.Name}' on Starcounter database resource " +
                                        $"'{index.Resource.Name}'. Indexes must point to statically defined instance " +
                                        "properties of Starcounter database classes.", e);
                }
            }
            return count;
        }

        /// <inheritdoc />
        public int Update(IRequest<DatabaseIndex> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            foreach (var index in request.GetInputEntities())
            {
                try
                {
                    DropIndex(index);
                    CreateIndex(index);
                    count += 1;
                }
                catch (DbException e)
                {
                    throw new Exception($"Could not update index '{index.Name}' on Starcounter database resource " +
                                        $"'{index.Resource.Name}'. Indexes must point to statically defined instance " +
                                        "properties of Starcounter database classes.", e);
                }
            }
            return count;
        }

        /// <inheritdoc />
        public int Delete(IRequest<DatabaseIndex> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            foreach (var index in request.GetInputEntities())
            {
                DropIndex(index);
                count += 1;
            }
            return count;
        }

        private static void CreateIndex(DatabaseIndex index)
        {
            var properties = index.Resource.Type.GetDeclaredProperties();
            var spec = index.Columns.Select(c =>
            {
                if (!properties.TryGetValue(c.Name, out var property))
                    throw new Exception($"Unknown property '{c.Name}' of resource '{index.ResourceName}'");
                var name = property.ScIndexableColumnName ?? c.Name;
                return $"{name.Fnuttify()}{(c.Descending ? " DESC" : "")}";
            });
            Db.SQL($"CREATE INDEX {index.Name.Fnuttify()} ON {index.Resource.Type.RESTarTypeName().Fnuttify()} " +
                   $"({string.Join(", ", spec)})");
        }

        private static void DropIndex(DatabaseIndex index)
        {
            Db.SQL($"DROP INDEX {index.Name.Fnuttify()} ON {index.Resource.Type.RESTarTypeName().Fnuttify()}");
        }
    }
}