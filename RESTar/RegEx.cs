﻿namespace RESTar
{
    /// <summary>
    /// RegEx patterns used internally by RESTar
    /// </summary>
    internal struct RegEx
    {
        /// <summary>
        /// The main URI regex, used when parsing requests
        /// </summary>
        internal const string RequestUri =
            @"\?*(?<resource_or_macro>/[^/-]*)?(?<view>-\w*)?(?<conditions>/[^/]*)?(?<metaconditions>/[^/]*)?";

        /// <summary>
        /// The base URI regex, used when validating base uris in RESTarConfig.Init
        /// </summary>
        internal const string BaseUri = @"^/?\w+$";

        /// <summary>
        /// A regex used to isolate the key meta-condition in meta-condition strings
        /// </summary>
        internal const string KeyMetaCondition = @"&key=(?<key>[^/&]+)|key=(?<key>[^/&]+)&?";

        /// <summary>
        /// A regex used to find protected authentication data in condition strings
        /// </summary>
        internal static string AuthDataCondition(string name) => $@"&{name}=(?<value>[^/&]+)|{name}=(?<value>[^/&]+)&?";

        /// <summary>
        /// Matches only letters, numbers and underscores
        /// </summary>
        internal const string LettersNumsAndUs = @"^\w+$";

        /// <summary>
        /// Matches only strings that are valid dynamic resource names
        /// </summary>
        internal const string DynamicResourceName = @"^[a-zA-Z0-9_\.]+$";

        /// <summary>
        /// Matches headers in source and destination header syntax
        /// </summary>
        internal const string RequestHeader = @"\[(?<header>.+):[\s]*(?<value>.+)\]";

        /// <summary>
        /// Used when sending unescaped data through a RESTar view model
        /// </summary>
        internal const string ViewMacro = @"\@RESTar\((?<content>[^\(\)]*)\)";

        /// <summary>
        /// Used in setoperations when mapping object data to function parameters
        /// </summary>
        internal const string MapMacro = @"\$\([^\$\(\)]+\)";

        /// <summary>
        /// Matches all header names reserved by RESTar
        /// </summary>
        internal const string ReservedHeaders = @"^(source|destination|authorization|restar-authtoken)$";
    }
}