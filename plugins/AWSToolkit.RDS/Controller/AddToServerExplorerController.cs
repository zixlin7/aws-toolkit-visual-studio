using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

using Amazon.AWSToolkit.RDS.Model;
using Amazon.AWSToolkit.RDS.Nodes;

using Amazon.RDS;
using Amazon.RDS.Model;

namespace Amazon.AWSToolkit.RDS.Controller
{
    public class AddToServerExplorerController : BaseContextCommand
    {
        public override ActionResults Execute(IViewModel model)
        {
            
            var rdsInstanceViewModel = model as RDSInstanceViewModel;
            if (rdsInstanceViewModel == null)
                return new ActionResults().WithSuccess(false);

            try
            {
                IAmazonRDS rdsClient = rdsInstanceViewModel.RDSClient;
                DBInstanceWrapper instance = rdsInstanceViewModel.DBInstance;
                if (instance.DBInstanceStatus != DBInstanceWrapper.DbStatusAvailable)
                {
                    instance = refreshDBInstance(rdsClient, instance.DBInstanceIdentifier);
                    if(instance.DBInstanceStatus != DBInstanceWrapper.DbStatusAvailable)
                    {
                        ToolkitFactory.Instance.ShellProvider.ShowError("Not Available", string.Format("DB instance {0} is not currently available.", instance.DBInstanceIdentifier));
                        return new ActionResults().WithSuccess(false);
                    }
                }

                if (instance.DatabaseType == DatabaseTypes.SQLServer && !RDSUtil.CanAccessSQLServer(instance.NativeInstance.Endpoint))
                {
                    var promptController = new PromptAddCurrentCIDRController();
                    if (!promptController.Execute(rdsInstanceViewModel).Success)
                        return new ActionResults().WithSuccess(false);
                }

                var service = ToolkitFactory.Instance.ShellProvider.QueryShellProverService<IRegisterDataConnectionService>();
                if (service == null)
                    return new ActionResults().WithSuccess(true);

                service.RegisterDataConnection(instance.DatabaseType, "rds." + instance.DBInstanceIdentifier, instance.Endpoint, instance.Port.GetValueOrDefault(), instance.MasterUsername, instance.DBName);

                return new ActionResults().WithSuccess(true);
            }
            catch (RegisterDataConnectionException e)
            {
                string msg = string.Format("{0}\r\n\r\n<a href=\"{1}\">{1}</a>", e.Message, e.URL);
                ToolkitFactory.Instance.ShellProvider.ShowErrorWithLinks("Missing Component", msg);
                return new ActionResults().WithSuccess(false);
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Data Error", "Error loading instances: " + e.Message);
                return new ActionResults().WithSuccess(false);
            } 
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
