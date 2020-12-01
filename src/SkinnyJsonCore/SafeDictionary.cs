using System.Collections.Generic;
using System.Linq;

namespace SkinnyJson {
    /// <summary>
    /// Dictionary with thread locks
    /// </summary>
	internal class SafeDictionary<TKey, TValue> {
		private readonly object _padlock = new object();
		private readonly Dictionary<TKey, TValue> _dictionary;

		public SafeDictionary () {
			_dictionary = new Dictionary<TKey, TValue>();
		}

        public TKey[] Keys {
            get {
                lock(_padlock){
                    return _dictionary.Keys.ToArray();
                }
            }
        }

		public bool TryGetValue (TKey key, out TValue value) {
			lock (_padlock) {
			    return _dictionary.TryGetValue(key, out value);
            }
		}

		public TValue this[TKey key] {
			get { lock (_padlock) return _dictionary[key]; }
			set { lock (_padlock) _dictionary[key] = value; }
		}

		public void Add (TKey key, TValue value) {
			lock (_padlock) {
				if (_dictionary.ContainsKey(key) == false) _dictionary.Add(key, value);
			}
		}
	}
}
