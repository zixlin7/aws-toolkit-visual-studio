using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.ECS.Model;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.ECS;

namespace Amazon.AWSToolkit.ECS.Controller
{
    public abstract class ECSFeatureController<M> : BaseContextCommand where M : new()
    {
        IAmazonECS _ecsClient;
        M _model;
        string _endpoint;
        ECSFeatureViewModel _featureViewModel;

        public override ActionResults Execute(IViewModel model)
        {
            this._featureViewModel = model as ECSFeatureViewModel;
            if (this._featureViewModel == null)
                return new ActionResults().WithSuccess(false);

            this._endpoint = ((IEndPointSupport)this._featureViewModel.Parent).CurrentEndPoint.Url;
            this._ecsClient = this._featureViewModel.ECSClient;
            this._model = new M();

            DisplayView();

            return new ActionResults().WithSuccess(true);
        }

        protected abstract void DisplayView();

        public IAmazonECS ECSClient
        {
            get { return this._ecsClient; }
        }

        protected ECSFeatureViewModel FeatureViewModel
        {
            get { return this._featureViewModel; }
        }

        public M Model
        {
            get { return this._model; }
        }

        public string EndPoint
        {
            get
            {
                return this._endpoint;
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
