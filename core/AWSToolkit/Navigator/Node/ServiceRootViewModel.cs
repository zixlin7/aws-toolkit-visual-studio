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
        RegionEndPointsManager.EndPoint _endPoint;
        string _baseName;


        public ServiceRootViewModel(IMetaNode metaNode, IViewModel parent, string name)
            : base(metaNode, parent, name)
        {
            this._baseName = name;
            this._endPoint = RegionEndPointsManager.Instance.GetDefaultRegionEndPoints().GetEndpoint(this.MetaNode.EndPointSystemName);
            BuildClient(this.AccountViewModel.AccessKey, this.AccountViewModel.SecretKey);
        }

        public RegionEndPointsManager.EndPoint CurrentEndPoint
        {
            get { return this._endPoint; }
        }

        protected abstract void BuildClient(string accessKey, string secretKey);

        public void UpdateEndPoint(string regionName)
        {
            var region = RegionEndPointsManager.Instance.GetRegion(regionName);
            this._endPoint = region.GetEndpoint(this.MetaNode.EndPointSystemName);
            this.BuildClient(this.AccountViewModel.AccessKey, this.AccountViewModel.SecretKey);
            this.Refresh(true);
        }

    }
}
