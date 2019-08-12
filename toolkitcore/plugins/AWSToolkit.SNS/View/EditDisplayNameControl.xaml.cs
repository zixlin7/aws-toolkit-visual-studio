using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.SNS.Controller;

namespace Amazon.AWSToolkit.SNS.View
{
    /// <summary>
    /// Interaction logic for EditDisplayNameControl.xaml
    /// </summary>
    public partial class EditDisplayNameControl : BaseAWSControl
    {
        EditDisplayNameController _controller;


        public EditDisplayNameControl(EditDisplayNameController controller)
        {
            this._controller = controller;
            this.DataContext = this._controller.Model;
            InitializeComponent();
        }


        public override string Title => "Edit Display Name";

        public override bool OnCommit()
        {
            if (string.IsNullOrEmpty(this._controller.Model.DisplayName))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Display name is required!");
                return false;
            }

            this._controller.Persist();
            return true;
        }    

    }
}
