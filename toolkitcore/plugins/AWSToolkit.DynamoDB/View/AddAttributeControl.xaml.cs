using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.DynamoDB.Controller;

namespace Amazon.AWSToolkit.DynamoDB.View
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

        public override string Title => "Add Attribute";

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
