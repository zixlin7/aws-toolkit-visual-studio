using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.IdentityManagement.View;
using Amazon.AWSToolkit.IdentityManagement.Model;
using Amazon.AWSToolkit.IdentityManagement.Nodes;
using Amazon.IdentityManagement.Model;

namespace Amazon.AWSToolkit.IdentityManagement.Controller
{
    public class CreateGroupController : BaseContextCommand
    {
        CreateGroupControl _control;
        CreateGroupModel _model;
        IAMGroupRootViewModel _rootModel;
        ActionResults _results;

        public override ActionResults Execute(IViewModel model)
        {
            this._rootModel = model as IAMGroupRootViewModel;
            if (this._rootModel == null)
                return new ActionResults().WithSuccess(false);

            this._model = new CreateGroupModel();
            this._control = new CreateGroupControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(this._control);

            if (this._results == null)
                return new ActionResults().WithSuccess(false);
            return this._results;
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
    }
}
