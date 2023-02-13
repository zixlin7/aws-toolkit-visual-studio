using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.IdentityManagement.Model;
using Amazon.AWSToolkit.IdentityManagement.Nodes;
using Amazon.AWSToolkit.IdentityManagement.Util;
using Amazon.AWSToolkit.IdentityManagement.View;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.IdentityManagement.Model;

namespace Amazon.AWSToolkit.IdentityManagement.Controller
{
    public class CreateGroupController : BaseContextCommand
    {
        private readonly ToolkitContext _toolkitContext;
        CreateGroupControl _control;
        CreateGroupModel _model;
        IAMGroupRootViewModel _rootModel;
        ActionResults _results;

        public CreateGroupController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public override ActionResults Execute(IViewModel model)
        {
            var result = CreateGroup(model);
            RecordMetric(result);
            return result;
        }

        public ActionResults CreateGroup(IViewModel model)
        {
            _rootModel = model as IAMGroupRootViewModel;
            if (_rootModel == null)
            {
                return ActionResults.CreateFailed();
            }

            _model = new CreateGroupModel();
            _control = new CreateGroupControl(this);
            if (!_toolkitContext.ToolkitHost.ShowModal(_control))
            {
                return ActionResults.CreateCancelled();
            }

            return _results ?? ActionResults.CreateFailed();
        }

        public CreateGroupModel Model => this._model;

        public void Persist()
        {
            var request = new CreateGroupRequest() { GroupName = this.Model.GroupName.Trim() };
            var response = this._rootModel.IAMClient.CreateGroup(request);

            this._results = new ActionResults()
                .WithSuccess(true)
                .WithParameter(IAMActionResultsConstants.PARAM_IAM_GROUP, response.Group);
        }

        public void RecordMetric(ActionResults results)
        {
            var awsConnectionSettings = _rootModel?.IAMRootViewModel?.AwsConnectionSettings;
            _toolkitContext.RecordIamCreate(IamResourceType.Group, results,
                 awsConnectionSettings);
        }
    }
}
