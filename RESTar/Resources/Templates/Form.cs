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
                if (value)
                {
                    PreSubmit();
                    _isSubmitted = true;
                    PostSubmit();
                }
            }
        }

        /// <summary>
        /// This method is called before a form is submitted
        /// </summary>
        protected abstract void PreSubmit();

        /// <summary>
        /// This method is called after a form has been submitted
        /// </summary>
        protected abstract void PostSubmit();

        IEnumerable<T> ISelector<T>.Select(IRequest<T> request)
        {
            yield return new T();
        }

        int IUpdater<T>.Update(IRequest<T> request)
        {
            return request.GetInputEntities().Count();
        }
    }
}