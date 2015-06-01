using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.Util
{
    public class Mutable<T>
    {
        public Mutable(T value)
        {
            this.Value = value;
        }

        public T Value
        {
            get;
            set;
        }
    }
}
