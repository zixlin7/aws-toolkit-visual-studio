using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

using Amazon.AWSToolkit.DynamoDB.Model;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

namespace Amazon.AWSToolkit.DynamoDB.View.CreateTableWizard.PageControllers
{
    public abstract class BasePageController : IAWSWizardPageController
    {
        public string PageID
        {
            get { return GetType().FullName; }
        }

        public abstract UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason);

        public abstract string PageGroup { get; }

        public abstract string PageTitle { get; }

        public abstract string ShortPageTitle { get; }

        public abstract string PageDescription { get; }

        public IAWSWizard HostingWizard { get; set; }

        public virtual void ResetPage()
        {

        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return true;
        }

        public bool QueryFinishButtonEnablement()
        {
            return IsForwardsNavigationAllowed;
        }

        protected abstract bool IsForwardsNavigationAllowed
        {
            get;
        }


        public void TestForwardTransitionEnablement()
        {
            bool fwdsOK = IsForwardsNavigationAllowed;
            bool isLast = this.HostingWizard.GetProperty(Amazon.AWSToolkit.DynamoDB.Controller.CreateTableController.LAST_CONTROLLER) == this;
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, !isLast && fwdsOK);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, fwdsOK);
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            return true;
        }

        public virtual void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            // can get here direct from quick launch page if a seed ami was set, so do the same
            // as the ami selector page and turn on the wizard buttons that quick launch turned off
            HostingWizard.SetNavigationButtonVisibility(AWSWizardConstants.NavigationButtons.Back, true);
            // may have gotten here direct from quick launch, so make sure button text is correct
            HostingWizard.SetNavigationButtonText(AWSWizardConstants.NavigationButtons.Forward, "Next");
            TestForwardTransitionEnablement();
        }

        public virtual bool AllowShortCircuit()
        {
            // user may have gone forwards enough for Finish to be enabled, then come back
            // and changed something so re-save
            return true;
        }

        public CreateTableModel DataContext
        {
            get { return HostingWizard[Amazon.AWSToolkit.DynamoDB.Controller.CreateTableController.WIZARD_SEED_DATACONTEXT] as CreateTableModel; }
        }

    }
}
