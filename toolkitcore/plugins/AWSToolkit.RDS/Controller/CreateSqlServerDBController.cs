using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

using Amazon.AWSToolkit.RDS.Nodes;
using Amazon.AWSToolkit.RDS.View;
using Amazon.AWSToolkit.RDS.Model;

using Amazon.RDS;
using Amazon.RDS.Model;

using log4net;
using Amazon.AWSToolkit.Account;

namespace Amazon.AWSToolkit.RDS.Controller
{
    public class CreateSqlServerDBController : BaseContextCommand
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(CreateSqlServerDBController));

        ActionResults _results;
        CreateSqlServerDBModel _model;
        IAmazonRDS _rdsClient;
        DBInstanceWrapper _dbInstance;
        CreateSqlServerDBControl _control;

        public CreateSqlServerDBController()
        {
        }

        public CreateSqlServerDBModel Model
        {
            get { return _model; }
        }

        public override ActionResults Execute(IViewModel model)
        {
            RDSInstanceViewModel instanceModel = model as RDSInstanceViewModel;
            if (instanceModel == null)
                return new ActionResults().WithSuccess(false);

            return Execute(instanceModel);
        }

        public ActionResults Execute(RDSInstanceViewModel rdsViewModel)
        {
            _rdsClient = rdsViewModel.RDSClient;

            if (_rdsClient == null)
                return new ActionResults().WithSuccess(false);

            this._dbInstance = rdsViewModel.DBInstance;

            if (this._dbInstance.DBInstanceStatus != DBInstanceWrapper.DbStatusAvailable)
            {
                this._dbInstance = refreshDBInstance(this._rdsClient, this._dbInstance.DBInstanceIdentifier);
                if (this._dbInstance.DBInstanceStatus != DBInstanceWrapper.DbStatusAvailable)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Not Available", string.Format("DB instance {0} is not currently available.", this._dbInstance.DBInstanceIdentifier));
                    return new ActionResults().WithSuccess(false);
                }
            }

            _model = new CreateSqlServerDBModel();
            _model.DBInstance = this._dbInstance.Endpoint;
            _model.UserName = this._dbInstance.MasterUsername;



            if (this._dbInstance.DBInstanceStatus != DBInstanceWrapper.DbStatusAvailable)
            {
                this._dbInstance = AddToServerExplorerController.refreshDBInstance(_rdsClient, this._dbInstance.DBInstanceIdentifier);
                if (this._dbInstance.DBInstanceStatus != DBInstanceWrapper.DbStatusAvailable)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Not Available", string.Format("DB instance {0} is not currently available.", this._dbInstance.DBInstanceIdentifier));
                    return new ActionResults().WithSuccess(false);
                }
            }

            if (!RDSUtil.CanAccessSQLServer(this._dbInstance.NativeInstance.Endpoint))
            {
                var promptController = new PromptAddCurrentCIDRController();
                if (!promptController.Execute(rdsViewModel).Success)
                    return new ActionResults().WithSuccess(false);
            }

            _control = new CreateSqlServerDBControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(_control);

            if (this._results == null)
                return new ActionResults().WithSuccess(false);

            return this._results;
        }

        public void CreateSqlServerDatabase()
        {
            var connStr = this._dbInstance.CreateConnectionString("master", this._model.Password);
            using(var connection = new SqlConnection(connStr))
            {
                connection.Open();
                var command = new SqlCommand(string.Format("CREATE DATABASE [{0}]", this._model.DBName), connection);
                command.ExecuteNonQuery();

                var service = ToolkitFactory.Instance.ShellProvider.QueryShellProviderService<IRegisterDataConnectionService>();
                if (service != null)
                {
                    var title = string.Format("rds.{0}.{1}", this._dbInstance.DBInstanceIdentifier, this._model.DBName);
                    service.AddDataConnection(this._dbInstance.DatabaseType, title, this._dbInstance.CreateConnectionString(this._model.DBName, this._model.Password));
                }

            }

            this._results = new ActionResults().WithSuccess(false);
        }

        internal static DBInstanceWrapper refreshDBInstance(IAmazonRDS rdsClient, string dbIdentifier)
        {
            var response = rdsClient.DescribeDBInstances(new DescribeDBInstancesRequest() { DBInstanceIdentifier = dbIdentifier });
            if (response.DBInstances.Count != 1)
                return null;

            return new DBInstanceWrapper(response.DBInstances[0]);
        }
    }
}
