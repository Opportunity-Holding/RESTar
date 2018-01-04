﻿using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Admin;
using RESTar.Linq;
using Starcounter;
using Starcounter.Metadata;

namespace RESTar.Resources
{
    internal class StarcounterIndexer : IDatabaseIndexer
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
                .Where(index => !index.Name.StartsWith("DYNAMIT_GENERATED_INDEX"))
                .Select(index => (resource: Resource.ByTypeName(index.Table.FullName), index))
                .Where(pair => pair.resource != null)
                .Select(pair => new DatabaseIndex(pair.resource.FullName)
                {
                    Name = pair.index.Name,
                    Columns = Db.SQL<IndexedColumn>(ColumnByIndex, pair.index).Select(c => new ColumnInfo
                    {
                        Name = c.Column.Name,
                        Descending = c.Ascending == 0
                    }).ToArray()
                })
                .Where(request.Conditions);
        }

        /// <inheritdoc />
        public int Insert(IEnumerable<DatabaseIndex> indexes, IRequest<DatabaseIndex> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            foreach (var index in indexes)
            {
                if (index.IResource == null)
                    throw new Exception("Found no resource to register index on");
                try
                {
                    Db.SQL($"CREATE INDEX {index.Name.Fnuttify()} ON {index.IResource.Type.FullName.Fnuttify()} " +
                           $"({string.Join(", ", index.Columns.Select(c => $"{c.Name.Fnuttify()} {(c.Descending ? "DESC" : "")}"))})");
                    count += 1;
                }
                catch (SqlException e)
                {
                    throw new Exception($"Could not create index '{index.Name}' on Starcounter database resource " +
                                        $"'{index.IResource.FullName}'. Indexes must point to statically defined instance " +
                                        "properties of Starcounter database classes.", e);
                }
            }
            return count;
        }

        /// <inheritdoc />
        public int Update(IEnumerable<DatabaseIndex> indexes, IRequest<DatabaseIndex> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            foreach (var index in indexes)
            {
                Db.SQL($"DROP INDEX {index.Name.Fnuttify()} ON {index.IResource.Type.FullName.Fnuttify()}");
                try
                {
                    Db.SQL($"CREATE INDEX {index.Name.Fnuttify()} ON {index.IResource.Type.FullName.Fnuttify()} " +
                           $"({string.Join(", ", index.Columns.Select(c => $"{c.Name.Fnuttify()} {(c.Descending ? "DESC" : "")}"))})");
                    count += 1;
                }
                catch (SqlException e)
                {
                    throw new Exception($"Could not update index '{index.Name}' on Starcounter database resource " +
                                        $"'{index.IResource.FullName}'. Indexes must point to statically defined instance " +
                                        "properties of Starcounter database classes.", e);
                }
            }
            return count;
        }

        /// <inheritdoc />
        public int Delete(IEnumerable<DatabaseIndex> indexes, IRequest<DatabaseIndex> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            foreach (var index in indexes)
            {
                Db.SQL($"DROP INDEX {index.Name.Fnuttify()} ON {index.IResource.Type.FullName.Fnuttify()}");
                count += 1;
            }
            return count;
        }
    }
}