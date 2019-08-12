using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

using Amazon.AWSToolkit.DynamoDB.View.CreateTableWizard.PageUI;

namespace Amazon.AWSToolkit.DynamoDB.View.CreateTableWizard.PageControllers
{
    public class NotificationPageController : BasePageController
    {
        NotificationPage _pageUI;

        public override string PageGroup => AWSWizardConstants.DefaultPageGroup;

        public override string PageTitle => "Notifications";

        public override string ShortPageTitle => null;

        public override string PageDescription => "Setup basic alarms and notifications.";

        public override UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new NotificationPage(this);
            }

            return _pageUI;
        }

        protected override bool IsForwardsNavigationAllowed => !this.DataContext.UseBasicAlarms || (this.DataContext.UseBasicAlarms && !string.IsNullOrWhiteSpace(this.DataContext.AlarmEmail));
    }
}
