using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.RDS.Nodes;
using Amazon.AWSToolkit.RDS.Model;
using Amazon.AWSToolkit.RDS.View;

using Amazon.RDS;
using Amazon.RDS.Model;

namespace Amazon.AWSToolkit.RDS.Controller
{
    public abstract class RDSFeatureController<M> : BaseContextCommand where M : new()
    {
        IAmazonRDS _rdsClient;
        M _model;
        string _endpointUniqueIdentifier;
        RDSFeatureViewModel _featureViewModel;

        public override ActionResults Execute(IViewModel model)
        {
            this._featureViewModel = model as RDSFeatureViewModel;
            if (this._featureViewModel == null)
                return new ActionResults().WithSuccess(false);

            this._endpointUniqueIdentifier = ((IEndPointSupport)this._featureViewModel.Parent).CurrentEndPoint.UniqueIdentifier;
            this._rdsClient = this._featureViewModel.RDSClient;
            this._model = new M();

            DisplayView();

            return new ActionResults().WithSuccess(true);
        }

        protected abstract void DisplayView();

        public IAmazonRDS RDSClient
        {
            get { return this._rdsClient; }
        }

        protected RDSFeatureViewModel FeatureViewModel
        {
            get { return this._featureViewModel; }
        }

        public M Model
        {
            get { return this._model; }
        }

        public string EndPointUniqueIdentifier
        {
            get
            {
                return this._endpointUniqueIdentifier;
            }
        }

        public AccountViewModel Account
        {
            get { return this._featureViewModel.AccountViewModel; }
        }

        public string RegionDisplayName
        {
            get { return this._featureViewModel.RegionDisplayName; }
        }

        public string RegionSystemName
        {
            get { return this.FeatureViewModel.RegionSystemName; }
        }
    }
}
