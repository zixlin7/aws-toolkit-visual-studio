using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

namespace Amazon.AWSToolkit.S3.View
{
    /// <summary>
    /// Interaction logic for NewUploadSettingsControl.xaml
    /// </summary>
    public partial class NewUploadSettingsControl : BaseAWSControl
    {
        NewUploadSettingsController _controller;

        public NewUploadSettingsControl(NewUploadSettingsController controller)
        {
            this._controller = controller;
            this.DataContext = this._controller.Model;
            InitializeComponent();
        }

        public override string Title
        {
            get { return "Upload Settings"; }
        }
    }
}
