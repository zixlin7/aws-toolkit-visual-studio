using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.IdentityManagement.View;
using Amazon.AWSToolkit.IdentityManagement.Model;
using Amazon.AWSToolkit.IdentityManagement.Nodes;
using Amazon.IdentityManagement.Model;
using Amazon.AWSToolkit.Context;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.IdentityManagement.Util;

namespace Amazon.AWSToolkit.IdentityManagement.Controller
{
    public class CreateUserController : BaseContextCommand
    {
        private readonly ToolkitContext _toolkitContext;
        CreateUserControl _control;
        CreateUserModel _model;
        IAMUserRootViewModel _rootModel;
        ActionResults _results;


        public CreateUserController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public override ActionResults Execute(IViewModel model)
        {
            var result = CreateUser(model);
            RecordMetric(result);
            return result;
        }

        public ActionResults CreateUser(IViewModel model)
        {
            _rootModel = model as IAMUserRootViewModel;
            if (_rootModel == null)
            {
                return ActionResults.CreateFailed();
            }

            _model = new CreateUserModel();
            _control = new CreateUserControl(this);

            if (!_toolkitContext.ToolkitHost.ShowModal(_control))
            {
                return ActionResults.CreateCancelled();
            }

            return _results ?? ActionResults.CreateFailed();
        }

        public CreateUserModel Model => this._model;

        public void Persist()
        {
            var request = new CreateUserRequest() { UserName = this.Model.UserName.Trim() };
            var response = this._rootModel.IAMClient.CreateUser(request);

            this._results = new ActionResults()
                .WithSuccess(true)
                .WithParameter(IAMActionResultsConstants.PARAM_IAM_USER, response.User);
        }

        public void RecordMetric(ActionResults results)
        {
            var awsConnectionSettings = _rootModel?.IAMRootViewModel?.AwsConnectionSettings;
            _toolkitContext.RecordIamCreate(IamResourceType.User, results,
                 awsConnectionSettings);
        }
    }
}
