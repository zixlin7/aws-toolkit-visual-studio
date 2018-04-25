using Amazon.Runtime;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;


namespace Amazon.AWSToolkit.Navigator.Node
{
    public abstract class ServiceRootViewModel : InstanceDataRootViewModel, IServiceRootViewModel
    {
        RegionEndPointsManager.RegionEndPoints _region;
        RegionEndPointsManager.EndPoint _endPoint;
        string _baseName;


        public ServiceRootViewModel(IMetaNode metaNode, IViewModel parent, string name)
            : base(metaNode, parent, name)
        {
            this._baseName = name;
            this._region = RegionEndPointsManager.GetInstance().GetDefaultRegionEndPoints();
            this._endPoint = this._region.GetEndpoint(this.MetaNode.EndPointSystemName);
            BuildClient(this.AccountViewModel.Credentials);
        }

        public RegionEndPointsManager.RegionEndPoints CurrentRegion
        {
            get { return this._region; }
        }

        public RegionEndPointsManager.EndPoint CurrentEndPoint
        {
            get { return this._endPoint; }
        }

        protected abstract void BuildClient(AWSCredentials credentials);

        public void UpdateEndPoint(string regionName)
        {
            this._region = RegionEndPointsManager.GetInstance().GetRegion(regionName);
            this._endPoint = this._region.GetEndpoint(this.MetaNode.EndPointSystemName);
            this.BuildClient(this.AccountViewModel.Credentials);
            this.Refresh(true);
        }

    }
}
