using System.Collections.Generic;
using RESTar.Requests;
using RESTar.Resources.Operations;

namespace RESTar.Resources.Templates
{
    /// <inheritdoc cref="ISelector{T}" />
    /// <inheritdoc cref="IUpdater{T}" />
    /// <summary>
    /// Represents a form resource that can be fetched, populated and returned
    /// </summary>
    public abstract class Form<T> : ISelector<T>, IUpdater<T> where T : Form<T>, new()
    {
        private bool _isSubmitted;

        /// <summary>
        /// Has this form been submitted?
        /// </summary>
        public bool IsSubmitted
        {
            get => _isSubmitted;
            set
            {
                _isSubmitted = value;
                if (value) OnSubmit();
            }
        }

        /// <summary>
        /// This method is called when a form has been updated
        /// </summary>
        protected abstract void OnSubmit();

        IEnumerable<T> ISelector<T>.Select(IRequest<T> request)
        {
            yield return new T();
        }

        int IUpdater<T>.Update(IRequest<T> request)
        {
            var count = 0;
            foreach (var item in request.GetInputEntities())
            {
                item.OnSubmit();
                count += 1;
            }
            return count;
        }
    }
}