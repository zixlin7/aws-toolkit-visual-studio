namespace Amazon.AWSToolkit.CommonUI
{
    public delegate void PropertySourceChange(object sender, bool forceShow, System.Collections.IList propertyObjects);
    public interface IPropertySupport
    {        
        event PropertySourceChange OnPropertyChange;
    }
}
