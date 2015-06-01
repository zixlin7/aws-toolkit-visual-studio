using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.RDS.Nodes;
using Amazon.AWSToolkit.RDS.View;
using Amazon.AWSToolkit.RDS.Model;
using Amazon.AWSToolkit;

using Amazon.RDS;
using Amazon.RDS.Model;

namespace Amazon.AWSToolkit.RDS.Controller
{
    public class DeleteDBInstanceController : BaseContextCommand
    {
        IAmazonRDS _rdsClient;
        ActionResults _results;
        DeleteDBInstanceModel _model;
        DeleteDBInstanceControl _control;
        RDSInstanceRootViewModel _instanceRootViewModel;

        public override ActionResults Execute(IViewModel model)
        {
            var rdsInstanceViewModel = model as RDSInstanceViewModel;
            if (rdsInstanceViewModel == null)
                return new ActionResults().WithSuccess(false);

            return Execute(rdsInstanceViewModel.RDSClient, rdsInstanceViewModel.Parent as RDSInstanceRootViewModel, rdsInstanceViewModel.DBInstance.DBInstanceIdentifier);
        }

        public ActionResults Execute(IAmazonRDS rdsClient, RDSInstanceRootViewModel instanceRootViewModel, string dbIdentifier)
        {
            this._rdsClient = rdsClient;
            this._instanceRootViewModel = instanceRootViewModel;
            this._model = new DeleteDBInstanceModel(dbIdentifier);
            this._control = new DeleteDBInstanceControl(this);

            if (ToolkitFactory.Instance.ShellProvider.ShowModal(this._control) && this._results != null)
                return this._results;


            return new ActionResults().WithSuccess(false);
        }

        public DeleteDBInstanceModel Model
        {
            get { return this._model; }
        }

        public void DeleteDBInstance()
        {
            var request = new DeleteDBInstanceRequest()
            {
                DBInstanceIdentifier = this._model.DBIdentifier,
                SkipFinalSnapshot = !this._model.CreateFinalSnapshot
            };

            if (this._model.CreateFinalSnapshot)
                request.FinalDBSnapshotIdentifier = this._model.FinalSnapshotName;

            this._rdsClient.DeleteDBInstance(request);

            if(this._instanceRootViewModel != null)
            {
                this._instanceRootViewModel.RemoveDBInstance(this._model.DBIdentifier);
            }
            this._results = new ActionResults().WithSuccess(true);
        }
    }
}
