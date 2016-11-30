using System.Collections.Generic;
using System.Windows;
using Amazon.AWSToolkit.ElasticBeanstalk.View.Components;
using Microsoft.Win32;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.ElasticBeanstalk.Nodes;

using log4net;
using AWSDeployment;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Controller
{
    public class GetConfigurationController : BaseContextCommand
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(GetConfigurationController));

        public override ActionResults Execute(IViewModel model)
        {
            var environmentModel = model as EnvironmentViewModel;
            if (environmentModel == null)
                return new ActionResults().WithSuccess(false);

            string configFile = null;
            var dlg = new SaveFileDialog
                          {
                              Title = "Save Environment Configuration to File",
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
            settings[DeploymentEngineBase.ACCOUNT_PROFILE_NAME] = environmentModel.AccountViewModel.AccountDisplayName;
            settings[DeploymentEngineBase.REGION] = environmentModel.ApplicationViewModel.ElasticBeanstalkRootViewModel.CurrentEndPoint.RegionSystemName;
            settings[BeanstalkDeploymentEngine.APPLICATION_NAME] = environmentModel.ApplicationViewModel.Application.ApplicationName;
            settings[BeanstalkDeploymentEngine.ENVIRONMENT_NAME] = environmentModel.Environment.EnvironmentName;

            var config = DeploymentEngineBase.CaptureEnvironmentConfig(settings);
            DeploymentConfigurationWriter.WriteDeploymentToFile(config, configFile);
            LOGGER.DebugFormat("Configuration file for environment {0} created at {1}", environmentModel.Environment.EnvironmentName, configFile);

            string message = string.Format("The configuration for environment {0} was saved successfully to {1}", 
                                            environmentModel.Environment.EnvironmentName, 
                                            configFile);

            LOGGER.DebugFormat(message);
            var control = new EnvConfigSavedControl {SuccessFailMsg = message};
            ToolkitFactory.Instance.ShellProvider.ShowModal(control, MessageBoxButton.OK);

            if (control.OpenFileForEdit)
                ToolkitFactory.Instance.ShellProvider.OpenInEditor(configFile);

            return new ActionResults().WithSuccess(true);
        }
    }
}
