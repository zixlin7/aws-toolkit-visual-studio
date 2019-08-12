using Amazon.S3.Model;
using System.Collections.ObjectModel;

namespace Amazon.AWSToolkit.S3.Model
{
    public interface ITagContainerModel
    {
        ObservableCollection<Tag> Tags { get; }
    }
}
