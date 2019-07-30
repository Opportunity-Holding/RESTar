using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RESTar.Requests;
using RESTar.Resources.Operations;

namespace RESTar.Resources.Templates
{
    /// <inheritdoc cref="ISelector{T}" />
    /// <inheritdoc cref="IUpdater{T}" />
    /// <summary>
    /// Represents a form resource that can be fetched, populated and returned
    /// </summary>
    public abstract class Form<T> : ISelector<T>, IUpdater<T>, IPropertyChangeNotifier where T : Form<T>, new()
    {
        /// <summary>
        /// Saved forms
        /// </summary>
        protected IDictionary<string, T> Forms { get; } = new ConcurrentDictionary<string, T>();

        IEnumerable<T> ISelector<T>.Select(IRequest<T> request)
        {
            if (!request.Cookies.TryGetValue("FormId", out var cookie))
            {
                cookie = new Cookie(name: "FormId", value: Guid.NewGuid().ToString("N"));
                request.Cookies.Add(cookie);
            }

            if (!Forms.TryGetValue(cookie.Value, out var form))
                form = Forms[cookie.Value] = new T();

            yield return form;
        }

        int IUpdater<T>.Update(IRequest<T> request)
        {
            return request.GetInputEntities().Count();
        }
    }
}