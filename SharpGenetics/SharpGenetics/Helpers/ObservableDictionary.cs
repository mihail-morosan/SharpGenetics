using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// From http://www.broculos.net/2014/03/wpf-editable-datagrid-and.html
/// </summary>

namespace SharpGenetics.Helpers
{
    public class Pair<TKey, TValue> : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected TKey _key;
        protected TValue _value;

        public TKey Key
        {
            get { return _key; }
            set
            {
                if (
                    (_key == null && value != null)
                    || (_key != null && value == null)
                    || !_key.Equals(value))
                {
                    _key = value;
                    NotifyPropertyChanged("Key");
                }
            }
        }

        public TValue Value
        {
            get { return _value; }
            set
            {
                if (
                    (_value == null && value != null)
                    || (_value != null && value == null)
                    || (_value != null && !_value.Equals(value)))
                {
                    _value = value;
                    NotifyPropertyChanged("Value");
                }
            }
        }

        public Pair()
        {
        }

        public Pair(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        public Pair(KeyValuePair<TKey, TValue> kv)
        {
            Key = kv.Key;
            Value = kv.Value;
        }

        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class ObservableDictionary<TKey, TValue> : ObservableCollection<Pair<TKey, TValue>>, IDictionary<TKey, TValue>
    {
        public ObservableDictionary(): base()
        {
        }

        public ObservableDictionary(IEnumerable<Pair<TKey, TValue>> enumerable): base(enumerable)
        {
        }

        public ObservableDictionary(List<Pair<TKey, TValue>> list) : base(list)
        {
        }

        public ObservableDictionary(IDictionary<TKey, TValue> dictionary)
        {
            foreach (var kv in dictionary)
            {
                Add(new Pair<TKey, TValue>(kv));
            }
        }

        public void Add(TKey key, TValue value)
        {
            Add(new Pair<TKey, TValue>(key, value));
        }

        public bool ContainsKey(TKey key)
        {
            foreach (var item in base.Items)
            {
                if (EqualityComparer<TKey>.Default.Equals(item.Key, key))
                {
                    return true;
                }
            }
            return false;
        }

        public ICollection<TKey> Keys
        {
            get
            {
                List<TKey> keys = new List<TKey>();
                foreach (var item in base.Items)
                {
                    keys.Add(item.Key);
                }
                return keys;
            }
        }

        public bool Remove(TKey key)
        {
            if (!ContainsKey(key))
                return false;
            Pair < TKey, TValue> pairToRemove = new Pair<TKey, TValue> ();
            foreach (var item in base.Items)
            {
                if (EqualityComparer<TKey>.Default.Equals(item.Key, key))
                {
                    pairToRemove = item;
                    break;
                }
            }
            this.Remove(pairToRemove);
            return true;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (!this.ContainsKey(key))
            {
                value = default(TValue);
                return false;
            }
            foreach (var item in base.Items)
            {
                if (EqualityComparer<TKey>.Default.Equals(item.Key, key))
                {
                    value = item.Value;
                    return true;
                }
            }
            value = default(TValue);
            return false;
        }

        public ICollection<TValue> Values
        {
            get
            {
                List<TValue> values = new List<TValue>();
                foreach (var item in base.Items)
                {
                    values.Add(item.Value);
                }
                return values;
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                TValue value;
                if (TryGetValue(key, out value))
                    return value;
                else
                    throw new KeyNotFoundException();

            }
            set
            {
                foreach (var item in base.Items)
                {
                    if (EqualityComparer<TKey>.Default.Equals(item.Key, key))
                    {
                        item.Value = value;
                        return;
                    }
                }
            }
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public new IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            List < KeyValuePair < TKey, TValue>> list = new List<KeyValuePair<TKey, TValue>> ();
            foreach (var item in base.Items)
            {
                list.Add(new KeyValuePair<TKey, TValue> (item.Key, item.Value));
            }
            return list.GetEnumerator();
        }

        public bool ContainsValue(TValue value)
        {
            foreach (var item in base.Items)
            {
                if (EqualityComparer<TValue>.Default.Equals(value, item.Value))
                {
                    return true;
                }
            }
            return false;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (!ContainsKey(item.Key))
                return false;
            Pair <TKey, TValue> pairToRemove = new Pair<TKey, TValue> ();
            foreach (var innerItem in base.Items)
            {
                if (EqualityComparer<TKey>.Default.Equals(item.Key, innerItem.Key))
                {
                    pairToRemove = innerItem;
                    break;
                }
            }
            this.Remove(pairToRemove);
            return true;
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            this.Add(item.Key, item.Value);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            foreach (var innerItem in base.Items)
            {
                if (EqualityComparer<TKey>.Default.Equals(item.Key, innerItem.Key) && EqualityComparer<TValue>.Default.Equals(item.Value, innerItem.Value))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
