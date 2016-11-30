using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

using Amazon.AWSToolkit.CloudFormation.Model;
using Amazon.AWSToolkit.CloudFormation.WizardPages.PageUI;

namespace Amazon.AWSToolkit.CloudFormation.WizardPages.PageControllers
{
    internal class CreateStackReviewPageController : IAWSWizardPageController
    {
        CreateStackReviewPage _pageUI = null;

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
            get { return "Review"; }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public string PageDescription
        {
            get { return "Review the information below, then click Finish to start deployment."; }
        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return true;
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
                _pageUI = new CreateStackReviewPage();

            return _pageUI;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            this._pageUI.ClearPanels();

            AddTemplateReviewPanel();
            AddParametersPanel();

            HostingWizard.RequestFinishEnablement(this);
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (navigationReason == AWSWizardConstants.NavigationReason.finishPressed)
            {
                //if (_pageUI != null)
                //    HostingWizard[DeploymentWizardProperties.ReviewProperties.propkey_LaunchStatusOnClose] = _pageUI.OpenStatusOnClose;
            }
            else
                if (navigationReason == AWSWizardConstants.NavigationReason.movingBack)
                    HostingWizard.SetNavigationButtonText(AWSWizardConstants.NavigationButtons.Finish, "Finish");

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
            return true;
        }

        #endregion

        void AddTemplateReviewPanel()
        {
            var panel = new StackPanel() { Orientation = Orientation.Vertical };

            TextBlock tb;

            tb = new TextBlock();
            tb.Text = string.Format("Stack Name: {0}", HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName]);
            panel.Children.Add(tb);

            var wrapper = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplate] as CloudFormationTemplateWrapper;
            if (wrapper != null)
            {
                if (wrapper.TemplateHeader != null)
                {
                    tb = new TextBlock();
                    tb.Text = string.Format("Template Name: {0}", wrapper.TemplateHeader);
                    tb.TextWrapping = System.Windows.TextWrapping.Wrap;
                    panel.Children.Add(tb);
                }
                if (wrapper.TemplateDescription != null)
                {
                    tb = new TextBlock();
                    tb.Text = string.Format("Description: {0}", wrapper.TemplateDescription);
                    tb.TextWrapping = System.Windows.TextWrapping.Wrap;
                    panel.Children.Add(tb);
                }
                if (wrapper.TemplateSource == CloudFormationTemplateWrapper.Source.Local && wrapper.TemplateFilename != null)
                {
                    tb = new TextBlock();
                    tb.Text = string.Format("File: {0}", wrapper.TemplateFilename);
                    tb.TextWrapping = System.Windows.TextWrapping.Wrap;
                    panel.Children.Add(tb);
                }
            }

            int timeout;
            object otimeout = HostingWizard.CollectedProperties[CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_CreationTimeout];
            if (otimeout != null && int.TryParse(otimeout.ToString(), out timeout))
            {
                if (timeout > 0)
                {
                    tb = new TextBlock();
                    tb = new TextBlock();
                    tb.Text = string.Format("Creation Timeout: {0} minutes.", timeout);
                    panel.Children.Add(tb);
                }
            }

            string snsTopic = HostingWizard[CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_SNSTopic] as string;
            if (snsTopic != null)
            {
                tb = new TextBlock();
                tb = new TextBlock();
                tb.Text = string.Format("SNS Topic: {0}", snsTopic);
                panel.Children.Add(tb);
            }

            tb = new TextBlock();
            tb.Text = string.Format("Rollback on failure: {0}", HostingWizard[CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_RollbackOnFailure]);
            panel.Children.Add(tb);

            this._pageUI.AddReviewPanel("Template", panel);
        }

        void AddParametersPanel()
        {
            var parameterValues = HostingWizard[CloudFormationDeploymentWizardProperties.TemplateParametersProperties.propkey_TemplateParameterValues] as Dictionary<string, CloudFormationTemplateWrapper.TemplateParameter>;

            if (parameterValues == null)
                return;

            var panel = new StackPanel() { Orientation = Orientation.Vertical };
            foreach (var kvp in parameterValues.OrderBy(x => x.Key))
            {
                TextBlock tb = new TextBlock();
                tb.TextWrapping = System.Windows.TextWrapping.Wrap;
                string value = kvp.Value.NoEcho ? CloudFormationConstants.NO_ECHO_VALUE_RETURN_FROM_CONSOLE : kvp.Value.OverrideValue;
                tb.Text = string.Format("{0}: {1}", kvp.Key, value);
                panel.Children.Add(tb);
            }


            this._pageUI.AddReviewPanel("Parameters", panel);
        }
    }
}
