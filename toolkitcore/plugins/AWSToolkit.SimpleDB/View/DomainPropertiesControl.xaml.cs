using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.SimpleDB.Controller;

namespace Amazon.AWSToolkit.SimpleDB.View
{
    /// <summary>
    /// Interaction logic for DomainPropertiesControl.xaml
    /// </summary>
    public partial class DomainPropertiesControl : BaseAWSControl
    {
        DomainPropertiesController _controller;

        public DomainPropertiesControl(DomainPropertiesController controller)
        {
            this._controller = controller;
            InitializeComponent();
        }

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }

        public override string Title => string.Format("Properties: {0}", this._controller.Model.Domain);
    }
}
