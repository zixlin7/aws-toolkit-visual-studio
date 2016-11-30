using System.Collections.Generic;
using System.Windows;
using Amazon.AWSToolkit.CloudFormation.View.Components;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.CloudFormation.Nodes;

using Microsoft.Win32;
using AWSDeployment;
using log4net;

namespace Amazon.AWSToolkit.CloudFormation.Controllers
{
    public class GetConfigurationController : BaseContextCommand
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(GetConfigurationController));

        public override ActionResults Execute(IViewModel model)
        {
            var stackModel = model as CloudFormationStackViewModel;
            if (stackModel == null)
                return new ActionResults().WithSuccess(false);

            string configFile = null;
            var dlg = new SaveFileDialog
                          {
                              Title = "Save Stack Configuration to File",
                              Filter = "Text Files|*.txt|All Files|*.*",
                              OverwritePrompt = true
                          };
            if (dlg.ShowDialog().GetValueOrDefault())
            {
                configFile= dlg.FileName;
            }

            if (string.IsNullOrEmpty(configFile))
                return new ActionResults().WithSuccess(false);

            var settings = new Dictionary<string, object>();
            settings[DeploymentEngineBase.ACCOUNT_PROFILE_NAME] = stackModel.AccountViewModel.AccountDisplayName;
            settings[DeploymentEngineBase.REGION] = stackModel.CloudFormationRootViewModel.CurrentEndPoint.RegionSystemName;
            settings[CloudFormationDeploymentEngine.STACK_NAME] = stackModel.StackName;

            var config = DeploymentEngineBase.CaptureEnvironmentConfig(settings);
            DeploymentConfigurationWriter.WriteDeploymentToFile(config, configFile);

            string message = string.Format("The configuration for stack {0} was saved successfully to {1}",
                                            stackModel.StackName,
                                            configFile);
            LOGGER.DebugFormat(message);
            var control = new StackConfigSavedControl {SuccessFailMsg = message};
            ToolkitFactory.Instance.ShellProvider.ShowModal(control, MessageBoxButton.OK);

            if (control.OpenFileForEdit)
                ToolkitFactory.Instance.ShellProvider.OpenInEditor(configFile);

            return new ActionResults().WithSuccess(true);
        }
    }
}
