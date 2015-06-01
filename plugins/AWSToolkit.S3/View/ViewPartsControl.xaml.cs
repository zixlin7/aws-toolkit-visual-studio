using System;
using System.Collections.Generic;
using System.Linq;
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
using Amazon.AWSToolkit.S3.Model;
using Amazon.AWSToolkit.S3.Controller;

using log4net;

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

        public override string Title
        {
            get { return "Parts"; }
        }

        public override bool SupportsBackGroundDataLoad
        {
            get { return true; }
        }

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() =>
            {
                this._ctlHeaderLabel.Text = string.Format("{0} part(s) successfully uploaded.", this._controller.Model.PartDetails.Count);
            }));

            return this._controller.Model;
        }
    }
}
