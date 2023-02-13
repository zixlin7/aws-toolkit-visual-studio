using System.Windows;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Model;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for AttachElasticIPToInstance.xaml
    /// </summary>
    public partial class AttachElasticIPToInstanceControl : BaseAWSControl
    {
        private readonly AttachElasticIPToInstanceModel _model;

        public AttachElasticIPToInstanceControl(AttachElasticIPToInstanceModel model)
        {
            _model = model;
            InitializeComponent();
            DataContext = _model;
        }

        public override string Title => "Attach Elastic IP to Instance";

        void onLoad(object sender, RoutedEventArgs e)
        {
            if (this._ctlElasticIPs.Items.Count == 0)
            {
                this._ctlSelectExisting.IsEnabled = false;
                _model.ActionCreateNewAddress = true;
            }
            else
            {
                this._ctlElasticIPs.SelectedIndex = 0;
                _model.ActionSelectedAddress = true;
            }
        }
    }
}
