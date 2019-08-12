using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.PageControllers;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using AWSDeployment;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers.Deployment
{
    /// <summary>
    /// 'Virtual' review page that contributes service-specific review text panels into a 
    /// list for occasion when the deployment service is Beanstalk. This page will never
    /// actually appear in the wizard flow, delegating review duties to the general review
    /// page.
    /// </summary>
    internal class PseudoReviewPageController : IAWSWizardPageController
    {
        public string PageID => GetType().FullName;

        public IAWSWizard HostingWizard { get; set; }

        public string PageGroup => DeploymentWizardPageGroups.ReviewGroup;

        public string PageTitle => string.Empty;

        public string ShortPageTitle => null;

        public string PageDescription => string.Empty;

        public void ResetPage()
        {

        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (navigationReason != AWSWizardConstants.NavigationReason.movingBack)
            {
                HostingWizard.SetProperty(DeploymentWizardProperties.ReviewProperties.propkey_ServiceReviewPanels, ConstructReviewPanels());
            }

            return false;
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
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
            HostingWizard.SetProperty(DeploymentWizardProperties.ReviewProperties.propkey_ServiceReviewPanels, ConstructReviewPanels());
            HostingWizard[DeploymentWizardProperties.ReviewProperties.propkey_LaunchStatusOnClose] = true;
            return true;
        }

        List<ServiceReviewPanelInfo> ConstructReviewPanels()
        {
            var reviewPanels = new List<ServiceReviewPanelInfo>();

            var isRedeployment = false;
            if (HostingWizard.IsPropertySet(DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy))
                isRedeployment = (bool)HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy];

            reviewPanels.Add(ApplicationReviewPanel(isRedeployment));
            if (!isRedeployment)
            {
                reviewPanels.Add(AWSOptionsReviewPanel());
                reviewPanels.Add(PermissionsReviewPanel());
            }
            reviewPanels.Add(ApplicationOptionsReviewPanel(isRedeployment));

            return reviewPanels;
        }

        ServiceReviewPanelInfo ApplicationReviewPanel(bool isRedeployment)
        {
            var tb = CreateTextBlock();
            var sb = new StringBuilder();

            if (isRedeployment)
            {
                // redeployment is always to an existing app environment
                sb.AppendFormat("Redeploy to environment '{0}' for application '{1}'.", 
                                HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvName] as string,
                                HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] as string);
            }
            else
            {
                // new deployment can be to a new environment for an existing app
                if (HostingWizard.IsPropertySet(DeploymentWizardProperties.DeploymentTemplate.propkey_RedeploymentInstance))
                {
                    // new env, existing app                    
                    sb.AppendFormat("Create a new environment '{0}' for application '{1}'.", 
                                    HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvName] as string,
                                    HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] as string);
                }
                else
                {
                    // new app, first env                    
                    sb.AppendFormat("Create a new application '{0}' with environment '{1}'.", 
                                    HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] as string,
                                    HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvName] as string);
                }

                var cname = HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_CName] as string;
                sb.AppendLine();
                sb.AppendFormat("Use CNAME '{0}' for environment.", cname);
                sb.AppendLine();
                sb.AppendFormat("(The application will be accessible at http://{0}.elasticbeanstalk.com.)", cname);
            }

            tb.Text = sb.ToString();
            return new ServiceReviewPanelInfo { ReviewPanelHeader = "Application", ReviewPanel = tb };
        }

        ServiceReviewPanelInfo AWSOptionsReviewPanel()
        {
            var tb = CreateTextBlock();
            var sb = new StringBuilder();

            var isSingleInstanceEnvironment = DeploymentWizardHelper.IsSingleInstanceEnvironment(HostingWizard);

            var customAmiId = string.Empty;
            if (HostingWizard.IsPropertySet(DeploymentWizardProperties.AWSOptions.propkey_CustomAMIID))
                customAmiId = HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_CustomAMIID] as string;

            sb.AppendLine(string.Format("Create a {0} environment using container '{1}', with instance type '{2}' ({3}).\r\n{4}.",
                                        isSingleInstanceEnvironment
                                                ? "single instance"
                                                : "load balanced, auto scaled",
                                        HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_SolutionStack] as string,
                                        HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_InstanceTypeName] as string,
                                        HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_InstanceTypeID] as string,
                                        string.IsNullOrEmpty(customAmiId) ? "Use the default AMI for the container"
                                                                        : string.Format("Use a custom AMI with ID {0}", customAmiId)));

            var keypair = HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_KeyPairName] as string;
            if (!string.IsNullOrEmpty(keypair))
            {
                var createKeyPair = (bool)HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_CreateKeyPair];
                sb.AppendFormat(createKeyPair
                                    ? "Launch EC2 instance(s) with a new key pair named '{0}'."
                                    : "Use existing key pair '{0}' when launching EC2 instances.", keypair);
            }
            else
                sb.Append("Do not use a keypair when launching EC2 instances.");

            sb.AppendLine();

            if (HostingWizard.GetProperty<bool>(BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_LaunchIntoVPC))
            {
                sb.AppendLine();
                sb.AppendFormat("Launch into VPC '{0}'", HostingWizard.GetProperty<string>(BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_VPCId));
                sb.AppendFormat(" with security group '{0}'.", HostingWizard.GetProperty<string>(BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_VPCSecurityGroup));

                sb.AppendLine();
                if (!isSingleInstanceEnvironment)
                {
                    sb.AppendFormat("Use Elastic Load Balancer scheme '{0}' and subnet '{1}'.",
                        HostingWizard.GetProperty<string>(BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_ELBScheme),
                        HostingWizard.GetProperty<string>(BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_ELBSubnet));
                    sb.AppendLine();
                }
                sb.AppendFormat("Use instances subnet '{0}'.", HostingWizard.GetProperty<string>(BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_InstanceSubnet));
                sb.AppendLine();
            }
            else
            {
                // if the user/region is vpc-by-default and the user didn't select to use a custom vpc, alert the attentive user
                // that the app will launch into the default vpc
                if (HostingWizard.GetProperty<bool>(DeploymentWizardProperties.SeedData.propkey_VpcOnlyMode))
                {
                    sb.AppendLine();
                    sb.AppendFormat("The application will launch in the default VPC '{0}'", HostingWizard.GetProperty<string>(DeploymentWizardProperties.AWSOptions.propkey_DefaultVpcId));
                }
            }

            if (HostingWizard.IsPropertySet(BeanstalkDeploymentWizardProperties.DatabaseOptions.propkey_RDSSecurityGroups))
            {
                sb.AppendLine();

                sb.AppendLine("Add the EC2 security group for my environment instance to the following RDS Security Groups:");
                var dbSecurityGroups = HostingWizard[BeanstalkDeploymentWizardProperties.DatabaseOptions.propkey_RDSSecurityGroups] as List<string>;
                foreach (var group in dbSecurityGroups)
                {
                    sb.AppendLine("    " + group);
                }
            }

            if (HostingWizard.IsPropertySet(BeanstalkDeploymentWizardProperties.DatabaseOptions.propkey_VPCSecurityGroups))
            {
                sb.AppendLine();

                sb.AppendLine("Add the EC2 security group for my environment instance to the following EC2-VPC Security Groups:");
                var vpcSecurityGroups = HostingWizard[BeanstalkDeploymentWizardProperties.DatabaseOptions.propkey_VPCSecurityGroups] as List<string>;
                foreach (var group in vpcSecurityGroups)
                {
                    sb.AppendLine("    " + group);
                }
            }

            tb.Text = sb.ToString();
            return new ServiceReviewPanelInfo { ReviewPanelHeader = "AWS Options", ReviewPanel = tb };
        }

        ServiceReviewPanelInfo PermissionsReviewPanel()
        {
            StringBuilder sb = new StringBuilder();

            if (HostingWizard.IsPropertySet(BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_InstanceProfileName))
            {
                var roleName = HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_InstanceProfileName] as string;
                if (roleName == BeanstalkParameters.DefaultRoleName)
                    sb.AppendLine(string.Format("Launch the instance(s) with the default IAM role '{0}'.", roleName));
                else
                    sb.AppendLine(string.Format("Launch the instance(s) with the IAM role '{0}'. This role will be modified to with the S3::PutObject permissions to enable log publishing",
                                                roleName));
            }

            var serviceRoleName = HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_ServiceRoleName] as string;
            if (serviceRoleName == BeanstalkParameters.DefaultServiceRoleName)
                sb.AppendLine(string.Format("Launch the instance(s) with the default service role '{0}'.", serviceRoleName));
            else
                sb.AppendLine(string.Format("Launch the instance(s) with the service role '{0}'.", serviceRoleName));

            TextBlock tb = CreateTextBlock();
            tb.Text = sb.ToString();
            return new ServiceReviewPanelInfo { ReviewPanelHeader = "Permissions", ReviewPanel = tb };
        }

        ServiceReviewPanelInfo ApplicationOptionsReviewPanel(bool isRedeployment)
        {
            var panel = new StackPanel() { Orientation = Orientation.Vertical };

            var tb = CreateTextBlock();
            var sb = new StringBuilder();

            var redeployingAppVersion = false;
            if (HostingWizard.IsPropertySet(DeploymentWizardProperties.SeedData.propkey_RedeployingAppVersion))
                redeployingAppVersion = (bool)HostingWizard[DeploymentWizardProperties.SeedData.propkey_RedeployingAppVersion];

            var appVersion = HostingWizard[BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_VersionLabel] as string;
            if (!redeployingAppVersion)
            {
                sb.AppendFormat("Use project configuration '{0}' when building for deployment.", HostingWizard[DeploymentWizardProperties.AppOptions.propkey_SelectedBuildConfiguration] as string);
                sb.AppendLine();
                sb.AppendFormat("Deploy as application version '{0}'", appVersion);
            }
            else
                sb.AppendFormat("Redeploying application version '{0}'", appVersion);

            sb.AppendLine();

            DeploymentTemplateWrapperBase template = null;
            if (HostingWizard.IsPropertySet(DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplate))
                template = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplate] as DeploymentTemplateWrapperBase;

            var isCoreCLRDeployment = false;
            if (HostingWizard.IsPropertySet(DeploymentWizardProperties.SeedData.propkey_ProjectType))
                isCoreCLRDeployment = (HostingWizard[DeploymentWizardProperties.SeedData.propkey_ProjectType] as string).Equals(DeploymentWizardProperties.NetCoreWebProject, StringComparison.Ordinal);
            var targetRuntime = HostingWizard[DeploymentWizardProperties.AppOptions.propkey_TargetRuntime] as string;     // this is framework if coreclr
            var iisAppPath = HostingWizard[DeploymentWizardProperties.AppOptions.propkey_DeployIisAppPath] as string;

            if (!string.IsNullOrEmpty(targetRuntime))
            {
                if (isCoreCLRDeployment)
                    sb.AppendFormat("Deploy a web application supporting .NET Core Framework {0}", targetRuntime);
                else
                    sb.AppendFormat("Use an IIS application pool supporting .NET Runtime {0}", targetRuntime);
                if (!redeployingAppVersion)
                {
                    sb.AppendFormat(" with path '{0}'.", iisAppPath);
                }
                sb.AppendLine();

                if (!isRedeployment)
                {
                    var customAmiId = string.Empty;
                    if (HostingWizard.IsPropertySet(DeploymentWizardProperties.AWSOptions.propkey_CustomAMIID))
                        customAmiId = HostingWizard[DeploymentWizardProperties.AWSOptions.propkey_CustomAMIID] as string;

                    if (template != null && !template.SupportsFrameworkVersion(targetRuntime) && string.IsNullOrEmpty(customAmiId))
                    {
                        tb.Text = sb.ToString();
                        panel.Children.Add(tb);

                        tb = CreateTextBlock(FontStyles.Italic, FontWeights.Normal);
                        tb.Foreground = Brushes.Red;
                        sb.Length = 0;

                        sb.AppendLine("WARNING: the selected .NET Runtime version is not supported in the default AMI associated with this container. An appropriately-configured custom AMI should be used but one has not been specified.");
                        tb.Text = sb.ToString();
                        panel.Children.Add(tb);

                        tb = CreateTextBlock();
                        sb.Length = 0;
                    }
                }
            }
            else if (!redeployingAppVersion)
            {
                sb.AppendLine(string.Format("Set the IIS application path to '{0}'.", iisAppPath));
            }
            
            if (!isCoreCLRDeployment)
            {
                if (HostingWizard.IsPropertySet(DeploymentWizardProperties.AppOptions.propkey_Enable32BitApplications)
                        && (bool)HostingWizard[DeploymentWizardProperties.AppOptions.propkey_Enable32BitApplications])
                    sb.AppendLine("Enable 32bit support for the application pool.");
            }

            if (DeploymentWizardHelper.IsSingleInstanceEnvironment(HostingWizard))
                sb.AppendLine("The status of the single EC2 instance will be used to determine environment health.");
            else
            {
                var healthUrl = HostingWizard[DeploymentWizardProperties.AppOptions.propkey_HealthCheckUrl] as string;
                if (!string.IsNullOrEmpty(healthUrl))
                    sb.AppendLine(string.Format("Use '{0}' as the URL for health-checks.", healthUrl));
            }

            if ((bool) HostingWizard.GetProperty(BeanstalkDeploymentWizardProperties.AppOptionsProperties.propkey_XRayAvailable))
            {
                if (HostingWizard.IsPropertySet(BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_EnableXRayDaemon))
                {
                    var enableXRay = (bool)HostingWizard[BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_EnableXRayDaemon];
                    if (enableXRay)
                    {
                        sb.Append("Enable AWS X-Ray Daemon.");
                    }
                }
            }

            if (HostingWizard.IsPropertySet(BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_EnableEnhancedHealth))
            {
                var enableXRay = (bool)HostingWizard[BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_EnableEnhancedHealth];
                if (enableXRay)
                {
                    sb.Append("Enable Enhanced Health Monitoring.");
                }
            }

            if (!isCoreCLRDeployment)
            {
                IDictionary<string, string> appSettings = null;
                if (HostingWizard.IsPropertySet(DeploymentWizardProperties.AppOptions.propkey_EnvAppSettings))
                    appSettings = HostingWizard[DeploymentWizardProperties.AppOptions.propkey_EnvAppSettings] as IDictionary<string, string>;

                if (appSettings != null && appSettings.Any())
                {
                    sb.AppendLine("Custom application settings:");
                    foreach (var k in appSettings.Keys)
                    {
                        sb.AppendFormat("    key='{0}', value='{1}'", k, appSettings[k]);
                        sb.AppendLine();
                    }
                }
            }

            tb.Text = sb.ToString();
            
            panel.Children.Add(tb);
            return new ServiceReviewPanelInfo { ReviewPanelHeader = "Application Options", ReviewPanel = panel };
        }

        TextBlock CreateTextBlock()
        {
            return CreateTextBlock(FontStyles.Normal, FontWeights.Normal);
        }

        static TextBlock CreateTextBlock(FontStyle fontStyle, FontWeight fontWeight)
        {
            var tb = new TextBlock {TextWrapping = TextWrapping.Wrap, FontStyle = fontStyle, FontWeight = fontWeight};
            return tb;
        }
    }
}
