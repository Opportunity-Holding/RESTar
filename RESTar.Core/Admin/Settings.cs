using System.Collections.Generic;
using System.IO;
using RESTar.Linq;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
using static RESTar.Method;

namespace RESTar.Admin
{
    /// <summary>
    /// The settings resource contains the current settings for the RESTar instance.
    /// </summary>
    [RESTar(GET, Description = description)]
    public class Settings : ISelector<Settings>
    {
        private const string description = "The Settings resource contains the current " +
                                           "settings for the RESTar instance.";

        internal const string All = "SELECT t FROM RESTar.Admin.Settings t";

        internal static ushort _Port => Instance.Port;
        internal static string _Uri => Instance.Uri;
        internal static bool _PrettyPrint => Instance.PrettyPrint;
        internal static int _DaysToSaveErrors => Instance.DaysToSaveErrors;
        internal static string _ResourcesPath => Instance.ResourcesPath;
        internal static string _HelpResourcePath => Instance.DocumentationURL;
        internal static bool _DontUseLRT => Instance.DontUseLRT;
        internal static LineEndings _LineEndings => Instance.LineEndings;

        /// <summary>
        /// The port of the RESTar REST API
        /// </summary>
        public ushort Port { get; private set; }

        /// <summary>
        /// The URI of the RESTar REST API
        /// </summary>
        public string Uri { get; private set; }

        /// <summary>
        /// Will JSON be serialized with pretty print? (indented JSON)
        /// </summary>
        public bool PrettyPrint { get; set; }

        /// <summary>
        /// Use long running transactions instead of regular transact calls
        /// </summary>
        [RESTarMember(ignore: true)] public bool DontUseLRT { get; set; }

        /// <summary>
        /// The line endings to use when writing JSON
        /// </summary>
        public LineEndings LineEndings { get; private set; }

        /// <summary>
        /// The path where resources are available
        /// </summary>
        public string ResourcesPath => $"http://[IP address]:{Port}{Uri}";

        /// <summary>
        /// The path where help resources are available
        /// </summary>
        public string DocumentationURL => "https://develop.mopedo.com";

        /// <summary>
        /// The number of days to store errors in the RESTar.Error resource
        /// </summary>
        public int DaysToSaveErrors { get; private set; }

        /// <summary>
        /// The RESTar version of the current application
        /// </summary>
        public string RESTarVersion { get; private set; }

        /// <summary>
        /// The path where temporary files are created
        /// </summary>
        [RESTarMember(hide: true)] public string TempFilePath { get; private set; }

        public IEnumerable<Settings> Select(IRequest<Settings> request)
        {
            return new[] {Instance}.Where(request.Conditions);
        }

        private static Settings Instance { get; set; }

        internal static void Init
        (
            ushort port,
            string uri,
            bool prettyPrint,
            int daysToSaveErrors,
            LineEndings lineEndings
        )
        {
            Instance = new Settings
            {
                Port = port,
                Uri = uri,
                PrettyPrint = prettyPrint,
                DaysToSaveErrors = daysToSaveErrors,
                LineEndings = lineEndings,
                TempFilePath = Path.GetTempPath(),
                RESTarVersion = RESTarConfig.Version
            };
        }
    }
}