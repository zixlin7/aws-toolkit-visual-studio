using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amazon.AWSToolkit.S3.Model
{
    public interface ITagContainerModel
    {
        ObservableCollection<Tag> Tags { get; }
    }
}
