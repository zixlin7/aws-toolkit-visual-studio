using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.RDS.Nodes;
using Amazon.AWSToolkit.RDS.View;
using Amazon.AWSToolkit.RDS.Model;
using Amazon.RDS;
using Amazon.RDS.Model;


namespace Amazon.AWSToolkit.RDS.Controller
{
    public class TakeSnapshotController : BaseContextCommand
    {
        IAmazonRDS _rdsClient;
        ActionResults _results;
        TakeSnapshotModel _model;
        TakeSnapshotControl _control;

        public override ActionResults Execute(IViewModel model)
        {
            var rdsInstanceViewModel = model as RDSInstanceViewModel;
            if (rdsInstanceViewModel == null)
                return new ActionResults().WithSuccess(false);

            return Execute(rdsInstanceViewModel.RDSClient, rdsInstanceViewModel.DBInstance.DBInstanceIdentifier);
        }

        public ActionResults Execute(IAmazonRDS rdsClient, string dbIdentifier)
        {
            this._rdsClient = rdsClient;
            this._model = new TakeSnapshotModel(dbIdentifier);
            this._control = new TakeSnapshotControl(this);

            if (ToolkitFactory.Instance.ShellProvider.ShowModal(this._control) && this._results != null)
                return this._results;


            return new ActionResults().WithSuccess(false);
        }

        public TakeSnapshotModel Model => this._model;

        public void TakeSnapshot()
        {
            var request = new CreateDBSnapshotRequest()
            {
                DBInstanceIdentifier = this._model.DBIdentifier,
                DBSnapshotIdentifier = this._model.SnapshotName
            };

            this._rdsClient.CreateDBSnapshot(request);
            this._results = new ActionResults().WithSuccess(true);
        }
    }
}
