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
using Amazon.AWSToolkit.ECS.Model;
using Amazon.AWSToolkit.ECS.Controller;

namespace Amazon.AWSToolkit.ECS.View
{
    /// <summary>
    /// Interaction logic for CreateRepositoryControl.xaml
    /// </summary>
    public partial class CreateRepositoryControl : BaseAWSControl
    {
        CreateRepositoryController _controller;

        public CreateRepositoryControl()
            : this(null)
        {
        }

        public CreateRepositoryControl(CreateRepositoryController controller)
        {
            this._controller = controller;
            this.DataContext = this._controller.Model;
            InitializeComponent();
        }

        public CreateRepositoryModel Model
        {
            get
            {
                return this._controller.Model;
            }
        }

        public override string Title
        {
            get
            {
                return "Create Repository";
            }
        }

        public override bool OnCommit()
        {
            string newName = this._controller.Model.RepositoryName == null ? string.Empty : this._controller.Model.RepositoryName;
            if (newName.Trim().Equals(string.Empty))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Name is required.");
                return false;
            }

            return this._controller.Persist();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this._ctlNewRepositoryName.Focus();
        }
    }
}
