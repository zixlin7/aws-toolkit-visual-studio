using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.CloudFormation.Parser
{
    internal class IndexableStack<T> : IEnumerable<T>
    {
        IList<T> _data = new List<T>();

        public IndexableStack()
        {
        }

        public void Push(T item)
        {
            this._data.Add(item);
        }

        public T Pop()
        {
            T item = this.Peek();
            this._data.RemoveAt(this._data.Count - 1);
            return item;
        }

        public T Peek()
        {
            T item = this._data.Last();
            return item;
        }

        public int Count
        {
            get { return this._data.Count; }
        }


        public T this[int index]
        {
            // This list stores things in reverse order then normal Stacks so reverse the index.
            get { return this._data[this.Count - 1 - index]; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            // Storing this in a list is already in reverse order then a normal stack so we won't actually call reverse.
            return this._data.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this._data.GetEnumerator();
        }
    }
}
