using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.CommonUI;

using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI;
using Amazon.AWSToolkit.ElasticBeanstalk.Controller;

using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers
{
    internal class ValidationPageController : IAWSWizardPageController
    {
        ValidationPage _pageUI = null;
        int warnings, errors;

        #region IAWSWizardPageController Members

        public string PageID
        {
            get { return GetType().FullName; }
        }

        public IAWSWizard HostingWizard { get; set; }

        public bool GetPageDescriptorInfo(out string pageTitle, out string pageDescription)
        {
            pageTitle = "Pre-deployment Validation";
            pageDescription = string.Empty;
            return true;
        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            bool showPage = false;
            // if user has decided to nav back from Review, skip
            if (navigationReason == AWSWizardConstants.NavigationReason.movingForward)
                showPage = !ValidateSettings();

            return showPage;
        }

        public Panel PageActivating()
        {
            return _pageUI;
        }

        public void PageActivated()
        {
            if (errors > 0)
                HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, false);
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
            return ValidateSettings();
        }

        #endregion

        bool ValidateSettings()
        {
            /*
            warnings = errors = 0;

            // validate settings; if any are bad then request the page is shown
            string awsAccessKey;
            string awsSecretKey;
            DeploymentWizardHelper.GetAWSAccountKeys(HostingWizard, out awsAccessKey, out awsSecretKey);

            var beanstalkClient = AWSClientFactory.CreateAmazonElasticBeanstalkClient(awsAccessKey, awsSecretKey);

            ValidateConfigurationSettingsResponse response;
            string validationText = string.Empty;
            try
            {
                response =
                    beanstalkClient.ValidateConfigurationSettings(new ValidateConfigurationSettingsRequest()
                                                                        //.WithTemplateName("64bit Amazon Linux running Tomcat 6") // HostingWizard[OptionsPageProperties.propkey_ContainerType] as string
                                                                        .WithApplicationName(HostingWizard[ApplicationPageProperties.propkey_AppName] as string)
                                                                        //.WithEnvironmentName(HostingWizard[EnvironmentPageProperties.propkey_EnvName] as string)
                                                                        .WithOptionSettings(DeploymentSettingsForValidation()));
                EnvironmentStatusController.messageCount(response.ValidateConfigurationSettingsResult.Messages, out warnings, out errors);

                if (errors > 0 || warnings > 0)
                {
                    var sb = new StringBuilder();

                    sb.AppendFormat("{0} error(s), {1} warning(s) issued:", errors, warnings);
                    sb.AppendLine();

                    foreach (var message in response.ValidateConfigurationSettingsResult.Messages)
                    {
                        sb.AppendLine("* " + message.Message);
                    }

                    validationText = sb.ToString();
                }
            }
            catch (AmazonElasticBeanstalkException exc)
            {
                errors = 1;
                warnings = 0;
                validationText = exc.Message;
            }

            if (errors > 0 || warnings > 0)
            {
                if (_pageUI == null)
                        _pageUI = new ValidationPage();

                _pageUI.ValidationMessages = validationText;
                return false;
            }
            */
            return true;
        }

        List<ConfigurationOptionSetting> DeploymentSettingsForValidation()
        {
            var settings = new List<ConfigurationOptionSetting>();

            settings.Add(new ConfigurationOptionSetting()
                    .WithNamespace("aws:autoscaling:launchconfiguration")
                    .WithOptionName("ImageId")
                    .WithValue("ami-f68b779f"));

            if (!string.IsNullOrEmpty(HostingWizard[DeploymentWizardProperties.OptionsProperties.propkey_InstanceTypeID] as string))
            {
                settings.Add(new ConfigurationOptionSetting()
                        .WithNamespace("aws:autoscaling:launchconfiguration")
                        .WithOptionName("InstanceType")
                        .WithValue(HostingWizard[DeploymentWizardProperties.OptionsProperties.propkey_InstanceTypeID] as string));
            }
            if (!string.IsNullOrEmpty(HostingWizard[DeploymentWizardProperties.OptionsProperties.propkey_HealthCheckUrl] as string))
            {
                settings.Add(new ConfigurationOptionSetting()
                        .WithNamespace("aws:elasticbeanstalk:application")
                        .WithOptionName("Application Healthcheck URL")
                        .WithValue(HostingWizard[DeploymentWizardProperties.OptionsProperties.propkey_HealthCheckUrl] as string));
            }
            if (!string.IsNullOrEmpty(HostingWizard[DeploymentWizardProperties.OptionsProperties.propkey_KeyPair] as string))
            {
                settings.Add(new ConfigurationOptionSetting()
                        .WithNamespace("aws:autoscaling:launchconfiguration")
                        .WithOptionName("EC2KeyName")
                        .WithValue(HostingWizard[DeploymentWizardProperties.OptionsProperties.propkey_KeyPair] as string));
            }
            if (!string.IsNullOrEmpty(HostingWizard[DeploymentWizardProperties.OptionsProperties.propkey_NotificationEmail] as string))
            {
                settings.Add(new ConfigurationOptionSetting()
                        .WithNamespace("aws:elasticbeanstalk:sns:topics")
                        .WithOptionName("Notification Endpoint")
                        .WithValue(HostingWizard[DeploymentWizardProperties.OptionsProperties.propkey_NotificationEmail] as string));
            }

            return settings;
        }
    }
}
