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
