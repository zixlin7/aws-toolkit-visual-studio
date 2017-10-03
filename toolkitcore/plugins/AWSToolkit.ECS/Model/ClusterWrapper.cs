using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.ECS.View.DataGrid;
using Amazon.ECS.Model;

namespace Amazon.AWSToolkit.ECS.Model
{
    public class ClusterWrapper : PropertiesModel, IWrapper
    {
        private readonly Cluster _cluster;

        public ClusterWrapper(Cluster cluster)
        {
            _cluster = cluster;
        }

        public override void GetPropertyNames(out string className, out string componentName)
        {
            className = "Cluster";
            componentName = this._cluster.ClusterName;
        }

        [DisplayName("Name")]
        [AssociatedIcon(true, "ClusterIcon")]
        public string Name
        {
            get { return _cluster.ClusterName; }
        }

        [DisplayName("Services")]
        public int Services
        {
            get { return _cluster.ActiveServicesCount; }
        }

        [DisplayName("Running tasks")]
        public int RunningTasks
        {
            get { return _cluster.RunningTasksCount; }
        }

        [DisplayName("Pending tasks")]
        public int PendingTasks
        {
            get { return _cluster.PendingTasksCount; }
        }

        [DisplayName("Container instances")]
        public int ContainerInstances
        {
            get { return _cluster.RegisteredContainerInstancesCount; }
        }

        [DisplayName("Status")]
        public string Status
        {
            get { return _cluster.Status; }
        }

        [Browsable(false)]
        public string TypeName
        {
            get { return "Cluster"; }
        }

        [Browsable(false)]
        public string DisplayName
        {
            get
            {
                return string.Format("{0} ({1})", _cluster.ClusterName, _cluster.ClusterArn);
            }
        }

        [Browsable(false)]
        public System.Windows.Media.ImageSource InstanceIcon
        {
            get
            {
                var icon = IconHelper.GetIcon("clusters.png");
                return icon.Source;
            }
        }

    }
}
