using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;


namespace CommonTypes
{
    public class ObservableList<T> : List<T>
    {
        public EventHandler<CollectionChangedArgs> ChangeEvent;


        public ObservableList(EventHandler<CollectionChangedArgs> e)
            : base()
        {
            ChangeEvent = e;
        }


        public new void Add(T item)
        {
            base.Add(item);
            ChangeEvent(this, new CollectionChangedArgs(item));
        }
    }

    public class ObservableDictionary<K, V> : ConcurrentDictionary<K, V>
    {
        public EventHandler<CollectionChangedArgs> ChangeEvent;


        public ObservableDictionary(EventHandler<CollectionChangedArgs> e)
            : base()
        {
            ChangeEvent = e;
        }


        public void BaseAdd(K key, V value)
        {
            base.TryAdd(key, value);
        }


        public void Add(K key, V value)
        {
            BaseAdd(key, value);
            ChangeEvent(this, new CollectionChangedArgs(value));
        }


        public void Update(K key, V value)
        {
            if (!base[key].Equals(value))
            {
                base[key] = value;
                ChangeEvent(this, new CollectionChangedArgs(value));
            }
        }


        public void AddOrUpdate(K key, V value)
        {
            if (base.ContainsKey(key))
            {
                Update(key, value);
            }
            else
            {
                Add(key, value);
            }
        }
    }


    public class CollectionChangedArgs : EventArgs
    {
        public object Object { get; set; }

        public CollectionChangedArgs(object o)
        {
            Object = o;
        }
    }
}
