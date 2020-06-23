namespace Amazon.AWSToolkit.ElasticBeanstalk.Model
{
    /// <summary>
    /// Used to handle multi-select of security groups in a combo box
    /// </summary>
    public class SelectableGroup<T>
    {
        public bool IsSelected { get; set; }
        public T InnerObject { get; set; }
        public string ReferencingDBInstances { get; set; }

        public SelectableGroup(T innerObject, string referencingDatabases)
            : this(innerObject, referencingDatabases, false)
        {
        }

        public SelectableGroup(T innerObject, string referencingDatabases, bool isSelected)
        {
            this.InnerObject = innerObject;
            this.ReferencingDBInstances = referencingDatabases;
            this.IsSelected = isSelected;
        }
    }
}