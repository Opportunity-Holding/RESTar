using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.ContentTypeProviders;
using RESTar.Linq;
using RESTar.Requests;
using Starcounter;
using static RESTar.Admin.Settings;

namespace RESTar.Admin
{
    /// <summary>
    /// </summary>
    [Database]
    public class DbOutputFormat
    {
        #region Schema

        /// <summary>
        /// The name of the pattern
        /// </summary>
        public string Name { get; internal set; }

        private string _regularPattern;

        private const string placeholder = "\"__RESTar__\"";
        private const string macro = "$data";

        /// <summary>
        /// The serialization pattern when prettyprint is set to false
        /// </summary>
        public string RegularPattern
        {
            get => _regularPattern;
            internal set
            {
                _regularPattern = value;
                (RegularPre, RegularPost) = value.TSplit(macro);
                var prettyPrintPattern = Providers.Json.SerializeFormatter(
                    JToken.Parse(RegularPre + placeholder + RegularPost), out var indents);
                (PrettyPrintPre, PrettyPrintPost) = prettyPrintPattern.TSplit(placeholder);
                StartIndent = indents;
            }
        }

        private bool _isDefault;

        /// <summary>
        /// Is this the default pattern?
        /// </summary>
        public bool IsDefault
        {
            get => _isDefault;
            set
            {
                if (value)
                {
                    GetAll().ForEach(f => f._isDefault = false);
                    _isDefault = true;
                }
                else _isDefault = false;
            }
        }

        /// <summary>
        /// Pre-data serialization pattern when prettyprint is set to false
        /// </summary>
        public string RegularPre { get; private set; }

        /// <summary>
        /// Post-data serialization pattern when prettyprint is set to false
        /// </summary>
        public string RegularPost { get; private set; }

        /// <summary>
        /// Pre-data serialization pattern when prettyprint is set to true
        /// </summary>
        public string PrettyPrintPre { get; private set; }

        /// <summary>
        /// Post-data serialization pattern when prettyprint is set to true
        /// </summary>
        public string PrettyPrintPost { get; private set; }

        /// <summary>
        /// The indentation level at which prettyprinted json is serialized
        /// </summary>
        public int StartIndent { get; private set; }

        /// <summary/>
        [Obsolete] public string PrettyPrintPattern { get; internal set; }

        #endregion

        internal DbOutputFormat() { }

        private const string RawPattern = macro;
        private const string SimplePattern = "{\"data\":$data}";
        private const string JSendPattern = "{\"status\":\"success\",\"data\":{\"posts\":$data}}";
        private const string All = "SELECT t FROM RESTar.Admin.DbOutputFormat t";
        private const string ByDefault = All + " WHERE t.IsDefault =?";
        private const string ByName = All + " WHERE t.Name =?";

        internal Formatter Format => _PrettyPrint
            ? new Formatter(Name, PrettyPrintPre, PrettyPrintPost, StartIndent)
            : new Formatter(Name, RegularPre, RegularPost, StartIndent);

        internal static Formatter Default => Db.SQL<DbOutputFormat>(ByDefault, true).FirstOrDefault()?.Format ?? default;
        internal static Formatter Raw => Db.SQL<DbOutputFormat>(ByName, nameof(Raw)).FirstOrDefault()?.Format ?? default;
        internal static IEnumerable<DbOutputFormat> GetAll() => Db.SQL<DbOutputFormat>(All);
        internal static DbOutputFormat GetByName(string formatName) => Db.SQL<DbOutputFormat>(ByName, formatName).FirstOrDefault();

        internal static void Init()
        {
            if (GetAll().All(format => format.Name != "Raw"))
                Db.TransactAsync(() => new DbOutputFormat
                {
                    Name = "Raw",
                    RegularPattern = RawPattern
                });
            if (GetAll().All(format => format.Name != "Simple"))
                Db.TransactAsync(() => new DbOutputFormat
                {
                    Name = "Simple",
                    RegularPattern = SimplePattern
                });
            if (GetAll().All(format => format.Name != "JSend"))
                Db.TransactAsync(() => new DbOutputFormat
                {
                    Name = "JSend",
                    RegularPattern = JSendPattern
                });
            if (GetAll().All(format => !format.IsDefault))
            {
                var raw = Db.SQL<DbOutputFormat>(ByName, "Raw").First();
                Db.TransactAsync(() => raw._isDefault = true);
            }
        }
    }
}