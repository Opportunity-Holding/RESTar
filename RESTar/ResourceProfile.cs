﻿using System;
using System.Linq;
using Dynamit;
using RESTar.Deflection.Dynamic;
using RESTar.Linq;
using Starcounter;
using Starcounter.Metadata;

namespace RESTar
{
    /// <summary>
    /// Provides a profile for a given resource
    /// </summary>
    public class ResourceProfile
    {
        private const int addBytes = 16;

        /// <summary>
        /// The name of the table
        /// </summary>
        public string ResourceName { get; set; }

        /// <summary>
        /// The number of rows in the table
        /// </summary>
        public long NumberOfEntities { get; set; }

        /// <summary>
        /// An approximation of the table size in memory
        /// </summary>
        public ResourceSize ApproximateSize { get; set; }

        internal static ResourceProfile MakeStarcounter(Type starcounter)
        {
            var resourceSQLName = starcounter.FullName;
            var columns = Db.SQL<Column>($"SELECT t FROM {typeof(Column).FullName} t WHERE t.Table.Fullname =?",
                    resourceSQLName)
                .Select(c => c.Name)
                .ToList();
            var domainCount = Db.SQL<long>($"SELECT COUNT(t) FROM {resourceSQLName} t").First;
            var properties = starcounter.GetTableColumns().Where(p => columns.Contains(p.DatabaseQueryName)).ToList();
            var scExtension = Db.SQL($"SELECT t FROM {resourceSQLName} t");
            var totalBytes = 0L;
            if (domainCount <= 1000)
                scExtension.ForEach(e =>
                {
                    foreach (var p in properties)
                        totalBytes += p.ByteCount(e);
                    totalBytes += addBytes;
                });
            else
            {
                var step = domainCount / 1000;
                var sample = scExtension.Where((_, i) => i % step == 0).ToList();
                var sampleRate = (decimal) sample.Count / domainCount;
                var sampleBytes = 0L;
                sample.ForEach(e =>
                {
                    foreach (var p in properties)
                        sampleBytes += p.ByteCount(e);
                    sampleBytes += addBytes;
                });
                var total = sampleBytes / sampleRate;
                totalBytes = decimal.ToInt64(total);
            }
            return new ResourceProfile
            {
                ResourceName = starcounter.FullName,
                NumberOfEntities = domainCount,
                ApproximateSize = new ResourceSize(totalBytes)
            };
        }

        internal static ResourceProfile MakeDDictionary(Type ddict)
        {
            var domainCount = Db.SQL<long>($"SELECT COUNT(t) FROM {ddict.FullName} t").First;
            var ddictExtension = Db.SQL<DDictionary>($"SELECT t FROM {ddict.FullName} t");
            long totalBytes;
            if (domainCount <= 1000)
                totalBytes = ddictExtension.Sum(entity => addBytes + entity.KeyValuePairs.Sum(kvp => kvp.ByteCount));
            else
            {
                var step = domainCount / 1000;
                var sample = ddictExtension.Where((_, i) => i % step == 0).ToList();
                var sampleRate = (decimal) sample.Count / domainCount;
                var sampleBytes = ddictExtension.Sum(entity => addBytes + entity.KeyValuePairs.Sum(kvp => kvp.ByteCount));
                var total = sampleBytes / sampleRate;
                totalBytes = decimal.ToInt64(total);
            }
            return new ResourceProfile
            {
                ResourceName = ddict.FullName,
                NumberOfEntities = domainCount,
                ApproximateSize = new ResourceSize(totalBytes)
            };
        }

        /// <summary>
        /// Creates a ResourceProfile for the given type.
        /// </summary>
        /// <param name="type">The type to profile</param>
        public static ResourceProfile Make(Type type)
        {
            switch (type)
            {
                case var _ when type.IsDDictionary(): return MakeDDictionary(type);
                case var _ when type.IsStarcounter(): return MakeStarcounter(type);
                default:
                    return Resource.SafeGet(type)?.ResourceProfile
                           ?? throw new ArgumentException($"Cannot profile '{type.FullName}'. No profiler implemented for type");
            }
        }
    }

    /// <summary>
    /// Contains a description of a table size
    /// </summary>
    public class ResourceSize
    {
        /// <summary>
        /// The size in bytes
        /// </summary>
        public long Bytes { get; }

        /// <summary>
        /// The size in kilobytes
        /// </summary>
        public decimal KB { get; }

        /// <summary>
        /// The size in megabytes
        /// </summary>
        public decimal MB { get; }

        /// <summary>
        /// The size in gigabytes
        /// </summary>
        public decimal GB { get; }

        /// <summary>
        /// Creates a new ResourceSize instance, encoding the given bytes
        /// </summary>
        public ResourceSize(long bytes)
        {
            Bytes = bytes;
            GB = decimal.Round((decimal) bytes / 1000000000, 6);
            MB = decimal.Round((decimal) bytes / 1000000, 6);
            KB = decimal.Round((decimal) bytes / 1000, 6);
        }
    }
}