using System;
using System.Windows;

using Amazon.AWSToolkit.ElasticBeanstalk.Nodes;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.ElasticBeanstalk.View.Components;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageWorkers;
using Amazon.ElasticBeanstalk.Model;

using log4net;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Controller
{
    public class ChangeEnvironmentTypeController : BaseContextCommand
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ChangeEnvironmentTypeController));

        public string RequestedEnvironmentType { get; set; }
        public string VPCId { get; set; }

        public override ActionResults Execute(IViewModel model)
        {
            var environmentModel = model as EnvironmentViewModel;
            if (environmentModel == null)
                return new ActionResults().WithSuccess(false);

            if (string.IsNullOrEmpty(RequestedEnvironmentType))
                RequestedEnvironmentType = BeanstalkConstants.EnvType_LoadBalanced;

            QueryVPCPropertiesWorker.VPCPropertyData vpcPropertyData = null;

            bool proceed = false;
            string msg;
            EnvTypeChangeControl control = null;
            if (string.IsNullOrEmpty(VPCId))
            {
                msg = string.Format("Are you sure you want to change the type of the environment named \"{0}\" to \"{1}\"?\r\n\r\n" +
                                    "This operation may take several minutes, during which your application " +
                                    "will not be available."
                                    , environmentModel.Name
                                    , RequestedEnvironmentType
                                    );
                // loss of vpc settings here makes for a silly large dialog with spare test, so use standard confirmation
                // message box
                proceed = ToolkitFactory.Instance.ShellProvider.Confirm("Change Environment Type", msg, MessageBoxButton.OKCancel);
            }
            else
            {
                vpcPropertyData = QueryVPCPropertiesWorker.QueryVPCProperties(environmentModel.AccountViewModel,
                                                                              environmentModel.ApplicationViewModel.Region,
                                                                              VPCId,
                                                                              LOGGER);
                msg = string.Format("Are you sure you want to change the type of the environment named \"{0}\" to \"{1}\"?\r\n\r\n" +
                                    "This operation may take several minutes, during which your application " +
                                    "will not be available.\r\n\r\n" +
                                    "You will also need to adjust your VPC settings to be appropriate for the new environment type:"
                                    , environmentModel.Name
                                    , RequestedEnvironmentType
                                    );

                control = new EnvTypeChangeControl 
                { 
                    ConfirmationMessage = msg,
                    NewEnvironmentType = RequestedEnvironmentType
                };
                control.SetVPCData(VPCId, vpcPropertyData);

                proceed = ToolkitFactory.Instance.ShellProvider.ShowModal(control, MessageBoxButton.OKCancel);
            }

            if (proceed)
            {
                var isSingleInstanceEnvironment 
                    = !string.IsNullOrEmpty(RequestedEnvironmentType) 
                            && RequestedEnvironmentType.Equals(BeanstalkConstants.EnvType_SingleInstance, 
                                                               StringComparison.Ordinal); 
                var beanstalkClient = environmentModel.BeanstalkClient;
                try
                {
                    LOGGER.DebugFormat("Changing environment {0} to type '{1}'", environmentModel.Environment.EnvironmentId, RequestedEnvironmentType);
                    var request = new UpdateEnvironmentRequest
                    {
                        EnvironmentId = environmentModel.Environment.EnvironmentId, 
                    };

                    AddOptionSetting(request,
                                     BeanstalkConstants.ENVIRONMENT_NAMESPACE,
                                     BeanstalkConstants.ENVIRONMENTTYPE_OPTION,
                                     RequestedEnvironmentType);

                    if (!string.IsNullOrEmpty(VPCId))
                    {
                        AddOptionSetting(request, "aws:ec2:vpc", "Subnets", control.SelectedInstanceSubnetId);
                        AddOptionSetting(request, "aws:autoscaling:launchconfiguration", "SecurityGroups", control.SelectedVPCSecurityGroupId);

                        if (!isSingleInstanceEnvironment)
                        {
                            AddOptionSetting(request, "aws:ec2:vpc", "ELBSubnets", control.SelectedELBSubnetId);
                            AddOptionSetting(request, "aws:ec2:vpc", "ELBScheme", control.SelectedELBScheme);
                        }
                    }

                    beanstalkClient.UpdateEnvironment(request);
                }
                catch (Exception e)
                {
                    LOGGER.Error(string.Format("Error changing environment {0} to type '{1}'", environmentModel.Environment.EnvironmentId, RequestedEnvironmentType), e);
                    ToolkitFactory.Instance.ShellProvider.ShowMessage("Error Changing Type", "Error changing environment type: " + e.Message);
                    return new ActionResults().WithSuccess(false);
                }
            }

            return new ActionResults().WithSuccess(true);
        }

        void AddOptionSetting(UpdateEnvironmentRequest request, string ns, string option, string val)
        {
            request.OptionSettings.Add(new ConfigurationOptionSetting
                                            {
                                                Namespace = ns, 
                                                OptionName = option,
                                                Value = val
                                            });
        }
    }
}
