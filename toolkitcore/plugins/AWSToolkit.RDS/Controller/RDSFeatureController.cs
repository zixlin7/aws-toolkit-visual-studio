using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.RDS.Nodes;
using Amazon.RDS;

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

        public IAmazonRDS RDSClient => this._rdsClient;

        protected RDSFeatureViewModel FeatureViewModel => this._featureViewModel;

        public M Model => this._model;

        public string EndPointUniqueIdentifier => this._endpointUniqueIdentifier;

        public AccountViewModel Account => this._featureViewModel.AccountViewModel;

        public string RegionDisplayName => this._featureViewModel.RegionDisplayName;

        public string RegionSystemName => this.FeatureViewModel.RegionSystemName;
    }
}
