using System;
using System.Collections.Generic;
using RESTar.Linq;
using RESTar.Resources;
using RESTar.Resources.Templates;
using Starcounter;

namespace RESTar.Admin
{
    /// <inheritdoc />
    /// <summary>
    /// Sends feed messages representing generated queries in e.g. SQL
    /// </summary>
    [RESTar]
    public class QueryConsole : FeedTerminal
    {
        private static TerminalSet<QueryConsole> Consoles { get; }
        static QueryConsole() => Consoles = new TerminalSet<QueryConsole>();

        /// <inheritdoc />
        public override void Open()
        {
            base.Open();
            Consoles.Add(this);
        }

        /// <inheritdoc />
        public override void Dispose() => Consoles.Remove(this);

        internal static void Publish<T>(string query, object[] args, Func<IEnumerator<T>> getEnumerator)
        {
            if (Consoles.Count == 0) return;
            var argsString = args == null ? null : "\r\nArgs: " + string.Join(", ", args);
            string queryPlan;
            if (getEnumerator != null)
                using (var enumerator = getEnumerator())
                    queryPlan = enumerator.ToString();
            else queryPlan = "No query plan available";
            var message = $"{DateTime.UtcNow:O}: {query}{argsString}\r\n{queryPlan.Replace("\t", "  ")}";
            Scheduling.RunTask(() => Consoles.ForEach(c => c.WebSocket.SendText(message)));
        }
    }
}