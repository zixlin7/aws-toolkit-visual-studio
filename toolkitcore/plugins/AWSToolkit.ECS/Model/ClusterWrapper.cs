using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CommonUI;
using Amazon.ECS.Model;
using System.Windows.Media;

namespace Amazon.AWSToolkit.ECS.Model
{
    public class ClusterWrapper : PropertiesModel, IWrapper
    {
        private Cluster _cluster;
        private readonly string _clusterArn;

        public ClusterWrapper(Cluster cluster)
        {
            _cluster = cluster;
            _clusterArn = cluster.ClusterArn;
        }

        public ClusterWrapper(string arn)
        {
            _clusterArn = arn;
        }

        public void LoadFrom(Cluster cluster)
        {
            _cluster = cluster;
            NotifyPropertyChanged("Name");
            NotifyPropertyChanged("Services");
            NotifyPropertyChanged("RunningTasks");
            NotifyPropertyChanged("PendingTasks");
            NotifyPropertyChanged("ContainerInstances");
            NotifyPropertyChanged("Status");
            NotifyPropertyChanged("StatusHealthColor");
            NotifyPropertyChanged("ClusterArn");
            NotifyPropertyChanged("DisplayName");
        }

        public bool IsLoaded
        {
            get { return _cluster != null; }
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
            get
            {
                return _cluster != null ? _cluster.ClusterName : _clusterArn.Split('/').LastOrDefault();
            }
        }

        [DisplayName("Services")]
        public int Services
        {
            get { return _cluster?.ActiveServicesCount ?? 0; }
        }

        [DisplayName("Running tasks")]
        public int RunningTasks
        {
            get { return _cluster?.RunningTasksCount ?? 0; }
        }

        [DisplayName("Pending tasks")]
        public int PendingTasks
        {
            get { return _cluster?.PendingTasksCount ?? 0; }
        }

        [DisplayName("Container instances")]
        public int ContainerInstances
        {
            get { return _cluster?.RegisteredContainerInstancesCount ?? 0; }
        }

        [DisplayName("Status")]
        public string Status
        {
            get { return _cluster?.Status ?? string.Empty; }
        }

        public SolidColorBrush StatusHealthColor
        {
            get
            {
                Color clr;
                switch (this.Status)
                {
                    case "ACTIVE":
                        clr = Colors.Green;
                        break;

                    case "INACTIVE":
                        clr = Colors.Blue;
                        break;

                    default:
                        clr = ThemeUtil.GetCurrentTheme() == VsTheme.Dark
                            ? Colors.White
                            : new Color() { A = 255 };
                        break;
                }

                return new SolidColorBrush(clr);
            }
        }

        [DisplayName("ARN")]
        public string ClusterArn
        {
            get { return _clusterArn; }
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
                return string.Format("{0} ({1})", Name, _clusterArn);
            }
        }

        [Browsable(false)]
        public System.Windows.Media.ImageSource ClusterIcon
        {
            get
            {
                var icon = IconHelper.GetIcon("clusters.png");
                return icon.Source;
            }
        }

    }
}
