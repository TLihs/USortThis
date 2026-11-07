using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USortThis.Core.Models
{
    /// <summary>
    /// Provides the base class for list models.
    /// </summary>
    /// <typeparam name="T">The type of the items in the list.</typeparam>
    public abstract class ListModelBase<T> : ModelBase
    {
        /// <summary>
        /// Occurs when an item is added to the list.
        /// </summary>
        public event EventHandler<ListItemEventArgs<T>>? ItemAdded;
        /// <summary>
        /// Occurs when an item is removed from the list.
        /// </summary>
        public event EventHandler<ListItemEventArgs<T>>? ItemRemoved;

        /// <summary>
        /// Initializes a new instance of the <see cref="ListModelBase{T}"/> class.
        /// </summary>
        /// <param name="id">The identifier for the model.</param>
        public ListModelBase(long id) : base(id)
        {

        }

        /// <summary>
        /// Raises the <see cref="ItemAdded"/> event.
        /// </summary>
        /// <param name="item">The item that was added.</param>
        protected virtual void OnItemAdded(T item)
        {
            ItemAdded?.Invoke(this, new ListItemEventArgs<T>(item));
        }

        /// <summary>
        /// Raises the <see cref="ItemRemoved"/> event.
        /// </summary>
        /// <param name="item">The item that was removed.</param>
        protected virtual void OnItemRemoved(T item)
        {
            ItemRemoved?.Invoke(this, new ListItemEventArgs<T>(item));
        }
    }

    /// <summary>
    /// Provides data for the <see cref="ListModelBase{T}.ItemAdded"/> and <see cref="ListModelBase{T}.ItemRemoved"/> events.
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    public class ListItemEventArgs<T> : EventArgs
    {
        /// <summary>
        /// Gets the item associated with the event.
        /// </summary>
        public T Item { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ListItemEventArgs{T}"/> class.
        /// </summary>
        /// <param name="item">The item associated with the event.</param>
        public ListItemEventArgs(T item)
        {
            Item = item;
        }
    }
}
