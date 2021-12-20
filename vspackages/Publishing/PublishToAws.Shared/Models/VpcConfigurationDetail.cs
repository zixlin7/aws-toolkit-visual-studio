using System.Diagnostics;
using System.Windows.Input;

namespace Amazon.AWSToolkit.Publish.Models
{
    public enum VpcOption
    {
        Default,
        New,
        Existing,
    };

    [DebuggerDisplay("VPC: {Id} | {VpcOption}")]
    public class VpcConfigurationDetail : ConfigurationDetail
    {
        public static class ChildDetailIds
        {
            public const string VpcId = "VpcId";
            public const string IsDefault = "IsDefault";
            public const string CreateNew = "CreateNew";
        }

        private ConfigurationDetail _useDefaultVpc;
        private ConfigurationDetail _createNewRole;
        private ConfigurationDetail _vpcId;

        private VpcOption _vpcOption;
        private ICommand _selectVpc;

        public ICommand SelectVpc
        {
            get => _selectVpc;
            set => SetProperty(ref _selectVpc, value);
        }

        public ConfigurationDetail VpcIdDetail
        {
            get => _vpcId;
            private set => SetProperty(ref _vpcId, value);
        }

        public VpcOption VpcOption
        {
            get => _vpcOption;
            set
            {
                if (_vpcOption == value)
                {
                    return;
                }

                _vpcOption = value;

                SuspendDetailChangeEvents(() =>
                {
                    ApplyToDetails(value);
                });

                NotifyPropertyChanged(nameof(VpcOption));
                RaiseConfigurationDetailChanged(this);
            }
        }

        private void ApplyToDetails(VpcOption value)
        {
            switch (value)
            {
                case VpcOption.Default:
                    ApplyDefaultVpcToDetails();
                    break;
                case VpcOption.New:
                    ApplyNewVpcToDetails();
                    break;
                case VpcOption.Existing:
                    ApplyExistingVpcToDetails();
                    break;
            }
        }

        private void ApplyDefaultVpcToDetails()
        {
            _useDefaultVpc.Value = true;
            _createNewRole.Value = false;
            ClearInvalidVpc();
        }

        private void ApplyNewVpcToDetails()
        {
            _createNewRole.Value = true;
            _useDefaultVpc.Value = false;
            ClearInvalidVpc();
        }

        private void ApplyExistingVpcToDetails()
        {
            _createNewRole.Value = false;
            _useDefaultVpc.Value = false;
        }

        private void ClearInvalidVpc()
        {
            if (!string.IsNullOrWhiteSpace(_vpcId.ValidationMessage))
            {
                _vpcId.Value = string.Empty;
            }
        }

        public override void AddChild(ConfigurationDetail child)
        {
            base.AddChild(child);

            switch (child.Id)
            {
                case ChildDetailIds.VpcId:
                    _vpcId = child;
                    break;
                case ChildDetailIds.IsDefault:
                    _useDefaultVpc = child;
                    break;
                case ChildDetailIds.CreateNew:
                    _createNewRole = child;
                    break;
            }

            ApplyDetailsToVpcOption();
        }

        protected override void RemoveChild(ConfigurationDetail child)
        {
            switch (child.Id)
            {
                case ChildDetailIds.VpcId:
                    _vpcId = null;
                    break;
                case ChildDetailIds.IsDefault:
                    _useDefaultVpc = null;
                    break;
                case ChildDetailIds.CreateNew:
                    _createNewRole = null;
                    break;
            }

            base.RemoveChild(child);
        }

        private void ApplyDetailsToVpcOption()
        {
            if (_vpcId == null || _useDefaultVpc == null || _createNewRole == null)
            {
                return;
            }

            _vpcOption = _useDefaultVpc.Value.Equals(true) ? VpcOption.Default :
                _createNewRole.Value.Equals(true) ? VpcOption.New : VpcOption.Existing;

            NotifyPropertyChanged(nameof(VpcOption));
        }
    }
}
