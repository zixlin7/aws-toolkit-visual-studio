﻿using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ECS.WizardPages.PageUI;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Amazon.AWSToolkit.ECS.WizardPages.PageControllers
{
    public class PushImageToECRPageController : IAWSWizardPageController
    {
        private PushImageToECRPage _pageUI;

        public IAWSWizard HostingWizard { get; set; }

        public string PageDescription
        {
            get
            {
                return "Select the Amazon ECR Repository to push the Docker image to.";
            }
        }

        public string PageGroup
        {
            get { return AWSWizardConstants.DefaultPageGroup; }
        }

        public string PageID
        {
            get { return GetType().FullName; }
        }

        public string PageTitle
        {
            get
            {
                return "Publish Container to AWS";
            }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public bool AllowShortCircuit()
        {
            return true;
        }

        public void ResetPage()
        {

        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            TestForwardTransitionEnablement();
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new PushImageToECRPage(this);
                _pageUI.PropertyChanged += _pageUI_PropertyChanged;
            }

            return _pageUI;
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (navigationReason != AWSWizardConstants.NavigationReason.movingBack)
                return StorePageData();

            return true;
        }

        public bool QueryFinishButtonEnablement()
        {
            HostingWizard[PublishContainerToAWSWizardProperties.DeploymentMode] = _pageUI.DeploymentOption.Mode;

            // don't stand in the way of our previous sibling pages!
            return IsForwardsNavigationAllowed;
        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return true;
        }

        public void TestForwardTransitionEnablement()
        {
            if(this._pageUI.DeploymentOption?.Mode == Constants.DeployMode.PushOnly)
                HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, false);
            else
                HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, IsForwardsNavigationAllowed);

            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, QueryFinishButtonEnablement());
        }

        public bool IsForwardsNavigationAllowed
        {
            get
            {
                if (_pageUI == null)
                    return false;

                return _pageUI.AllRequiredFieldsAreSet;
            }
        }

        AccountViewModel _lastSavedAccount;
        RegionEndPointsManager.RegionEndPoints _lastSavedRegion;

        bool StorePageData()
        {
            if (_pageUI == null)
                return false;

            bool resetForwardPages = false;
            if (_lastSavedAccount == null)
            {
                _lastSavedAccount = _pageUI.SelectedAccount;
                _lastSavedRegion = _pageUI.SelectedRegion;
            }
            else
            {
                if (_lastSavedAccount != _pageUI.SelectedAccount || _lastSavedRegion != _pageUI.SelectedRegion)
                    resetForwardPages = true;
            }

            HostingWizard[PublishContainerToAWSWizardProperties.UserAccount] = _pageUI.SelectedAccount;
            HostingWizard[PublishContainerToAWSWizardProperties.Region] = _pageUI.SelectedRegion;

            HostingWizard[PublishContainerToAWSWizardProperties.Configuration] = _pageUI.Configuration;
            HostingWizard[PublishContainerToAWSWizardProperties.DockerRepository] = _pageUI.DockerRepository;
            HostingWizard[PublishContainerToAWSWizardProperties.DockerTag] = _pageUI.DockerTag;

            HostingWizard[PublishContainerToAWSWizardProperties.DeploymentMode] = _pageUI.DeploymentOption.Mode;

            HostingWizard[PublishContainerToAWSWizardProperties.PersistSettingsToConfigFile] = _pageUI.PersistSettingsToConfigFile;

            if (resetForwardPages)
            {
                this.HostingWizard.NotifyForwardPagesReset(this);
            }

            return true;
        }

        private void _pageUI_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            TestForwardTransitionEnablement();
        }
    }
}
