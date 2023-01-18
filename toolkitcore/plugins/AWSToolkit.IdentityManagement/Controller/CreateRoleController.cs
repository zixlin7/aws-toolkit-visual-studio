using System.Linq;

using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.IdentityManagement.Model;
using Amazon.AWSToolkit.IdentityManagement.Nodes;
using Amazon.AWSToolkit.IdentityManagement.Util;
using Amazon.AWSToolkit.IdentityManagement.View;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.EC2;
using Amazon.IdentityManagement.Model;

namespace Amazon.AWSToolkit.IdentityManagement.Controller
{
    public class CreateRoleController : BaseContextCommand
    {
        private static readonly string Ec2ServiceName = new AmazonEC2Config().RegionEndpointServiceName;
        private readonly ToolkitContext _toolkitContext;
        CreateRoleControl _control;
        CreateRoleModel _model;
        IAMRoleRootViewModel _rootModel;
        ActionResults _results;

        public CreateRoleController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public override ActionResults Execute(IViewModel model)
        {
            var result = CreateRole(model);
            RecordMetric(result);
            return result;
        }

        public ActionResults CreateRole(IViewModel model)
        {
            _rootModel = model as IAMRoleRootViewModel;
            if (_rootModel == null)
            {
                return ActionResults.CreateFailed();
            }

            _model = new CreateRoleModel();
            _control = new CreateRoleControl(this);

            if (!_toolkitContext.ToolkitHost.ShowModal(_control))
            {
                return ActionResults.CreateCancelled();
            }

            return _results ?? ActionResults.CreateFailed();
        }

        public CreateRoleModel Model => this._model;

        public void Persist()
        {
            var assumeRolePolicyDocument = Constants.GetIAMRoleAssumeRolePolicyDocument(
                Ec2ServiceName,
                this._rootModel.IAMRootViewModel.Region.Id);
            var roleRequest = new CreateRoleRequest()
            {
                RoleName = this.Model.RoleName.Trim(),
                AssumeRolePolicyDocument = assumeRolePolicyDocument,
            };
            var createResponse = this._rootModel.IAMClient.CreateRole(roleRequest);

            InstanceProfile instanceProfile = null;
            try
            {
                instanceProfile = this._rootModel.IAMClient.GetInstanceProfile(new GetInstanceProfileRequest() { InstanceProfileName = roleRequest.RoleName }).InstanceProfile;
            }
            catch (NoSuchEntityException) { }

            // Check to see if an instance profile exists for this role and if not create it.
            if (instanceProfile == null)
            {
                var profileRequest = new CreateInstanceProfileRequest() { InstanceProfileName = roleRequest.RoleName };
                this._rootModel.IAMClient.CreateInstanceProfile(profileRequest);

                var addRoleRequest = new AddRoleToInstanceProfileRequest() { RoleName = roleRequest.RoleName, InstanceProfileName = roleRequest.RoleName };
                this._rootModel.IAMClient.AddRoleToInstanceProfile(addRoleRequest);
            }
            // If it already exists see if this role is already assigned and if not assign it.
            else
            {
                if (instanceProfile.Roles.FirstOrDefault(x => x.RoleName == roleRequest.RoleName) == null)
                {
                    var addRoleRequest = new AddRoleToInstanceProfileRequest()
                    {
                        RoleName = roleRequest.RoleName,
                        InstanceProfileName = roleRequest.RoleName
                    };
                    this._rootModel.IAMClient.AddRoleToInstanceProfile(addRoleRequest);
                }
            }

            this._results = new ActionResults()
                .WithSuccess(true)
                .WithParameter(IAMActionResultsConstants.PARAM_IAM_ROLE, createResponse.Role);
        }

        public void RecordMetric(ActionResults results)
        {
            var awsConnectionSettings = _rootModel?.IAMRootViewModel?.AwsConnectionSettings;
            _toolkitContext.RecordIamCreate(IamResourceType.Role, results,
                 awsConnectionSettings);
        }
    }
}
