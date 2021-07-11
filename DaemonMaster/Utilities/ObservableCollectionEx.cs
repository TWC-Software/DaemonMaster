using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace DaemonMaster.Utilities
{
    public class ObservableCollectionEx<T> : ObservableCollection<T> where T : INotifyPropertyChanged
    {
        /// <summary>
        /// Occurs when a property is changed within an item.
        /// </summary>
        public event EventHandler<ItemPropertyChangedEventArgs> ItemPropertyChanged;


        public ObservableCollectionEx() : base()
        { }

        public ObservableCollectionEx(List<T> list) : base(list)
        {
            SubscribeAllItemPropertyChangedEvents();
        }

        public ObservableCollectionEx(IEnumerable<T> enumerable) : base(enumerable)
        {
            SubscribeAllItemPropertyChangedEvents();
        }


        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove ||
                e.Action == NotifyCollectionChangedAction.Replace)
            {
                foreach (T item in e.OldItems)
                    item.PropertyChanged -= ChildPropertyChanged;
            }

            if (e.Action == NotifyCollectionChangedAction.Add ||
                e.Action == NotifyCollectionChangedAction.Replace)
            {
                foreach (T item in e.NewItems)
                    item.PropertyChanged += ChildPropertyChanged;
            }

            base.OnCollectionChanged(e);
        }

        protected override void ClearItems()
        {
            foreach (T item in Items)
                item.PropertyChanged -= ChildPropertyChanged;

            base.ClearItems();
        }

        protected void OnItemPropertyChanged(ItemPropertyChangedEventArgs e)
        {
            ItemPropertyChanged?.Invoke(this, e);
        }

        public void AddRange(IEnumerable<T> enumerable)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));

            foreach (var item in enumerable)
            {
                Add(item);
            }
        }

        public void RemoveRange(IEnumerable<T> enumerable)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));

            foreach (var item in enumerable)
            {
                Remove(item);
            }
        }

        private void ChildPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            T item = (T)sender;

            int index = IndexOf(item);
            if(index < 0)
                throw new IndexOutOfRangeException("ChildPropertyChanged: the item is not from this list.");

            OnItemPropertyChanged(new ItemPropertyChangedEventArgs(index, e.PropertyName));
        }

        private void SubscribeAllItemPropertyChangedEvents()
        {
            foreach (T item in Items)
                item.PropertyChanged += ChildPropertyChanged;
        }
    }

    public class ItemPropertyChangedEventArgs : PropertyChangedEventArgs
    {
        /// <summary>
        /// Gets the index of the item in the collection where the change occurred.
        /// </summary>
        public int ItemIndex { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemPropertyChangedEventArgs"/> class.
        /// </summary>
        /// <param name="index">The index of the item in the collection.</param>
        /// <param name="propertyName">Name of the property than changed.</param>
        public ItemPropertyChangedEventArgs(int index, string propertyName) : base(propertyName)
        {
            ItemIndex = index;
        }
    }
}
