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

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.SimpleDB.Model;
using Amazon.AWSToolkit.SimpleDB.Controller;

namespace Amazon.AWSToolkit.SimpleDB.View
{
    /// <summary>
    /// Interaction logic for CreateDomainControl.xaml
    /// </summary>
    public partial class CreateDomainControl : BaseAWSControl
    {
        CreateDomainController _controller;

        public CreateDomainControl(CreateDomainController controller)
        {
            this._controller = controller;
            this.DataContext = this._controller.Model;
            InitializeComponent();
        }

        public override string Title
        {
            get
            {
                return "Create Domain";
            }
        }

        public override bool OnCommit()
        {
            string newName = this._controller.Model.DomainName == null ? string.Empty : this._controller.Model.DomainName;
            if (newName.Trim().Equals(string.Empty))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Domain name is required.");
                return false;
            }

            this._controller.Persist();
            return true;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this._ctlDomainName.Focus();
        }
    }
}
