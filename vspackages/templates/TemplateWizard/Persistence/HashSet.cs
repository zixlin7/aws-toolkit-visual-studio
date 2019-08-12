using System.Collections;
using System.Collections.Generic;

namespace Amazon.AWSToolkit.Persistence
{
    class HashSet<T> : ICollection<T>, IEnumerable<T>, IEnumerable
    {
        #region Private members

        private Dictionary<T, object> _data = new Dictionary<T, object>();

        #endregion

        #region ICollection<T> Members

        public void Add(T item)
        {
            _data.Add(item, null);
        }

        public void Clear()
        {
            _data.Clear();
        }

        public bool Contains(T item)
        {
            return (_data.ContainsKey(item));
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _data.Keys.CopyTo(array, arrayIndex);
        }

        public int Count => _data.Keys.Count;

        public bool IsReadOnly => false;

        public bool Remove(T item)
        {
            return _data.Remove(item);
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            return _data.Keys.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _data.Keys.GetEnumerator();
        }

        #endregion

        #region IEnumerable<T> Members

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _data.Keys.GetEnumerator();
        }

        #endregion
    }
}
