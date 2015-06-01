using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Win32;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.ElasticBeanstalk.Model;
using Amazon.AWSToolkit.ElasticBeanstalk.Controller;

namespace Amazon.AWSToolkit.ElasticBeanstalk.View
{
    /// <summary>
    /// Interaction logic for ApplicationStatusControl.xaml
    /// </summary>
    public partial class ApplicationStatusControl : BaseAWSControl
    {
        ApplicationStatusController _controller;
        public ApplicationStatusControl(ApplicationStatusController controller)
        {
            this._controller = controller;
            InitializeComponent();

            this._ctlEvents.Initialize(this._controller);
            this._ctlApplicationVersions.Initialize(this._controller);
        }

        public override bool SupportsBackGroundDataLoad
        {
            get { return true; }
        }

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }

        public override string Title
        {
            get { return "App: " + this._controller.Model.ApplicationName; }
        }

        public override string UniqueId
        {
            get { return "AppStatus" + this._controller.Model.ApplicationName; }
        }

        void onRefreshClick(object sender, RoutedEventArgs e)
        {
            this._controller.Refresh();
        }
    }
}
