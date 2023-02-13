using System;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.IdentityManagement.View;
using Amazon.AWSToolkit.IdentityManagement.Model;
using Amazon.AWSToolkit.IdentityManagement.Nodes;

using Amazon.Auth.AccessControlPolicy;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;

using log4net;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.IdentityManagement.Util;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

namespace Amazon.AWSToolkit.IdentityManagement.Controller
{
    public class EditGroupController : BaseContextCommand
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(EditGroupController));

        private readonly ToolkitContext _toolkitContext;
        IAmazonIdentityManagementService _iamClient;
        EditGroupModel _model;
        IAMGroupViewModel _iamGroupViewModel;

        public EditGroupController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public EditGroupModel Model => this._model;

        public override ActionResults Execute(IViewModel model)
        {
            this._iamGroupViewModel = model as IAMGroupViewModel;
            if (this._iamGroupViewModel == null)
                return new ActionResults().WithSuccess(false);

            this._iamClient = this._iamGroupViewModel.IAMClient;
            this._model = new EditGroupModel();

            this._model.OriginalName = this._iamGroupViewModel.Group.GroupName;
            this._model.NewName = this._iamGroupViewModel.Group.GroupName;

            EditGroupControl control = new EditGroupControl(this);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(control);

            return new ActionResults()
                    .WithSuccess(true);
        }

        public void LoadModel()
        {
            var listPolicyRequest =
                new ListGroupPoliciesRequest() { GroupName = this.Model.OriginalName };
            ListGroupPoliciesResponse listPolicyResponse = null;
            do
            {
                if (listPolicyResponse != null)
                    listPolicyRequest.Marker = listPolicyResponse.Marker;
                listPolicyResponse = this._iamClient.ListGroupPolicies (listPolicyRequest);

                foreach (var policyName in listPolicyResponse.PolicyNames)
                {
                    var policyModel = new IAMPolicyModel {Name = policyName};

                    try
                    {
                        var response = this._iamClient.GetGroupPolicy(new GetGroupPolicyRequest()
                        {
                            GroupName = this.Model.OriginalName,
                            PolicyName = policyName
                        });

                        policyModel.Policy = Policy.FromJson(response.GetDecodedPolicyDocument());

                        ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() => this.Model.IAMPolicyModels.Add(policyModel)));
                    }
                    catch (Exception e)
                    {
                        _logger.Error("Error loading policy " + policyName, e);
                        ToolkitFactory.Instance.ShellProvider.ShowError("Error parsing template " + policyName + ": " + e.Message);
                    }
                }
            }
            while (listPolicyResponse.IsTruncated);

            this.Model.CommitChanges();            
        }


        public void Persist()
        {
            if (!this.Model.OriginalName.Equals(this.Model.NewName))
            {
                var request = new UpdateGroupRequest()
                {
                    GroupName = this.Model.OriginalName,
                    NewGroupName = this.Model.NewName
                };
                this._iamClient.UpdateGroup(request);

                this.Model.OriginalName = this.Model.NewName;

                if (this._iamGroupViewModel != null)
                {
                    this._iamGroupViewModel.UpdateGroup(this.Model.OriginalName);
                }
            }

            foreach (var policyModel in this.Model.DeletedPolicies)
            {
                if (!this.Model.NewPolicies.Contains(policyModel))
                {
                    var request = new DeleteGroupPolicyRequest()
                    {
                        GroupName = this.Model.NewName,
                        PolicyName = policyModel
                    };
                    this._iamClient.DeleteGroupPolicy(request);
                }
            }

            foreach (var policyModel in this.Model.IAMPolicyModels)
            {
                if (policyModel.HasChanged)
                {
                    var request = new PutGroupPolicyRequest()
                    {
                        GroupName = this.Model.OriginalName,
                        PolicyName = policyModel.Name,
                        PolicyDocument = policyModel.Policy.ToJson()
                    };
                    this._iamClient.PutGroupPolicy(request);
                }
            }

            this.Model.CommitChanges();
        }

        public void Refresh()
        {
            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
            {
                this._model.IAMPolicyModels.Clear();
            }));
            this.LoadModel();
        }

        public void RecordEditGroup(ActionResults results)
        {
            var awsConnectionSettings =
                _iamGroupViewModel?.IAMGroupRootViewModel?.IAMRootViewModel?.AwsConnectionSettings;
            _toolkitContext.RecordIamEdit(IamResourceType.Group, results, awsConnectionSettings);
        }
    }
}
