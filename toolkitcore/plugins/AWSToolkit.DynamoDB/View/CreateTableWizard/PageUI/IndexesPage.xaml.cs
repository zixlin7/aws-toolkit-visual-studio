using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Amazon.AWSToolkit.CommonUI;

using Amazon.AWSToolkit.DynamoDB.Model;
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
