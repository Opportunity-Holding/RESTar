using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using RESTar.Meta;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
using static RESTar.Internal.EntityResourceProviderController;

namespace RESTar.Admin
{
    /// <inheritdoc cref="ISelector{T}" />
    /// <inheritdoc cref="IInserter{T}" />
    /// <inheritdoc cref="IUpdater{T}" />
    /// <inheritdoc cref="IDeleter{T}" />
    /// <inheritdoc cref="IValidator{T}" />
    /// <summary>
    /// The DatabaseIndex resource lets an administrator set indexes for RESTar database resources.
    /// </summary>
    [RESTar(Description = description)]
    public class DatabaseIndex : ISelector<DatabaseIndex>, IInserter<DatabaseIndex>, IUpdater<DatabaseIndex>,
        IDeleter<DatabaseIndex>, IValidator<DatabaseIndex>
    {
        private const string description = "The DatabaseIndex resource lets an administrator set " +
                                           "indexes for Starcounter database resources.";

        private string _name;

        /// <summary>
        /// The name of the index
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(Name));
                if (!Regex.IsMatch(value, RegEx.LettersNumsAndUs))
                    throw new FormatException("Index name contained invalid characters. Can only contain " +
                                              "letters, numbers and underscores");

                _name = value;
            }
        }

        private string _resourceName;

        /// <summary>
        /// The full name of the RESTar resource corresponding with the database table on which 
        /// this index is registered
        /// </summary>
        public string ResourceName
        {
            get => _resourceName;
            set
            {
                if (!Meta.Resource.TryFind<IEntityResource>(value, out var resource, out var error))
                    throw error;
                Resource = resource;
                _resourceName = Resource.Name;
                Provider = Resource.Provider;
            }
        }

        /// <summary>
        /// The resource provider that generated this index
        /// </summary>
        public string Provider { get; private set; }

        /// <summary>
        /// The RESTar resource corresponding to the table on which this index is registered
        /// </summary>
        [RESTarMember(ignore: true)] public IEntityResource Resource { get; private set; }

        /// <summary>
        /// The columns on which this index is registered
        /// </summary>
        public ColumnInfo[] Columns { get; set; }

        /// <inheritdoc />
        [JsonConstructor]
        public DatabaseIndex(string resourceName)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
                throw new Exception("Found no resource to register the index on. The 'ResourceName' " +
                                    "property was null or empty");
            ResourceName = resourceName;
        }

        #region Public helpers

        /// <summary>
        /// Creates an ascending database index for the table T with a given name on the given column(s).
        /// If an index with the same name already exists, does nothing.
        /// </summary>
        public static void Register<T>(string indexName, params string[] columnNames) where T : class
        {
            Register<T>(indexName, columnNames.Select(columnName => (ColumnInfo) (columnName, false)).ToArray());
        }

        /// <summary>
        /// Creates a database index for the table T with a given name on the given column(s).
        /// If an index with the same name already exists, does nothing.
        /// </summary>
        public static void Register<T>(string indexName, params (string columnName, bool descending)[] columns) where T : class
        {
            Register<T>(indexName, columns.Select(column => (ColumnInfo) column).ToArray());
        }

        /// <summary>
        /// Creates a database index for a table type with a given name on a given list of columns.
        /// If an index with the same name already exists, does nothing.
        /// </summary>
        private static void Register<T>(string indexName, params ColumnInfo[] columns) where T : class
        {
            if (!RESTarConfig.Initialized)
                throw new NotInitializedException(
                    $"Invalid call to DatabaseIndex.Register() with index name '{indexName}' for type '{typeof(T)}'. " +
                    "Cannot register database indexes before RESTarConfig.Init() has been called.");
            SelectionCondition.Value = indexName;
            SelectionRequest.Selector = () => new[] {new DatabaseIndex(typeof(T).RESTarTypeName()) {Name = indexName, Columns = columns}};
            SelectionRequest.Evaluate().ThrowIfError();
        }

        #endregion

        private static Condition<DatabaseIndex> SelectionCondition { get; set; }

        private static IRequest<DatabaseIndex> SelectionRequest { get; set; }
        //internal static readonly Dictionary<string, IDatabaseIndexer> Indexers;
        //static DatabaseIndex() => Indexers = new Dictionary<string, IDatabaseIndexer>();

        internal static void Init()
        {
            SelectionCondition = new Condition<DatabaseIndex>(nameof(Name), Operators.EQUALS, null);
            SelectionRequest = Context.Root.CreateRequest<DatabaseIndex>(Method.PUT);
            SelectionRequest.Conditions.Add(SelectionCondition);
        }

        /// <inheritdoc />
        public bool IsValid(DatabaseIndex entity, out string invalidReason)
        {
            if (string.IsNullOrWhiteSpace(entity.ResourceName))
            {
                invalidReason = "Index resource name cannot be null or whitespace";
                return false;
            }
            if (string.IsNullOrWhiteSpace(entity.Name))
            {
                invalidReason = "Index name cannot be null or whitespace";
                return false;
            }
            if (entity.Columns?.Any() != true)
            {
                invalidReason = "No columns specified for index";
                return false;
            }

            invalidReason = null;
            return true;
        }

        /// <inheritdoc />
        public IEnumerable<DatabaseIndex> Select(IRequest<DatabaseIndex> request) => EntityResourceProviders
            .Values
            .Select(p => p.DatabaseIndexer)
            .Where(indexer => indexer != null)
            .Distinct()
            .SelectMany(indexer => indexer.Select(request));

        /// <inheritdoc />
        public int Insert(IRequest<DatabaseIndex> request) => request.GetInputEntities()
            .GroupBy(index => index.Resource.Provider)
            .Sum(group =>
            {
                if (!EntityResourceProviders.TryGetValue(group.Key, out var provider) || !(provider.DatabaseIndexer is IDatabaseIndexer indexer))
                    throw new Exception($"Unable to register index. Resource '{group.First().Resource.Name}' is not a database resource.");
                request.Selector = () => group;
                return indexer.Insert(request);
            });

        /// <inheritdoc />
        public int Update(IRequest<DatabaseIndex> request) => request.GetInputEntities()
            .GroupBy(index => index.Resource.Provider)
            .Sum(group =>
            {
                request.Updater = _ => group;
                return EntityResourceProviders[group.Key].DatabaseIndexer.Update(request);
            });

        /// <inheritdoc />
        public int Delete(IRequest<DatabaseIndex> request) => request.GetInputEntities()
            .GroupBy(index => index.Resource.Provider)
            .Sum(group =>
            {
                request.Selector = () => group;
                return EntityResourceProviders[group.Key].DatabaseIndexer.Delete(request);
            });
    }
}