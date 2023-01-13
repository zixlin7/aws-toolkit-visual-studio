using System.Windows;
using System.Windows.Data;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Model;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for ChangeInstanceTypeControl.xaml
    /// </summary>
    public partial class ChangeInstanceTypeControl : BaseAWSControl
    {
        private readonly ChangeInstanceTypeModel _model;

        public ChangeInstanceTypeControl(ChangeInstanceTypeModel model)
        {
            _model = model;
            DataContext = _model;
            InitializeComponent();

            // switch on grouping for the instance type and vpc subnet dropdowns
            var instanceTypesView = (CollectionView)CollectionViewSource.GetDefaultView(_ctlInstanceTypes.ItemsSource);
            var familyGroupDescription = new PropertyGroupDescription("HardwareFamily");
            instanceTypesView.GroupDescriptions.Add(familyGroupDescription);
        }

        public override string Title => "Change Instance Type";

        public override bool Validated()
        {
            if (_model.SelectedInstanceType == null)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Required Field", "Instance Type is required.");
                return false;
            }
            return true;
        }

        void onLoad(object sender, RoutedEventArgs e)
        {
            this._ctlInstanceTypes.Focus();
        }
    }
}
