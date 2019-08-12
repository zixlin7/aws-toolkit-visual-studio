using System.Windows;
using Amazon.AWSToolkit.CommonUI;
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

        public override string Title => "Create Domain";

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
