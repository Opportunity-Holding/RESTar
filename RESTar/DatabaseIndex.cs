﻿using System.Collections.Generic;
using System.Linq;
using Starcounter;
using Starcounter.Metadata;
using static RESTar.RESTarPresets;

namespace RESTar
{
    public class Column
    {
        public string Name { get; set; }
        public bool Descending { get; set; }
    }

    [RESTar(ReadAndWrite)]
    public class DatabaseIndex : ISelector<DatabaseIndex>, IInserter<DatabaseIndex>, IUpdater<DatabaseIndex>,
        IDeleter<DatabaseIndex>
    {
        public string Name { get; set; }
        public string Resource { get; set; }
        public Column[] Columns { get; set; }

        private const string IndexSQL = "SELECT t FROM Starcounter.Metadata.\"Index\" t";

        private const string ColumnSQL = "SELECT t FROM Starcounter.Metadata.IndexedColumn t " +
                                         "WHERE t.\"Index\" =? ORDER BY t.Position";

        private static IEnumerable<DatabaseIndex> All => Db
            .SQL<Index>(IndexSQL)
            .Where(i => !i.Table.FullName.StartsWith("Starcounter."))
            .Where(i => !i.Name.StartsWith("DYNAMIT_GENERATED_INDEX"))
            .Select(i =>
            {
                var columns = Db.SQL<IndexedColumn>(ColumnSQL, i);
                return new DatabaseIndex
                {
                    Name = i.Name,
                    Resource = i.Table.FullName,
                    Columns = columns.Select(c => new Column
                    {
                        Name = c.Column.Name,
                        Descending = c.Ascending == 0
                    }).ToArray(),
                };
            });

        public IEnumerable<DatabaseIndex> Select(IRequest request) => All.Filter(request.Conditions).ToList();

        public int Insert(IEnumerable<DatabaseIndex> indices, IRequest request)
        {
            var count = 0;
            foreach (var index in indices)
            {
                Db.SQL($"CREATE INDEX \"{index.Name}\" ON {index.Resource} " +
                       $"({string.Join(", ", index.Columns.Select(c => $"\"{c.Name}\" {(c.Descending ? "DESC" : "")}"))})");
                count += 1;
            }
            return count;
        }

        public int Update(IEnumerable<DatabaseIndex> indices, IRequest request)
        {
            var count = 0;
            foreach (var index in indices)
            {
                Db.SQL($"DROP INDEX {index.Name} ON {index.Resource}");
                Db.SQL($"CREATE INDEX \"{index.Name}\" ON {index.Resource} " +
                       $"({string.Join(", ", index.Columns.Select(c => $"\"{c.Name}\" {(c.Descending ? "DESC" : "")}"))})");
                count += 1;
            }
            return count;
        }

        public int Delete(IEnumerable<DatabaseIndex> indices, IRequest request)
        {
            var count = 0;
            foreach (var index in indices)
            {
                Db.SQL($"DROP INDEX {index.Name} ON {index.Resource}");
                count += 1;
            }
            return count;
        }
    }
}