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
            this._region = RegionEndPointsManager.Instance.GetDefaultRegionEndPoints();
            this._endPoint = this._region.GetEndpoint(this.MetaNode.EndPointSystemName);
            BuildClient(this.AccountViewModel.AccessKey, this.AccountViewModel.SecretKey);
        }

        public RegionEndPointsManager.RegionEndPoints CurrentRegion
        {
            get { return this._region; }
        }

        public RegionEndPointsManager.EndPoint CurrentEndPoint
        {
            get { return this._endPoint; }
        }

        protected abstract void BuildClient(string accessKey, string secretKey);

        public void UpdateEndPoint(string regionName)
        {
            this._region = RegionEndPointsManager.Instance.GetRegion(regionName);
            this._endPoint = this._region.GetEndpoint(this.MetaNode.EndPointSystemName);
            this.BuildClient(this.AccountViewModel.AccessKey, this.AccountViewModel.SecretKey);
            this.Refresh(true);
        }

    }
}
