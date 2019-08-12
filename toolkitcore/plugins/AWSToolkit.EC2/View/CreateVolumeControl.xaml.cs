using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI.ResourceTags;
using Amazon.AWSToolkit.EC2.Controller;
using Amazon.AWSToolkit.EC2.Model;

using log4net;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for CreateVolumeControl.xaml
    /// </summary>
    public partial class CreateVolumeControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(CreateVolumeControl));

        readonly CreateVolumeController _controller;

        public CreateVolumeControl(CreateVolumeController controller)
        {
            InitializeComponent();
            this._controller = controller;
        }

        public override string Title => "Create Volume";

        public override bool SupportsBackGroundDataLoad => true;

        public ResourceTagsModel TagsModel => _controller != null ? this._controller.Model.TagsModel : null;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();

            _snapshotSelector.Dispatcher.BeginInvoke((Action)(() => 
            {
                _snapshotSelector.DataContext = _controller.Model;
                _snapshotSelector.ItemsSource = new ObservableCollection<SnapshotModel>(_controller.Model.AvailableSnapshots.OrderBy(x=>x.SnapshotId));
                _snapshotSelector.SelectedIndex = 0;
            }));
            _ctlVolumeType.Dispatcher.BeginInvoke((Action) (() =>
            {
                _ctlVolumeType.ItemsSource = _controller.Model.VolumeTypes;
                _ctlVolumeType.SelectedIndex = 0;
            }));

            return this._controller.Model;
        }

        public override bool Validated()
        {
            return true;
        }

        private void _snapCtl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _controller.Model.SnapshotId = ((SnapshotModel)_snapshotSelector.SelectedItem).SnapshotId;
            _controller.AdjustSizePerSnapshot();
        }

        public override bool OnCommit()
        {
            try
            {
                string volumeId = _controller.CreateVolume();
                ToolkitFactory.Instance.ShellProvider.ShowMessage("Volume Created", String.Format("Volume created with id {0}.", volumeId));
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error creating volume", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating volume: " + e.Message);
                return false;
            }
        }

        public void SetupIopsFields()
        {
            this._ctlIops.IsEnabled = _controller.Model.VolumeType.TypeCode.Equals(VolumeWrapper.ProvisionedIOPSTypeCode, StringComparison.OrdinalIgnoreCase);
        }

        public void SetupDeviceNameField()
        {
            if (this._controller.Model.InstanceToAttach == null)
            {
                this._ctlDeviceLabel.Visibility = Visibility.Hidden;
                this._ctlDevice.Visibility = Visibility.Hidden;
            }
            else
            {
                this._zoneCtl.IsEnabled = false;
                if (_controller.Model.InstanceToAttach.UnmappedDeviceSlots.Count > 0)
                    _controller.Model.Device = _controller.Model.InstanceToAttach.UnmappedDeviceSlots[0];
                this.Height += 30;
            }
        }

        private void _ctlVolumeType_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
                return;

            _controller.Model.VolumeType = e.AddedItems[0] as CreateVolumeModel.VolumeTypeOption;
            SetupIopsFields();
        }
    }

    public class IopsRangeValidationRule : ValidationRule
    {
        private int _minimumValue = 100;
        private int _maximumValue = 4000;
        private string _errorMessage;

        public int MinimumValue
        {
            get => _minimumValue;
            set => _minimumValue = value;
        }

        public int MaximumValue
        {
            get => _maximumValue;
            set => _maximumValue = value;
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => _errorMessage = value;
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var result = new ValidationResult(true, null);
            string inputString = (value ?? string.Empty).ToString();
            try
            {
                var iops = int.Parse(inputString);
                if (iops < MinimumValue || iops > MaximumValue)
                    throw new Exception();
            }
            catch (Exception)
            {
                result = new ValidationResult(false, string.Format("Valid range {0} to {1}", MinimumValue, MaximumValue));
            }

            return result;
        }
    }
}
