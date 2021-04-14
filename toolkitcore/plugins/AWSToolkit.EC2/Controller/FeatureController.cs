using System;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.AWSToolkit.Regions;
using Amazon.EC2;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public abstract class FeatureController<M> : BaseContextCommand where M : new()
    {
        IAmazonEC2 _ec2Client;
        M _model;
        string _endpointUniqueIdentifier;
        FeatureViewModel _featureViewModel;

        public override ActionResults Execute(IViewModel model)
        {
            this._featureViewModel = model as FeatureViewModel;
            if (this._featureViewModel == null)
                return new ActionResults().WithSuccess(false);

            this._endpointUniqueIdentifier = ((IEndPointSupport)this._featureViewModel.Parent).Region.Id;
            this._ec2Client = this._featureViewModel.EC2Client;
            this._model = new M();

            DisplayView();

            return new ActionResults().WithSuccess(true);
        }

        protected abstract void DisplayView();

        public IAmazonEC2 EC2Client => this._ec2Client;

        protected FeatureViewModel FeatureViewModel => this._featureViewModel;

        public M Model => this._model;

        public string EndPointUniqueIdentifier => this._endpointUniqueIdentifier;

        public AccountViewModel Account => this._featureViewModel.AccountViewModel;

        public string RegionDisplayName => this._featureViewModel.Region.DisplayName ?? string.Empty;

        public ToolkitRegion Region => this.FeatureViewModel.Region;
    }
}
