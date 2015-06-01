using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Threading;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

using Amazon.AWSToolkit.DynamoDB.View.CreateTableWizard.PageUI;

namespace Amazon.AWSToolkit.DynamoDB.View.CreateTableWizard.PageControllers
{
    public class NotificationPageController : BasePageController
    {
        NotificationPage _pageUI;

        public override string PageGroup
        {
            get { return AWSWizardConstants.DefaultPageGroup; }
        }

        public override string PageTitle
        {
            get { return "Notifications"; }
        }

        public override string ShortPageTitle
        {
            get { return null; }
        }

        public override string PageDescription
        {
            get { return "Setup basic alarms and notifications."; }
        }

        public override UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new NotificationPage(this);
            }

            return _pageUI;
        }

        protected override bool IsForwardsNavigationAllowed
        {
            get
            {
                return !this.DataContext.UseBasicAlarms || (this.DataContext.UseBasicAlarms && !string.IsNullOrWhiteSpace(this.DataContext.AlarmEmail));
            }
        }
    }
}
