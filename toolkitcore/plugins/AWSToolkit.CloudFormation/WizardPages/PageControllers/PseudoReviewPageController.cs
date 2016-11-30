using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.PageControllers;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.PluginServices.Deployment;

namespace Amazon.AWSToolkit.CloudFormation.WizardPages.PageControllers
{
    /// <summary>
    /// 'Virtual' review page that contributes service-specific review text panels into a 
    /// list for occasion when the deployment service is CloudFormation. This page will never
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
                if (IsWizardInCloudFormationMode)
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
            if (IsWizardInCloudFormationMode)
            {
                HostingWizard.SetProperty(DeploymentWizardProperties.ReviewProperties.propkey_ServiceReviewPanels, ConstructReviewPanels());
                HostingWizard[DeploymentWizardProperties.ReviewProperties.propkey_LaunchStatusOnClose] = true;
            }
            return true;
        }

        #endregion

        bool IsWizardInCloudFormationMode
        {
            get
            {
                string service = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_TemplateServiceOwner] as string;
                return service == DeploymentServiceIdentifiers.CloudFormationServiceName;
            }
        }

        List<ServiceReviewPanelInfo> ConstructReviewPanels()
        {
            List<ServiceReviewPanelInfo> reviewPanels = new List<ServiceReviewPanelInfo>();

            bool isRedeploying = (bool)HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy];
            if (!isRedeploying)
            {
                reviewPanels.Add(TemplateReviewPanel());
                reviewPanels.Add(AWSOptionsReviewPanel());
                reviewPanels.Add(ApplicationOptionsReviewPanel(isRedeploying));
                ServiceReviewPanelInfo panel = ParametersReviewPanel();
                if (panel != null)
                    reviewPanels.Add(panel);
            }
            else
            {
                reviewPanels.Add(RedeployReviewPanel());
                reviewPanels.Add(ApplicationOptionsReviewPanel(isRedeploying));
            }

            return reviewPanels;
        }

        ServiceReviewPanelInfo TemplateReviewPanel()
        {
            TextBlock tb = CreateTextBlock(); ;
            var sb = new StringBuilder();

            var template
                = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplate] as DeploymentTemplateWrapperBase;
            sb.AppendFormat("Publish the application using template '{0}'.",
                                string.IsNullOrEmpty(template.TemplateHeader) ? template.TemplateFilename : template.TemplateHeader);

            tb.Text = sb.ToString();
            return new ServiceReviewPanelInfo { ReviewPanelHeader = "Template", ReviewPanel = tb };
        }

        ServiceReviewPanelInfo AWSOptionsReviewPanel()
        {
            TextBlock tb = CreateTextBlock();
            var sb = new StringBuilder();

            var amiID = HostingWizard[CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_ContainerAMI] as string;
            if (!string.IsNullOrEmpty(amiID))
            {
                var containerName =
                    HostingWizard[CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_ContainerName]
                    as string;
                sb.AppendFormat("Use the default AMI (ID '{0}') running {1}, ", amiID, containerName);
            }
            else
            {
                amiID = HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_CustomAMIID] as string;
                sb.AppendFormat("Use a custom AMI with ID '{0}' instead of the template default, ", amiID);
            }

            sb.AppendLine(string.Format(" with EC2 instance size '{0}' ({1}).",
                                HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_InstanceTypeName] as string,
                                HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_InstanceTypeID] as string));

            sb.AppendLine();

            string keypair = null;
            if (HostingWizard.IsPropertySet(DeploymentWizardProperties.AWSOptions.propkey_KeyPairName))
                keypair = HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_KeyPairName] as string;
            if (!string.IsNullOrEmpty(keypair))
            {
                var createKeyPair = (bool)HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_CreateKeyPair];
                sb.AppendFormat(createKeyPair
                                    ? "Launch EC2 instance(s) with a new key pair named '{0}'."
                                    : "Use existing key pair '{0}' when launching EC2 instances.", keypair);
            }
            else
                sb.Append("Do not use a keypair when launching EC2 instances.");

            // security group should be mandatory, but check in case we change our minds
            sb.AppendLine();
            if (HostingWizard.IsPropertySet(DeploymentWizardProperties.AWSOptions.propkey_SecurityGroupName))
            {
                string securityGroup = HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_SecurityGroupName] as string;
                sb.AppendFormat("Launch instances with security group '{0}'.", securityGroup);

                if (HostingWizard.IsPropertySet(DeploymentWizardProperties.AWSOptions.propkey_AutoOpenPort80))
                {
                    if ((bool)HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_AutoOpenPort80])
                    {
                        sb.AppendLine();
                        sb.Append("NOTE: Port 80 will be opened for the selected security group.");
                    }
                }
            }
            else
                sb.Append("No security group specified for EC2 instances.");

            sb.AppendLine();
            if (HostingWizard.IsPropertySet(CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_CreationTimeout))
            {
                int timeout = (int)HostingWizard[CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_CreationTimeout];
                if (timeout > 0)
                    sb.AppendFormat("Creation timeout: {0} minutes.", timeout);
            }

            if (HostingWizard.IsPropertySet(CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_RollbackOnFailure))
            {
                sb.AppendLine();
                if ((bool)HostingWizard[CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_RollbackOnFailure])
                    sb.Append("Rollback to previous version on deployment failure.");
                else
                    sb.Append("Do not rollback application version on deployment failure.");
            }

            tb.Text = sb.ToString();
            return new ServiceReviewPanelInfo { ReviewPanelHeader = "AWS Options", ReviewPanel = tb };
        }

        ServiceReviewPanelInfo ApplicationOptionsReviewPanel(bool isRedeploying)
        {
            var panel = new StackPanel() { Orientation = Orientation.Vertical };

            TextBlock tb = CreateTextBlock();
            var sb = new StringBuilder();

            string targetRuntime = HostingWizard[DeploymentWizardProperties.AppOptions.propkey_TargetRuntime] as string;
            if (!string.IsNullOrEmpty(targetRuntime))
            {
                sb.AppendLine(string.Format("Use an application pool supporting .NET Runtime v{0}.", targetRuntime));
                if (!isRedeploying)
                {
                    var template = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplate] as DeploymentTemplateWrapperBase;
                    if (!template.SupportsFrameworkVersion(targetRuntime))
                    {
                        string customAMI = HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_CustomAMIID] as string;
                        if (string.IsNullOrEmpty(customAMI))
                        {
                            tb.Text = sb.ToString();
                            panel.Children.Add(tb);

                            tb = CreateTextBlock(FontStyles.Italic, FontWeights.Normal);
                            tb.Foreground = Brushes.Red;

                            sb.Length = 0;
                            sb.AppendLine("WARNING: the selected .NET Framework version is not supported in the default AMI associated with this deployment template. An appropriately-configured custom AMI should be used but one has not been specified.");
                            tb.Text = sb.ToString();
                            panel.Children.Add(tb);

                            tb = CreateTextBlock();
                            sb.Length = 0;
                        }
                    }
                }
            }

            if ((bool)HostingWizard[DeploymentWizardProperties.AppOptions.propkey_Enable32BitApplications])
                sb.AppendLine("Enable 32bit support for the application pool.");

            string healthUrl = HostingWizard[DeploymentWizardProperties.AppOptions.propkey_HealthCheckUrl] as string;
            if (!string.IsNullOrEmpty(healthUrl))
                sb.AppendLine(string.Format("Use '{0}' as the URL for health-checks.", healthUrl));

            tb.Text = sb.ToString();
            panel.Children.Add(tb);

            return new ServiceReviewPanelInfo { ReviewPanelHeader = "Application Options", ReviewPanel = panel };
        }

        ServiceReviewPanelInfo ParametersReviewPanel()
        {
            var parameterValues = HostingWizard[CloudFormationDeploymentWizardProperties.TemplateParametersProperties.propkey_TemplateParameterValues] 
                as Dictionary<string, CloudFormationTemplateWrapper.TemplateParameter>;

            if (parameterValues == null)
                return null;

            var panel = new StackPanel() { Orientation = Orientation.Vertical };
            foreach (var kvp in parameterValues.OrderBy(x => x.Key))
            {
                if (!kvp.Value.Hidden)
                {
                    TextBlock tb = CreateTextBlock();
                    tb.Text = string.Format("{0}: {1}", kvp.Key, kvp.Value.OverrideValue);
                    panel.Children.Add(tb);
                }
            }

            return new ServiceReviewPanelInfo { ReviewPanelHeader = "Parameters", ReviewPanel = panel };
        }

        ServiceReviewPanelInfo RedeployReviewPanel()
        {
            TextBlock tb = CreateTextBlock();
            StringBuilder sb = new StringBuilder();

            string stackName = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] as string;
            sb.AppendFormat("Redeploy application to existing CloudFormation stack '{0}'.", stackName);

            tb.Text = sb.ToString();
            return new ServiceReviewPanelInfo { ReviewPanelHeader = "Application", ReviewPanel = tb };
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
    }
}
