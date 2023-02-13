using System;
using System.Collections.Generic;
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
    public class EditUserController : BaseContextCommand
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(EditUserController));

        private readonly ToolkitContext _toolkitContext;
        IAmazonIdentityManagementService _iamClient;
        EditUserModel _model;
        IAMUserViewModel _iamUserViewModel;

        public EditUserController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public EditUserModel Model => this._model;

        public override ActionResults Execute(IViewModel model)
        {
            this._iamUserViewModel = model as IAMUserViewModel;
            if (this._iamUserViewModel == null)
                return new ActionResults().WithSuccess(false);

            this._iamClient = this._iamUserViewModel.IAMClient;
            this._model = new EditUserModel();

            this._model.OriginalName = this._iamUserViewModel.User.UserName;
            this._model.NewName = this._iamUserViewModel.User.UserName;

            EditUserControl control = new EditUserControl(this);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(control);

            return new ActionResults()
                    .WithSuccess(true);
        }

        public void LoadModel()
        {
            loadGroups();
            loadPolicies();
            loadAccessKeys();

            this.Model.CommitChanges();
        }

        #region Load Model
        void loadGroups()
        {
            var assignedGroups = new HashSet<string>();
            var listGroupsRequest = new ListGroupsForUserRequest() { UserName = this.Model.OriginalName };
            ListGroupsForUserResponse listGroupResponse = null;
            do
            {
                if (listGroupResponse != null)
                    listGroupsRequest.Marker = listGroupResponse.Marker;
                listGroupResponse = this._iamClient.ListGroupsForUser(listGroupsRequest);

                ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
                    {
                        foreach (var group in listGroupResponse.Groups)
                        {
                            assignedGroups.Add(group.GroupName);
                            this.Model.AssignedGroups.Add(group.GroupName);
                        }
                    }));
            } while (listGroupResponse.IsTruncated);


            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
                {
                    var rootGroup = this._iamUserViewModel.IAMUserRootViewModel.IAMRootViewModel.IAMGroupRootViewModel;
                    foreach (IViewModel model in rootGroup.Children)
                    {
                        IAMGroupViewModel groupModel = model as IAMGroupViewModel;
                        if (groupModel == null)
                            continue;


                        if (!assignedGroups.Contains(groupModel.Group.GroupName))
                            this.Model.AvailableGroups.Add(groupModel.Group.GroupName);
                    }
                }));
        }

        void loadPolicies()
        {
            var listPolicyRequest =
                new ListUserPoliciesRequest() { UserName = this.Model.OriginalName };
            ListUserPoliciesResponse listPolicyResponse = null;
            do
            {
                if (listPolicyResponse != null)
                    listPolicyRequest.Marker = listPolicyResponse.Marker;
                listPolicyResponse = this._iamClient.ListUserPolicies(listPolicyRequest);

                foreach (var policyName in listPolicyResponse.PolicyNames)
                {
                    var policyModel = new IAMPolicyModel {Name = policyName};

                    try
                    {
                        var response = this._iamClient.GetUserPolicy(new GetUserPolicyRequest()
                        {
                            UserName = this.Model.OriginalName,
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
        }

        void loadAccessKeys()
        {
            var secretKeys = AccessKeyModel.LoadSecretKeysLocalRepository();

            var listAccessKeysRequest =
                new ListAccessKeysRequest()
                {
                    UserName = this.Model.OriginalName
                };
            ListAccessKeysResponse listAccessKeysResponse = null;
            do
            {                
                if (listAccessKeysResponse != null)
                    listAccessKeysRequest.Marker = listAccessKeysResponse.Marker;
                listAccessKeysResponse = this._iamClient.ListAccessKeys(listAccessKeysRequest);

                ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
                    {
                        foreach (var metadata in listAccessKeysResponse.AccessKeyMetadata)
                        {
                            string secretKey = secretKeys[metadata.AccessKeyId];

                            var accessModel = new AccessKeyModel(
                                metadata.AccessKeyId, metadata.Status, metadata.CreateDate,
                                !string.IsNullOrEmpty(secretKey), secretKey);

                            this._model.AccessKeys.Add(accessModel);
                        }
                    }));
            }
            while (listAccessKeysResponse.IsTruncated);
        }
        #endregion

        public void Refresh()
        {
            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
                {
                    this._model.AccessKeys.Clear();
                    this._model.AssignedGroups.Clear();
                    this._model.AvailableGroups.Clear();
                    this._model.IAMPolicyModels.Clear();
                }));
            this.LoadModel();
        }

        public void Persist()
        {
            if (!this.Model.OriginalName.Equals(this.Model.NewName))
            {
                var request = new UpdateUserRequest()
                {
                    UserName = this.Model.OriginalName,
                    NewUserName = this.Model.NewName
                };
                this._iamClient.UpdateUser(request);

                this.Model.OriginalName = this.Model.NewName;

                if (this._iamUserViewModel != null)
                {
                    this._iamUserViewModel.UpdateUser(this.Model.OriginalName);
                }
            }

            foreach (var group in this.Model.AssignedGroups)
            {
                if (!this.Model.OrignalAssignedGroups.Contains(group))
                {
                    var request = new AddUserToGroupRequest()
                    {
                        GroupName = group,
                        UserName = this.Model.NewName
                    };
                    this._iamClient.AddUserToGroup(request);
                }
            }

            foreach (var group in this.Model.OrignalAssignedGroups)
            {
                if (!this.Model.AssignedGroups.Contains(group))
                {
                    var request = new RemoveUserFromGroupRequest()
                    {
                        GroupName = group,
                        UserName = this.Model.NewName
                    };
                    this._iamClient.RemoveUserFromGroup(request);
                }
            }

            foreach (var policyModel in this.Model.DeletedPolicies)
            {
                if (!this.Model.NewPolicies.Contains(policyModel))
                {
                    var request = new DeleteUserPolicyRequest()
                    {
                        UserName = this.Model.NewName,
                        PolicyName = policyModel
                    };
                    this._iamClient.DeleteUserPolicy(request);
                }
            }

            foreach (var policyModel in this.Model.IAMPolicyModels)
            {
                var request = new PutUserPolicyRequest()
                {
                    UserName = this.Model.OriginalName,
                    PolicyName = policyModel.Name,
                    PolicyDocument = policyModel.Policy.ToJson()
                };
                this._iamClient.PutUserPolicy(request);
            }

            this.Model.CommitChanges();
        }


        public AccessKeyModel CreateNewAccessKeys()
        {
            var request = new CreateAccessKeyRequest() { UserName = this._model.OriginalName };
            var response = this._iamClient.CreateAccessKey(request);

            var accessKey = response.AccessKey;
            var accessModel = new AccessKeyModel(accessKey.AccessKeyId, accessKey.Status, accessKey.CreateDate)
                                  {
                                      SecretKey = accessKey.SecretAccessKey
                                  };
            this._model.AccessKeys.Add(accessModel);
            return accessModel;
        }

        public void DeleteAccessKey(AccessKeyModel accessKeyModel)
        {
            // Sanity check so we don't accidently delete the root access keys.
            if (string.IsNullOrEmpty(this._model.OriginalName))
                throw new ApplicationException("Missing username");

            var request = new DeleteAccessKeyRequest()
            {
                UserName = this._model.OriginalName,
                AccessKeyId = accessKeyModel.AccessKey
            };
            this._iamClient.DeleteAccessKey(request);

            if(accessKeyModel.PersistSecretKeyLocal)
                accessKeyModel.PersistSecretKeyLocal = false;
        }

        public void UpdateAccessKey(string accessKeyId, string status)
        {
            // Sanity check so we don't accidently delete the root access keys.
            if (string.IsNullOrEmpty(this._model.OriginalName))
                throw new ApplicationException("Missing username");

            var request = new UpdateAccessKeyRequest()
            {
                UserName = this._model.OriginalName,
                AccessKeyId = accessKeyId,
                Status = status
            };
            this._iamClient.UpdateAccessKey(request);
        }

        public void RecordEditUser(ActionResults results)
        {
            var awsConnectionSettings =
                _iamUserViewModel?.IAMUserRootViewModel?.IAMRootViewModel?.AwsConnectionSettings;
            _toolkitContext.RecordIamEdit(IamResourceType.User, results, awsConnectionSettings);
        }

        public void RecordCreateAccessKey(ActionResults results)
        {
            var awsConnectionSettings =
                _iamUserViewModel?.IAMUserRootViewModel?.IAMRootViewModel?.AwsConnectionSettings;
            _toolkitContext.RecordIamCreateAccessKey(results, awsConnectionSettings);
        }

        public void RecordDeleteAccessKey(ActionResults results)
        {
            var awsConnectionSettings =
                _iamUserViewModel?.IAMUserRootViewModel?.IAMRootViewModel?.AwsConnectionSettings;
            _toolkitContext.RecordIamDeleteAccessKey(results, awsConnectionSettings);
        }
    }
}
