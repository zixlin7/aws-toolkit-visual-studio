using System;
using System.Windows;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.ElasticBeanstalk.Models;
using Amazon.AWSToolkit.ElasticBeanstalk.View.Components;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageWorkers;
using Amazon.AWSToolkit.Navigator;
using Amazon.EC2;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;

using log4net;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Controller
{
    public class ChangeEnvironmentTypeController : BaseConnectionContextCommand
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ChangeEnvironmentTypeController));

        private readonly BeanstalkEnvironmentModel _beanstalkEnvironment;
        private readonly AmazonElasticBeanstalkClient _beanstalk;
        private readonly AmazonEC2Client _ec2;

        public string RequestedEnvironmentType { get; set; }
        public string VPCId { get; set; }

        public ChangeEnvironmentTypeController(BeanstalkEnvironmentModel beanstalkEnvironment,
            ToolkitContext toolkitContext, AwsConnectionSettings connectionSettings)
            : base(toolkitContext, connectionSettings)
        {
            _beanstalkEnvironment = beanstalkEnvironment;
            _beanstalk = _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonElasticBeanstalkClient>(ConnectionSettings.CredentialIdentifier, ConnectionSettings.Region);
            _ec2 = _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonEC2Client>(ConnectionSettings.CredentialIdentifier, ConnectionSettings.Region);
        }

        public override ActionResults Execute()
        {
            if (_beanstalkEnvironment == null)
            {
                return new ActionResults().WithSuccess(false);
            }

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
                                    , _beanstalkEnvironment.Name
                                    , RequestedEnvironmentType
                                    );
                // loss of vpc settings here makes for a silly large dialog with spare test, so use standard confirmation
                // message box
                proceed = ToolkitFactory.Instance.ShellProvider.Confirm("Change Environment Type", msg, MessageBoxButton.OKCancel);
            }
            else
            {
                vpcPropertyData = QueryVPCPropertiesWorker.QueryVPCProperties(_ec2,
                                                                              VPCId,
                                                                              _logger);
                msg = string.Format("Are you sure you want to change the type of the environment named \"{0}\" to \"{1}\"?\r\n\r\n" +
                                    "This operation may take several minutes, during which your application " +
                                    "will not be available.\r\n\r\n" +
                                    "You will also need to adjust your VPC settings to be appropriate for the new environment type:"
                                    , _beanstalkEnvironment.Name
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

                try
                {
                    _logger.DebugFormat("Changing environment {0} to type '{1}'", _beanstalkEnvironment.Id, RequestedEnvironmentType);
                    var request = new UpdateEnvironmentRequest
                    {
                        EnvironmentId = _beanstalkEnvironment.Id, 
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

                    _beanstalk.UpdateEnvironment(request);
                }
                catch (Exception e)
                {
                    _logger.Error(string.Format("Error changing environment {0} to type '{1}'", _beanstalkEnvironment.Id, RequestedEnvironmentType), e);
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
