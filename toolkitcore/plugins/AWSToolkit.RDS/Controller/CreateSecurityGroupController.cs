using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

using Amazon.AWSToolkit.RDS.Model;
using Amazon.AWSToolkit.RDS.View;
using Amazon.AWSToolkit.RDS.Nodes;

using Amazon.RDS;
using Amazon.RDS.Model;

using log4net;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.RDS.Util;

namespace Amazon.AWSToolkit.RDS.Controller
{
    public class CreateSecurityGroupController : BaseContextCommand
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(CreateSecurityGroupController));

        private readonly ToolkitContext _toolkitContext;
        ActionResults _results;
        IAmazonRDS _rdsClient;
        CreateSecurityGroupModel _model;
        RDSSecurityGroupRootViewModel _securityGroupRootViewModel;

        public CreateSecurityGroupController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public CreateSecurityGroupModel Model => _model;

        public ToolkitContext ToolkitContext => _toolkitContext;

        public override ActionResults Execute(IViewModel model)
        {
            var result = CreateSecurityGroup(model);
            RecordMetric(result);
            return result;
        }

        private ActionResults CreateSecurityGroup(IViewModel model)
        {
            var securityGroupRootViewModel = model as RDSSecurityGroupRootViewModel;
            if (securityGroupRootViewModel == null)
            {
                return ActionResults.CreateFailed(new ToolkitException("Unable to find RDS Security group data",
                    ToolkitException.CommonErrorCode.InternalMissingServiceState));
            }

            return Execute(securityGroupRootViewModel);
        }

        public ActionResults Execute(RDSSecurityGroupRootViewModel securityGroupRootViewModel)
        {
            _securityGroupRootViewModel = securityGroupRootViewModel;
            _model = new CreateSecurityGroupModel();
            _rdsClient = _securityGroupRootViewModel.RDSClient;

            var control = new CreateSecurityGroupControl(this);

            if (!_toolkitContext.ToolkitHost.ShowModal(control))
            {
                return ActionResults.CreateCancelled();
            }

            return _results ?? ActionResults.CreateFailed();
        }

        public string CreateSecurityGroup()
        {
            var request = new CreateDBSecurityGroupRequest()
            {
                DBSecurityGroupName = this._model.Name,
                DBSecurityGroupDescription = this._model.Description
            };

            var response = this._rdsClient.CreateDBSecurityGroup(request);

            this._results = new ActionResults().WithFocalname(this._model.Name).WithSuccess(true);

            if (this._securityGroupRootViewModel != null)
            {
                var wrapper = new DBSecurityGroupWrapper(response.DBSecurityGroup);
                this._securityGroupRootViewModel.AddSecurityGroup(wrapper);
            }

            return this._model.Name;
        }

        public void RecordMetric(ActionResults results)
        {
            var awsConnectionSettings = _securityGroupRootViewModel?.RDSRootViewModel?.AwsConnectionSettings;
            _toolkitContext.RecordRdsCreateSecurityGroup(results, awsConnectionSettings);
        }
    }
}
