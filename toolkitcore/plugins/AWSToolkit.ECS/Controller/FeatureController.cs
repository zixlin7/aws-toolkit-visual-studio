using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.ECS.Model;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.ECS;
using Amazon.ECR;

namespace Amazon.AWSToolkit.ECS.Controller
{
    public abstract class FeatureController<M> : BaseContextCommand where M : new()
    {
        private IAmazonECS _ecsClient;
        private IAmazonECR _ecrClient;
        private M _model;
        private string _endpointUniqueIdentifier;
        private FeatureViewModel _featureViewModel;

        public override ActionResults Execute(IViewModel model)
        {
            this._featureViewModel = model as FeatureViewModel;
            if (this._featureViewModel == null)
                return new ActionResults().WithSuccess(false);

            this._endpointUniqueIdentifier = ((IEndPointSupport)this._featureViewModel.Parent).CurrentEndPoint.UniqueIdentifier;
            this._ecsClient = this._featureViewModel.ECSClient;
            this._model = new M();

            DisplayView();

            return new ActionResults().WithSuccess(true);
        }

        protected abstract void DisplayView();

        public IAmazonECS ECSClient
        {
            get => this._ecsClient;
            protected set => this._ecsClient = value;
        }

        public IAmazonECR ECRClient
        {
            get => this._ecrClient;
            protected set => this._ecrClient = value;
        }

        public FeatureViewModel FeatureViewModel => this._featureViewModel;

        public M Model => this._model;

        public string EndPointUniqueIdentifier => this._endpointUniqueIdentifier;

        public AccountViewModel Account => this._featureViewModel.AccountViewModel;

        public string RegionDisplayName => this._featureViewModel.RegionDisplayName;

        public string RegionSystemName => this.FeatureViewModel.RegionSystemName;
    }
}
