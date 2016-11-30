using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

using Amazon.AWSToolkit.SimpleDB.Controller;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.SimpleDB.View
{
    /// <summary>
    /// Interaction logic for AddAttributeControl.xaml
    /// </summary>
    public partial class AddAttributeControl : BaseAWSControl
    {
        AddAttributeController _controller;

        public AddAttributeControl(AddAttributeController controller)
        {
            this._controller = controller;
            this.DataContext = this._controller.Model;
            InitializeComponent();
        }

        public override string Title
        {
            get { return "Add Attribute"; }
        }

        public override bool Validated()
        {
            if(string.IsNullOrEmpty(this._controller.Model.AttributeName))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Attribute name is required.");
                return false;
            }

            return true;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this._ctlAttributeName.Focus();
        }
    }
}
