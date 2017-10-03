using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.ECS.View.DataGrid;
using Amazon.ECS.Model;

namespace Amazon.AWSToolkit.ECS.Model
{
    public class ViewClustersModel : BaseModel
    {
        readonly ObservableCollection<Cluster> _clusters = new ObservableCollection<Cluster>();
        public ObservableCollection<Cluster> Clusters
        {
            get { return this._clusters; }
        }

        readonly IList<Cluster> _selectedClusters = new List<Cluster>();
        public IList<Cluster> SelectedClusters
        {
            get { return this._selectedClusters; }
        }

        Cluster _focusCluster;
        public Cluster FocusInstance
        {
            get { return this._focusCluster; }
            set
            {
                this._focusCluster = value;
                base.NotifyPropertyChanged("FocusCluster");
            }
        }

        ECSColumnDefinition[] _clusterPropertyColumnDefinitions;
        public ECSColumnDefinition[] ClusterPropertyColumnDefinitions
        {
            get
            {
                return this._clusterPropertyColumnDefinitions 
                    ?? (this._clusterPropertyColumnDefinitions =
                           ECSColumnDefinition.GetPropertyColumnDefinitions(typeof(ClusterWrapper)));
            }
        }

    }
}
