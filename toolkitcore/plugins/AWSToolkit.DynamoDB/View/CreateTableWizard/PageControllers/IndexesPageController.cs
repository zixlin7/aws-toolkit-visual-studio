using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

using Amazon.AWSToolkit.DynamoDB.View.CreateTableWizard.PageUI;

namespace Amazon.AWSToolkit.DynamoDB.View.CreateTableWizard.PageControllers
{
    public class IndexesPageController : BasePageController
    {
        IndexesPage _pageUI;

        public override string PageGroup => AWSWizardConstants.DefaultPageGroup;

        public override string PageTitle => "Secondary Indexes";

        public override string ShortPageTitle => null;

        public override string PageDescription => "Define local and global secondary indexes for the table.";

        public override UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new IndexesPage(this);
            }

            return _pageUI;
        }


        protected override bool IsForwardsNavigationAllowed
        {
            get
            {
                if (this.DataContext.UseLocalSecondaryIndexes)
                {
                    foreach (var index in this.DataContext.LocalSecondaryIndexes)
                    {
                        if (string.IsNullOrWhiteSpace(index.Name))
                            return false;

                        if (index.RangeKey == null || string.IsNullOrWhiteSpace(index.RangeKey.Name))
                            return false;
                    }
                }

                if (this.DataContext.UseGlobalSecondaryIndexes)
                {
                    foreach (var index in this.DataContext.GlobalSecondaryIndexes)
                    {
                        if (string.IsNullOrWhiteSpace(index.Name))
                            return false;

                        if (index.HashKey == null || string.IsNullOrWhiteSpace(index.HashKey.Name))
                            return false;

                        if (index.ReadCapacity < 1 || index.WriteCapacity < 1)
                            return false;
                    }
                }

                return true;
            }
        }
    }
}
