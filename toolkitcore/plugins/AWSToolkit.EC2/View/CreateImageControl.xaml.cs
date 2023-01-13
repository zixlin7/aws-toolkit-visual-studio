using System.Windows;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Model;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for CreateImageControl.xaml
    /// </summary>
    public partial class CreateImageControl : BaseAWSControl
    {
        private readonly CreateImageModel _model;

        public CreateImageControl(CreateImageModel model)
        {
            _model = model;
            InitializeComponent();
            DataContext = _model;
        }

        public override string Title => "Create Image";

        public override bool Validated()
        {
            if (string.IsNullOrEmpty(_model.Name))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Required Field", "Name is required.");
                return false;
            }
            return true;
        }

        void onLoad(object sender, RoutedEventArgs e)
        {
            this._ctlName.Focus();
        }
    }
}
