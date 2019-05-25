using System;
using System.Collections.Generic;

namespace RESTar.Meta
{
    public class DependencyTable<T> : IDisposable where T : class
    {
        private static DependencyTable<T> Cache { get; set; }

        private Action<Term, object, object> ReportChange { get; set; }

        public static DependencyTable<T> Create(string termComponentSeparator, Action<Term, object, object> reportServerSideChange)
        {
            var instance = Cache ?? (Cache = new DependencyTable<T>(termComponentSeparator, reportServerSideChange));
            instance.ReportChange = reportServerSideChange;
            return instance;
        }

        private DependencyTable(string termComponentSeparator, Action<Term, object, object> reportServerSideChange)
        {
            ReportChange = reportServerSideChange;

            IEnumerable<Term> recurseProperties(Type type, Term context)
            {
                foreach (var property in type.GetDeclaredProperties().Values)
                {
                    foreach (var nestedProperty in recurseProperties(property.Type, Term.Append(context, property)))
                        yield return nestedProperty;
                    foreach (var definesProperty in property.DefinesTerms)
                        yield return Term.Append(context, definesProperty);
                }
            }

            var terms = recurseProperties
            (
                type: typeof(T),
                context: Term.Empty(termComponentSeparator)
            );


        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}