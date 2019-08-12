using System.Collections.ObjectModel;

namespace Amazon.AWSToolkit.S3.Model
{
    public interface IPermissionContainerModel
    {
        ObservableCollection<Permission> PermissionEntries
        {
            get;
            set;
        }
    }
}
