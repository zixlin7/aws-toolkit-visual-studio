using System.Windows;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Model;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for ChangeUserDataControl.xaml
    /// </summary>
    public partial class ChangeUserDataControl : BaseAWSControl
    {
        private readonly ChangeUserDataModel _model;

        public ChangeUserDataControl(ChangeUserDataModel model)
        {
            _model = model;
            InitializeComponent();
            DataContext = _model;
            checkWarningMessage();
        }


        public override string Title => "User Data";

        void checkWarningMessage()
        {
            if (_model.IsReadOnly)
            {
                this._ctlWarning.Visibility = Visibility.Visible;
                this._ctlWarning.Height = double.NaN;
            }
            else
            {
                this._ctlWarning.Visibility = Visibility.Hidden;
                this._ctlWarning.Height = 0;
            }
        }
    }
}
