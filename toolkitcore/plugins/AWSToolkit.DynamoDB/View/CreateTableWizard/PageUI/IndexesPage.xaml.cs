using System.Windows;
using Amazon.AWSToolkit.DynamoDB.View.CreateTableWizard.PageControllers;

namespace Amazon.AWSToolkit.DynamoDB.View.CreateTableWizard.PageUI
{
    /// <summary>
    /// Interaction logic for IndexesPage.xaml
    /// </summary>
    public partial class IndexesPage
    {
        IndexesPageController _controller;
        public IndexesPage(IndexesPageController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = this._controller.DataContext;

            this._ctlLocalIndexes.Mode = Components.TableIndexesControl.EditingMode.LocalNew;
            this._ctlGlobalIndexes.Mode = Components.TableIndexesControl.EditingMode.GlobalNew;

            OnEnableSecondaryIndexesChecked(this, null);
            OnEnableGlobalSecondaryIndexesChecked(this, null);
        }

        private void OnEnableSecondaryIndexesChecked(object sender, RoutedEventArgs e)
        {
            this._ctlLocalIndexes.IsEnabled = _chkEnableLocalSecondaryIndexes.IsChecked.Value;

            this._controller.TestForwardTransitionEnablement();
        }

        private void OnEnableGlobalSecondaryIndexesChecked(object sender, RoutedEventArgs e)
        {
            this._ctlGlobalIndexes.IsEnabled = _chkEnableGlobalSecondaryIndexes.IsChecked.Value;

            this._controller.TestForwardTransitionEnablement();
        }

        private void _ctlIndexes_IndexChanged(object sender, RoutedEventArgs e)
        {
            this._controller.TestForwardTransitionEnablement();
        } 
    }
}
