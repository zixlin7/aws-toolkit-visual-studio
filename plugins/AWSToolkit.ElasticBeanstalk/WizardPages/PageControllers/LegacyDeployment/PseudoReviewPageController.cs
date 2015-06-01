using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.PageControllers;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.PluginServices.Deployment;
using System.Windows.Media;
using Amazon.IdentityManagement.Model;
using AWSDeployment;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers.LegacyDeployment
{
    /// <summary>
    /// 'Virtual' review page that contributes service-specific review text panels into a 
    /// list for occasion when the deployment service is Beanstalk. This page will never
    /// actually appear in the wizard flow, delegating review duties to the general review
    /// page.
    /// </summary>
    internal class PseudoReviewPageController : IAWSWizardPageController
    {
        #region IAWSWizardPageController Members

        public string PageID
        {
            get { return GetType().FullName; }
        }

        public IAWSWizard HostingWizard { get; set; }

        public string PageGroup
        {
            get { return AWSWizardConstants.DefaultPageGroup; }
        }

        public string PageTitle
        {
            get { return string.Empty; }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public string PageDescription
        {
            get { return string.Empty; }
        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (navigationReason != AWSWizardConstants.NavigationReason.movingBack)
            {
                string service = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_TemplateServiceOwner] as string;
                if (service == DeploymentServiceIdentifiers.BeanstalkServiceName)
                    HostingWizard.SetProperty(DeploymentWizardProperties.ReviewProperties.propkey_ServiceReviewPanels, ConstructReviewPanels());
            }

            return false;
        }

        public System.Windows.Controls.UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            return null;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            return true;
        }

        public bool QueryFinishButtonEnablement()
        {
            return true;
        }

        public void TestForwardTransitionEnablement()
        {
        }

        public bool AllowShortCircuit()
        {
            string service = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_TemplateServiceOwner] as string;
            if (service == DeploymentServiceIdentifiers.BeanstalkServiceName)
            {
                HostingWizard.SetProperty(DeploymentWizardProperties.ReviewProperties.propkey_ServiceReviewPanels, ConstructReviewPanels());
                HostingWizard[DeploymentWizardProperties.ReviewProperties.propkey_LaunchStatusOnClose] = true;
            }
            return true;
        }

        #endregion

        List<ServiceReviewPanelInfo> ConstructReviewPanels()
        {
            List<ServiceReviewPanelInfo> reviewPanels = new List<ServiceReviewPanelInfo>();

            bool newDeployment = !(bool)HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy];
            bool showOptions = false;
            if (HostingWizard.IsPropertySet(DeploymentWizardProperties.DeploymentTemplate.propkey_RedeployVersion))
            {
                if ((bool)HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_RedeployVersion])
                    showOptions = (bool)HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_CreateNewEnv];
            }
            else
                showOptions = (bool)HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_CreateNewEnv];

            reviewPanels.Add(ApplicationReviewPanel(!newDeployment));
            reviewPanels.Add(EnvironmentReviewPanel(!newDeployment));
            if (newDeployment)
                reviewPanels.Add(AWSOptionsReviewPanel());
            if (newDeployment || showOptions)
                reviewPanels.Add(ApplicationOptionsReviewPanel(!newDeployment));
            if (HostingWizard.IsPropertySet(BeanstalkDeploymentWizardProperties.DatabaseOptions.propkey_RDSSecurityGroups))
                reviewPanels.Add(DatabaseReviewPanel());

            return reviewPanels;
        }

        ServiceReviewPanelInfo ApplicationReviewPanel(bool isRedeploying)
        {
            TextBlock tb = CreateTextBlock();
            StringBuilder sb = new StringBuilder();
            
            if (!isRedeploying)
            {
                sb.AppendFormat("Publish new application '{0}'", HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] as string);
                string appDesc = HostingWizard[BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_AppDescription] as string;
                if (!string.IsNullOrEmpty(appDesc))
                    sb.AppendFormat(", with description '{0}'", appDesc);
                sb.Append(".");
            }
            else
                sb.AppendFormat("Update existing application '{0}'.", HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] as string);

            sb.AppendLine();

            if (Convert.ToBoolean(HostingWizard[BeanstalkDeploymentWizardProperties.DeploymentModeProperties.propkey_IncrementalDeployment]))
                sb.Append("Publish application using incremental deployment.");
            else
            {
                string appVersion = HostingWizard[BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_VersionLabel] as string;
                if (!string.IsNullOrEmpty(appVersion))
                    sb.AppendFormat("Publishing with version label '{0}'.", appVersion);
                else
                    sb.Append("No version label specified.");
            }

            tb.Text = sb.ToString();
            return new ServiceReviewPanelInfo { ReviewPanelHeader = "Application", ReviewPanel = tb };
        }

        ServiceReviewPanelInfo EnvironmentReviewPanel(bool isRedeploying)
        {
            TextBlock tb = CreateTextBlock();
            StringBuilder sb = new StringBuilder();

            if ((bool)HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_CreateNewEnv])
            {
                sb.AppendFormat("Publish to new environment named '{0}' of type '{1}'",
                                HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvName] as string,
                                HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvType] as string);

                string envDesc = HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvDescription] as string;
                if (!string.IsNullOrEmpty(envDesc))
                    sb.AppendFormat(", with description '{0}'", envDesc);
                sb.Append(".");

                string cname = HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_CName] as string;
                if (!string.IsNullOrEmpty(cname))
                {
                    sb.AppendLine();
                    sb.AppendFormat("Apply CNAME '{0}'.", cname);
                }
            }
            else
                sb.AppendFormat("Publish to existing environment '{0}'.", HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvName] as string);

            tb.Text = sb.ToString();
            return new ServiceReviewPanelInfo { ReviewPanelHeader = "Environment", ReviewPanel = tb };
        }

        ServiceReviewPanelInfo AWSOptionsReviewPanel()
        {
            TextBlock tb = CreateTextBlock();
            StringBuilder sb = new StringBuilder();

            string customAMI = HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_CustomAMIID] as string;
            sb.AppendLine(string.Format("Run the application in container '{0}', size '{1}' ({2}){3}.",
                                HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_SolutionStack] as string,
                                HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_InstanceTypeName] as string,
                                HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_InstanceTypeID] as string,
                                string.IsNullOrEmpty(customAMI) ? " using the default AMI"
                                                                : string.Format(" using a custom AMI with ID {0}", customAMI)));

            if (HostingWizard.IsPropertySet(BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_InstanceProfileName))
            {
                var roleName = HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_InstanceProfileName] as string;
                if (roleName == BeanstalkParameters.DefaultRoleName)
                    sb.AppendLine(string.Format("Launch the instance(s) with the default IAM role '{0}'.", roleName));
                else
                    sb.AppendLine(string.Format("Launch the instance(s) with the IAM role '{0}'. This role will be modified to with the S3::PutObject permissions to enable log publishing", 
                                                roleName));
            }

            string keypair = HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_KeyPairName] as string;
            if (!string.IsNullOrEmpty(keypair))
            {
                bool createKeyPair = (bool)HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_CreateKeyPair];
                if (createKeyPair)
                    sb.AppendFormat("Create a new key pair named '{0}' with deployment.", keypair);
                else
                    sb.AppendFormat("Use existing key pair '{0}'.", keypair);
            }
            else
                sb.Append("Do not launch EC2 instances with a keypair.");

            if (HostingWizard.GetProperty<bool>(BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_LaunchIntoVPC))
            {
                sb.AppendLine();
                sb.AppendFormat("Launch into VPC '{0}'", HostingWizard.GetProperty<string>(BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_VPCId));
                sb.AppendFormat(" with security group '{0}'.", HostingWizard.GetProperty<string>(BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_VPCSecurityGroup));

                sb.AppendLine();
                if (!IsSingleInstanceEnvironmentType)
                {
                    sb.AppendFormat("Use Elastic Load Balancer scheme '{0}' and subnet '{1}'.",
                        HostingWizard.GetProperty<string>(BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_ELBScheme),
                        HostingWizard.GetProperty<string>(BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_ELBSubnet));
                    sb.AppendLine();
                }
                sb.AppendFormat("Use instances subnet '{0}'.", HostingWizard.GetProperty<string>(BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_InstanceSubnet));
                sb.AppendLine();
            }

            tb.Text = sb.ToString();
            return new ServiceReviewPanelInfo { ReviewPanelHeader = "AWS Options", ReviewPanel = tb };
        }

        ServiceReviewPanelInfo ApplicationOptionsReviewPanel(bool isRedeploying)
        {
            var panel = new StackPanel() { Orientation = Orientation.Vertical };

            TextBlock tb = CreateTextBlock();
            StringBuilder sb = new StringBuilder();

            DeploymentTemplateWrapperBase template = null;
            if (HostingWizard.IsPropertySet(DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplate))
                template = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplate] as DeploymentTemplateWrapperBase;
            
            string targetFramework = HostingWizard[DeploymentWizardProperties.AppOptions.propkey_TargetFramework] as string;
            if (!string.IsNullOrEmpty(targetFramework))
            {
                sb.AppendLine(string.Format("Use an application pool supporting .NET Framework {0}.", targetFramework));
                if (!isRedeploying)
                {
                    string customAMI = HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_CustomAMIID] as string;

                    if (template != null && !template.SupportsFrameworkVersion(targetFramework) && string.IsNullOrEmpty(customAMI))
                    {
                        tb.Text = sb.ToString();
                        panel.Children.Add(tb);

                        tb = CreateTextBlock(FontStyles.Italic, FontWeights.Normal);
                        tb.Foreground = Brushes.Red;
                        sb.Length = 0;

                        sb.AppendLine("WARNING: the selected .Net Framework version is not supported in the default AMI associated with this container. An appropriately-configured custom AMI should be used but one has not been specified.");
                        tb.Text = sb.ToString();
                        panel.Children.Add(tb);

                        tb = CreateTextBlock();
                        sb.Length = 0;
                    }

                    if (!string.IsNullOrEmpty(customAMI))
                        sb.AppendLine(string.Format("Use custom AMI with id {0} for EC2 instances.", customAMI));
                }
            }
            
            if (HostingWizard[DeploymentWizardProperties.AppOptions.propkey_Enable32BitApplications] != null && 
                (bool)HostingWizard[DeploymentWizardProperties.AppOptions.propkey_Enable32BitApplications])
                sb.AppendLine("Enable 32bit support for the application pool.");

            if (DeploymentWizardHelper.IsSingleInstanceEnvironment(HostingWizard))
                sb.AppendLine("The status of the single EC2 instance will be used to determine environment health.");
            else
            {
                string healthUrl = HostingWizard[DeploymentWizardProperties.AppOptions.propkey_HealthCheckUrl] as string;
                if (!string.IsNullOrEmpty(healthUrl))
                    sb.AppendLine(string.Format("Use '{0}' as the URL for health-checks.", healthUrl));
            }

            string email = HostingWizard[BeanstalkDeploymentWizardProperties.AppOptionsProperties.propkey_NotificationEmail] as string;
            if (!string.IsNullOrEmpty(email))
                sb.AppendLine(string.Format("Send email notifications to '{0}'.", email));
            else
                sb.AppendLine("No email notification address given.");
            tb.Text = sb.ToString();
            
            panel.Children.Add(tb);
            return new ServiceReviewPanelInfo { ReviewPanelHeader = "Application Options", ReviewPanel = panel };
        }

        ServiceReviewPanelInfo DatabaseReviewPanel()
        {
            TextBlock tb = CreateTextBlock();
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Add the EC2 security group for my environment instance to the following RDS Security Groups:");
            List<string> dbSecurityGroups = HostingWizard[BeanstalkDeploymentWizardProperties.DatabaseOptions.propkey_RDSSecurityGroups]
                as List<string>;
            foreach (string group in dbSecurityGroups)
            {
                sb.AppendLine("    " + group);
            }

            tb.Text = sb.ToString();
            return new ServiceReviewPanelInfo { ReviewPanelHeader = "RDS Database", ReviewPanel = tb };
        }

        TextBlock CreateTextBlock()
        {
            return CreateTextBlock(FontStyles.Normal, FontWeights.Normal);
        }

        TextBlock CreateTextBlock(FontStyle fontStyle, FontWeight fontWeight)
        {
            TextBlock tb = new TextBlock();
            tb.TextWrapping = TextWrapping.Wrap;
            tb.FontStyle = fontStyle;
            tb.FontWeight = fontWeight;
            return tb;
        }

        bool IsSingleInstanceEnvironmentType
        {
            get
            {
                if (HostingWizard.IsPropertySet(BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvType))
                    return ((string)HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvType]).Equals(BeanstalkConstants.EnvType_SingleInstance);

                return false;
            }
        }
    }
}
