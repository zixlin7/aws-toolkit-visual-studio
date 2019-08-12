namespace Amazon.AWSToolkit.CommonUI
{
    /// <summary>
    /// Used to wrap objects bound to lists etc to allow multi-select.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SelectableItem<T>
    {
        public bool IsSelected { get; set; }
        public T InnerObject { get; set; }

        public SelectableItem(T innerObject)
            : this(innerObject, false)
        {
        }

        public SelectableItem(T innerObject, bool isSelected)
        {
            this.InnerObject = innerObject;
            this.IsSelected = isSelected;
        }
    }
}
