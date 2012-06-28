using System.Collections.Generic;

namespace SevenDigital.Jester.EventStore.Serialisation
{
    internal class SafeDictionary<TKey, TValue>
    {
        private readonly object padlock = new object();
        private readonly Dictionary<TKey, TValue> dictionary;

        public SafeDictionary(int capacity)
        {
            dictionary = new Dictionary<TKey, TValue>(capacity);
        }

        public SafeDictionary()
        {
            dictionary = new Dictionary<TKey, TValue>();
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (padlock)
                return dictionary.TryGetValue(key, out value);
        }

        public TValue this[TKey key]
        {
            get
            {
                lock (padlock)
                    return dictionary[key];
            }
            set
            {
                lock (padlock)
                    dictionary[key] = value;
            }
        }

        public void Add(TKey key, TValue value)
        {
            lock (padlock)
            {
                if (dictionary.ContainsKey(key) == false)
                    dictionary.Add(key, value);
            }
        }
    }
}
