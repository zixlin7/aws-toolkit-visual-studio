using System.Collections.ObjectModel;

namespace Amazon.AWSToolkit.S3.Model
{
    public interface IMetadataContainerModel
    {
        ObservableCollection<Metadata> MetadataEntries
        {
            get;
            set;
        }
    }
}
