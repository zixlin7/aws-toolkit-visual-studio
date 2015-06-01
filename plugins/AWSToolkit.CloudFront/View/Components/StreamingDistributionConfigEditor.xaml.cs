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

using Amazon.AWSToolkit.CloudFront.Controller;

namespace Amazon.AWSToolkit.CloudFront.View.Components
{
    /// <summary>
    /// Interaction logic for StreamingDistributionConfigEditor.xaml
    /// </summary>
    public partial class StreamingDistributionConfigEditor
    {
        BaseDistributionConfigEditorController _controller;
        public StreamingDistributionConfigEditor()
        {
            InitializeComponent();
        }

        public void Initialize(BaseDistributionConfigEditorController controller)
        {
            this._controller = controller;
            this._ctlLogging.Initialize(controller);
            this._ctlTrustedSigners.Initialize(controller);
        }
    }
}
