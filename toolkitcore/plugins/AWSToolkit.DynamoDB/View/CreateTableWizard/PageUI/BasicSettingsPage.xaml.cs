using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Amazon.AWSToolkit.DynamoDB.View.CreateTableWizard.PageControllers;

namespace Amazon.AWSToolkit.DynamoDB.View.CreateTableWizard.PageUI
{
    /// <summary>
    /// Interaction logic for BasicSettingsPage.xaml
    /// </summary>
    public partial class BasicSettingsPage
    {
        BasicSettingsPageController _controller;

        public BasicSettingsPage(BasicSettingsPageController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = this._controller.DataContext;            
        }

        private void onTextChanged(object sender, TextChangedEventArgs e)
        {
            this._controller.TestForwardTransitionEnablement();
        }

        private void onClick(object sender, RoutedEventArgs e)
        {
            this._controller.TestForwardTransitionEnablement();
        }
    }
}
