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

namespace Amazon.AWSToolkit.IdentityManagement.Controller
{
    public class EditRoleController : BaseContextCommand
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(EditRoleController));

        IAmazonIdentityManagementService _iamClient;
        EditRoleModel _model;
        IAMRoleViewModel _iamRoleViewModel;

        public EditRoleModel Model => this._model;

        public override ActionResults Execute(IViewModel model)
        {
            this._iamRoleViewModel = model as IAMRoleViewModel;
            if (this._iamRoleViewModel == null)
                return new ActionResults().WithSuccess(false);

            this._iamClient = this._iamRoleViewModel.IAMClient;
            this._model = new EditRoleModel();

            this._model.OriginalName = this._iamRoleViewModel.Role.RoleName;
            this._model.NewName = this._iamRoleViewModel.Role.RoleName;

            EditRoleControl control = new EditRoleControl(this);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(control);

            return new ActionResults()
                    .WithSuccess(true);
        }

        public void LoadModel()
        {
            var listPolicyRequest =
                new ListRolePoliciesRequest() { RoleName = this.Model.OriginalName };
            ListRolePoliciesResponse listPolicyResponse = null;
            do
            {
                if (listPolicyResponse != null)
                    listPolicyRequest.Marker = listPolicyResponse.Marker;
                listPolicyResponse = this._iamClient.ListRolePolicies(listPolicyRequest);

                foreach (var policyName in listPolicyResponse.PolicyNames)
                {
                    var policyModel = new IAMPolicyModel {Name = policyName};

                    try
                    {
                        var response = this._iamClient.GetRolePolicy(new GetRolePolicyRequest()
                        {
                            RoleName = this.Model.OriginalName,
                            PolicyName = policyName
                        });

                        policyModel.Policy = Policy.FromJson(response.GetDecodedPolicyDocument());

                        ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() => this.Model.IAMPolicyModels.Add(policyModel)));
                    }
                    catch (Exception e)
                    {
                        LOGGER.Error("Error loading policy " + policyName, e);
                        ToolkitFactory.Instance.ShellProvider.ShowError("Error parsing template " + policyName + ": " + e.Message);
                    }
                }
            }
            while (listPolicyResponse.IsTruncated);

            this.Model.CommitChanges();
        }

        public void Refresh()
        {
            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() => this._model.IAMPolicyModels.Clear()));
            this.LoadModel();
        }

        public void Persist()
        {
            foreach (var policyModel in this.Model.DeletedPolicies)
            {
                if (!this.Model.NewPolicies.Contains(policyModel))
                {
                    var request = new DeleteRolePolicyRequest()
                    {
                        RoleName = this.Model.NewName,
                        PolicyName = policyModel
                    };
                    this._iamClient.DeleteRolePolicy(request);
                }
            }

            foreach (var policyModel in this.Model.IAMPolicyModels)
            {
                if (policyModel.HasChanged)
                {
                    var request = new PutRolePolicyRequest()
                    {
                        RoleName = this.Model.OriginalName,
                        PolicyName = policyModel.Name,
                        PolicyDocument = policyModel.Policy.ToJson()
                    };
                    this._iamClient.PutRolePolicy(request);
                }
            }

            this.Model.CommitChanges();
        }

    }
}
