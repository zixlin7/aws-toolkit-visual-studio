using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

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
