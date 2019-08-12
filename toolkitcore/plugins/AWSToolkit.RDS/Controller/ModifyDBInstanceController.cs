using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

using Amazon.AWSToolkit.RDS.Nodes;
using Amazon.AWSToolkit.RDS.Model;
using Amazon.AWSToolkit.RDS.WizardPages;
using Amazon.AWSToolkit.RDS.WizardPages.PageControllers;

using Amazon.RDS;
using Amazon.RDS.Model;

using log4net;
using Amazon.AWSToolkit.Account;
using Amazon.EC2;
using Amazon.AWSToolkit.RDS.WizardPages.PageUI;

namespace Amazon.AWSToolkit.RDS.Controller
{
    public class ModifyDBInstanceController : BaseContextCommand
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(ModifyDBInstanceController));

        public override ActionResults Execute(IViewModel model)
        {
            RDSInstanceViewModel instanceModel = model as RDSInstanceViewModel;
            if (instanceModel == null)
                return new ActionResults().WithSuccess(false);

            return Execute(model, instanceModel.DBInstance);
        }

        public ActionResults Execute(IViewModel model, DBInstanceWrapper dbInstanceWrapper)
        {
            // can be launched from explorer or instances view -- two different roots :-(
            IAmazonRDS rdsClient = null;
            IAmazonEC2 ec2Client = null;
            AccountViewModel account = null;
            RDSInstanceRootViewModel instanceRootViewModel = null;

            if (model is RDSInstanceViewModel)
            {
                RDSInstanceViewModel viewModel = (model as RDSInstanceViewModel);
                rdsClient = viewModel.RDSClient;
                account = viewModel.AccountViewModel;
                instanceRootViewModel = viewModel.Parent as RDSInstanceRootViewModel;    
            }
            else if (model is RDSInstanceRootViewModel)
            {
                RDSInstanceRootViewModel viewModel = model as RDSInstanceRootViewModel;
                rdsClient = viewModel.RDSClient;
                account = viewModel.AccountViewModel;
                instanceRootViewModel = viewModel;
            }

            if (rdsClient == null || account == null)
                return new ActionResults().WithSuccess(false);

            var endPoints = RegionEndPointsManager.GetInstance().GetRegion(instanceRootViewModel.CurrentEndPoint.RegionSystemName);
            var endPoint = endPoints.GetEndpoint(RegionEndPointsManager.EC2_SERVICE_NAME);
            var config = new AmazonEC2Config();
            endPoint.ApplyToClientConfig(config);
            ec2Client = new AmazonEC2Client(account.Credentials, config);

            if (dbInstanceWrapper.DBInstanceStatus != DBInstanceWrapper.DbStatusAvailable)
            {
                dbInstanceWrapper = AddToServerExplorerController.refreshDBInstance(rdsClient, dbInstanceWrapper.DBInstanceIdentifier);
                if (dbInstanceWrapper.DBInstanceStatus != DBInstanceWrapper.DbStatusAvailable)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Not Available", string.Format("DB instance {0} is not currently available.", dbInstanceWrapper.DBInstanceIdentifier));
                    return new ActionResults().WithSuccess(false);
                }
            }

            Dictionary<string, object> seedProperties = new Dictionary<string, object>();
            seedProperties[CommonWizardProperties.propkey_NavigatorRootViewModel] = model;
            seedProperties[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] = account;
            seedProperties[RDSWizardProperties.SeedData.propkey_RDSClient] = rdsClient;
            seedProperties[RDSWizardProperties.SeedData.propkey_EC2Client] = ec2Client;
            seedProperties[RDSWizardProperties.SeedData.propkey_DBInstanceWrapper] = dbInstanceWrapper;

            IAWSWizard wizard = AWSWizardFactory.CreateStandardWizard("Amazon.AWSToolkit.RDS.View.ModifyDBInstance", seedProperties);
            wizard.Title = string.Format("Modify DB Instance - {0}", dbInstanceWrapper.DisplayName);

            IAWSWizardPageController[] defaultPages = new IAWSWizardPageController[]
            {
                new ModifyDBInstancePageController(),
                new DBInstanceBackupsPageController(),
                new ModifyDBInstanceReviewPageController()
            };

            wizard.RegisterPageControllers(defaultPages, 0);
            if (wizard.Run() == true)
                return ModifyDBInstance(wizard.CollectedProperties);

            return new ActionResults().WithSuccess(false);
        }

        ActionResults ModifyDBInstance(Dictionary<string, object> properties)
        {
            bool success = true;
            try
            {
                IAmazonRDS rdsClient = properties[RDSWizardProperties.SeedData.propkey_RDSClient] as IAmazonRDS; // getValue doesn't work here
                DBInstanceWrapper instance = getValue<DBInstanceWrapper>(properties, RDSWizardProperties.SeedData.propkey_DBInstanceWrapper);

                ModifyDBInstanceRequest request = new ModifyDBInstanceRequest() { DBInstanceIdentifier = instance.DBInstanceIdentifier };

                // properties left unchanged will not be present in the set
                if (properties.ContainsKey(RDSWizardProperties.EngineProperties.propkey_EngineVersion))
                    request.EngineVersion = getValue<string>(properties, RDSWizardProperties.EngineProperties.propkey_EngineVersion);

                if (properties.ContainsKey(RDSWizardProperties.InstanceProperties.propkey_InstanceClass))
                    request.DBInstanceClass = getValue<string>(properties, RDSWizardProperties.InstanceProperties.propkey_InstanceClass);

                if (properties.ContainsKey(RDSWizardProperties.InstanceProperties.propkey_MultiAZ))
                    request.MultiAZ = getValue<bool>(properties, RDSWizardProperties.InstanceProperties.propkey_MultiAZ);

                if (properties.ContainsKey(RDSWizardProperties.InstanceProperties.propkey_AutoMinorVersionUpgrade))
                    request.AutoMinorVersionUpgrade = getValue<bool>(properties, RDSWizardProperties.InstanceProperties.propkey_AutoMinorVersionUpgrade);

                if (properties.ContainsKey(RDSWizardProperties.InstanceProperties.propkey_Storage))
                    request.AllocatedStorage = getValue<int>(properties, RDSWizardProperties.InstanceProperties.propkey_Storage);

                if (properties.ContainsKey(RDSWizardProperties.InstanceProperties.propkey_DBParameterGroup))
                    request.DBParameterGroupName = getValue<string>(properties, RDSWizardProperties.InstanceProperties.propkey_DBParameterGroup);

                if (properties.ContainsKey(RDSWizardProperties.InstanceProperties.propkey_SecurityGroups))
                {
                    List<SecurityGroupInfo> groups = getValue<List<SecurityGroupInfo>>(properties, RDSWizardProperties.InstanceProperties.propkey_SecurityGroups);
                    if (groups != null && groups.Any())
                    {
                        if (instance.NativeInstance.DBSubnetGroup != null)
                        {
                            var groupIds = new List<string>();
                            groupIds.AddRange(groups.Select(@group => @group.Id));
                            request.VpcSecurityGroupIds = groupIds;
                        }
                        else
                        {
                            var groupNames = new List<string>();
                            groupNames.AddRange(groups.Select(@group => @group.Name));
                            request.DBSecurityGroups = groupNames;
                        }
                    }
                }

                if (properties.ContainsKey(RDSWizardProperties.InstanceProperties.propkey_MasterUserPassword))
                    request.MasterUserPassword = getValue<string>(properties, RDSWizardProperties.InstanceProperties.propkey_MasterUserPassword);

                if (properties.ContainsKey(RDSWizardProperties.MaintenanceProperties.propkey_RetentionPeriod))
                    request.BackupRetentionPeriod = getValue<int>(properties, RDSWizardProperties.MaintenanceProperties.propkey_RetentionPeriod);

                if (properties.ContainsKey(RDSWizardProperties.MaintenanceProperties.propkey_BackupWindow))
                    request.PreferredBackupWindow = getValue<string>(properties, RDSWizardProperties.MaintenanceProperties.propkey_BackupWindow);

                if (properties.ContainsKey(RDSWizardProperties.MaintenanceProperties.propkey_MaintenanceWindow))
                    request.PreferredMaintenanceWindow = getValue<string>(properties, RDSWizardProperties.MaintenanceProperties.propkey_MaintenanceWindow);

                if (properties.ContainsKey(RDSWizardProperties.ReviewProperties.propkey_ApplyImmediately))
                    request.ApplyImmediately = getValue<bool>(properties, RDSWizardProperties.ReviewProperties.propkey_ApplyImmediately);

                var response = rdsClient.ModifyDBInstance(request);
                if (response.DBInstance == null)
                    success = false;
            }
            catch (AmazonRDSException e)
            {
                LOGGER.ErrorFormat("Caught exception modifying DB instance - {0}", e.Message);
                ToolkitFactory.Instance.ShellProvider.ShowError("Data Error", "Error modifying DB instance: " + e.Message);
                success = false;
            }

            return new ActionResults().WithSuccess(success);
        }

        T getValue<T>(Dictionary<string, object> properties, string key)
        {
            object value;
            if (properties.TryGetValue(key, out value))
            {
                T convertedValue = (T)Convert.ChangeType(value, typeof(T));
                return convertedValue;
            }

            return default(T);
        }
    }
}
