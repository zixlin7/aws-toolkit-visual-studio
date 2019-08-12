using System;
using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Controller;
using log4net;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for AttachVolumeControl.xaml
    /// </summary>
    public partial class AttachVolumeControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(AttachVolumeControl));

        AttachVolumeController _controller;

        public AttachVolumeControl(AttachVolumeController controller)
        {
            InitializeComponent();
            _controller = controller;
        }

        public override string Title => "Attach Volume";

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            _controller.LoadModel();
            return _controller.Model;
        }

        public override bool Validated()
        {
            if (null == _controller.Model.Instance)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Required Field", "Please select an instance.");
                return false;
            }

            if (String.IsNullOrEmpty(_controller.Model.Device))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Required Field", "Please Select a device.");
                return false;
            }
            
            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                _controller.AttachVolume();
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error attaching volume", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error attaching volume: " + e.Message);
                return false;
            }
        }

        void onLoad(object sender, RoutedEventArgs evt)
        {
        }
    }
}
