using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.RDS.Nodes;
using Amazon.AWSToolkit.RDS.View;
using Amazon.AWSToolkit.RDS.Model;
using Amazon.RDS;
using Amazon.RDS.Model;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.RDS.Util;

namespace Amazon.AWSToolkit.RDS.Controller
{
    public class DeleteDBInstanceController : BaseContextCommand
    {
        private readonly ToolkitContext _toolkitContext;
        IAmazonRDS _rdsClient;
        ActionResults _results;
        DeleteDBInstanceModel _model;
        DeleteDBInstanceControl _control;
        RDSInstanceRootViewModel _instanceRootViewModel;

        public DeleteDBInstanceController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public ToolkitContext ToolkitContext => _toolkitContext;

        public override ActionResults Execute(IViewModel model)
        {
            var result = DeleteInstance(model);
            RecordMetric(result);
            return result;
        }

        private ActionResults DeleteInstance(IViewModel model)
        {
            var rdsInstanceViewModel = model as RDSInstanceViewModel;
            if (rdsInstanceViewModel == null)
            {
                return ActionResults.CreateFailed(new ToolkitException("Unable to find RDS Instance data",
                            ToolkitException.CommonErrorCode.InternalMissingServiceState));
            }

            return Execute(rdsInstanceViewModel.RDSClient, rdsInstanceViewModel.Parent as RDSInstanceRootViewModel, rdsInstanceViewModel.DBInstance.DBInstanceIdentifier);
        }

        public ActionResults Execute(IAmazonRDS rdsClient, RDSInstanceRootViewModel instanceRootViewModel, string dbIdentifier)
        {
            _rdsClient = rdsClient;
            _instanceRootViewModel = instanceRootViewModel;
            _model = new DeleteDBInstanceModel(dbIdentifier);
            _control = new DeleteDBInstanceControl(this);

            if (!_toolkitContext.ToolkitHost.ShowModal(_control))
            {
                  return ActionResults.CreateCancelled();
            }

            return _results ?? ActionResults.CreateFailed();
        }

        public DeleteDBInstanceModel Model => this._model;

        public void DeleteDBInstance()
        {
            var request = new DeleteDBInstanceRequest()
            {
                DBInstanceIdentifier = this._model.DBIdentifier,
                SkipFinalSnapshot = !this._model.CreateFinalSnapshot
            };

            if (this._model.CreateFinalSnapshot)
            {
                request.FinalDBSnapshotIdentifier = this._model.FinalSnapshotName;
            }
              
            this._rdsClient.DeleteDBInstance(request);

            if(this._instanceRootViewModel != null)
            {
                this._instanceRootViewModel.RemoveDBInstance(this._model.DBIdentifier);
            }
            this._results = new ActionResults().WithSuccess(true);
        }
        
        public void RecordMetric(ActionResults results)
        {
            var awsConnectionSettings = _instanceRootViewModel?.RDSRootViewModel?.AwsConnectionSettings;
            _toolkitContext.RecordRdsDeleteInstance(results, awsConnectionSettings);
        }
    }
}
