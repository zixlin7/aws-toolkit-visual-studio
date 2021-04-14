using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Amazon.AWSToolkit.Util
{
    /// <summary>
    /// Represents a Most Recently Used List of Items
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MruList<T>: IEnumerable<T>
    {
        private readonly List<T> _items;
        private readonly int _maxSize;
        public const int NO_SIZE_LIMIT = -1;

        public MruList(int size)
        {
            _maxSize = size;
            _items = new List<T>();
        }

        public void Add(T element)
        {
            _items.Remove(element);
            _items.Insert(0, element);
            TrimListToSize();
        }

        public void Clear()
        {
            _items.Clear();
        }

        public List<T> ToList()
        {
            return _items.ToList();
        }

        public int Count()
        {
            return _items.Count;
        }

        public IEnumerator<T> GetEnumerator()
        {
           return  _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        private void TrimListToSize()
        {
            if (_maxSize > NO_SIZE_LIMIT)
            {
                while (_items.Count > _maxSize)
                {
                    _items.RemoveAt(_items.Count - 1);
                }
            }
        }
    }
}
