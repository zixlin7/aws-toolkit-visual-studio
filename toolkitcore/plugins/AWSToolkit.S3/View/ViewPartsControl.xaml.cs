using System;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.S3.Controller;

namespace Amazon.AWSToolkit.S3.View
{
    /// <summary>
    /// Interaction logic for ViewPartsControl.xaml
    /// </summary>
    public partial class ViewPartsControl : BaseAWSControl
    {
        ViewPartsController _controller;

        public ViewPartsControl(ViewPartsController controller)
        {
            this._controller = controller;
            InitializeComponent();
        }

        public override string Title => "Parts";

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
            {
                this._ctlHeaderLabel.Text = string.Format("{0} part(s) successfully uploaded.", this._controller.Model.PartDetails.Count);
            }));

            return this._controller.Model;
        }
    }
}
