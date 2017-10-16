using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.ECS.Model
{
    public class ViewClusterModel : BaseModel
    {
        public ClusterWrapper Cluster { get; internal set; }

        public ObservableCollection<ServiceWrapper> Services { get; } = new ObservableCollection<ServiceWrapper>();
    }
}
