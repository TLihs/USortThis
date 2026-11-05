using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USortThis.Core.Models
{
    public abstract class ListModelBase<T> : ModelBase
    {
        public event EventHandler<ListItemEventArgs<T>>? ItemAdded;
        public event EventHandler<ListItemEventArgs<T>>? ItemRemoved;

        public ListModelBase(long id) : base(id)
        {

        }

        protected virtual void OnItemAdded(T item)
        {
            ItemAdded?.Invoke(this, new ListItemEventArgs<T>(item));
        }

        protected virtual void OnItemRemoved(T item)
        {
            ItemRemoved?.Invoke(this, new ListItemEventArgs<T>(item));
        }
    }

    public class ListItemEventArgs<T> : EventArgs
    {
        public T Item { get; }

        public ListItemEventArgs(T item)
        {
            Item = item;
        }
    }
}
