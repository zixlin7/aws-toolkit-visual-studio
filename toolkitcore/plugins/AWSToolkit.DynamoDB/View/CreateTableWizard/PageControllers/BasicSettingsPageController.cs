using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

using Amazon.AWSToolkit.DynamoDB.View.CreateTableWizard.PageUI;

namespace Amazon.AWSToolkit.DynamoDB.View.CreateTableWizard.PageControllers
{
    public class BasicSettingsPageController : BasePageController
    {
        BasicSettingsPage _pageUI;

        public override string PageGroup => AWSWizardConstants.DefaultPageGroup;

        public override string PageTitle => "Basic Settings";

        public override string ShortPageTitle => null;

        public override string PageDescription => "These are the required settings for creating a Table.";

        public override UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new BasicSettingsPage(this);
            }

            return _pageUI;
        }


        protected override bool IsForwardsNavigationAllowed
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.DataContext.TableName))
                    return false;

                if (string.IsNullOrWhiteSpace(this.DataContext.HashKeyName))
                    return false;

                if (this.DataContext.UseRangeKey && string.IsNullOrWhiteSpace(this.DataContext.RangeKeyName))
                    return false;

                int value;
                if (!int.TryParse(this.DataContext.ReadCapacityUnits, out value) || value < 1)
                    return false;

                if (!int.TryParse(this.DataContext.WriteCapacityUnits, out value) || value < 1)
                    return false;

                return true;
            }
        }
    }
}
