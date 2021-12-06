using System.Windows.Input;

namespace Amazon.AWSToolkit.Publish.Models
{
    public class IamRoleConfigurationDetail : ConfigurationDetail
    {
        private ConfigurationDetail _createNewRole;
        private ConfigurationDetail _roleArn;

        private ICommand _selectRoleArn;

        public ICommand SelectRoleArn
        {
            get => _selectRoleArn;
            set => SetProperty(ref _selectRoleArn, value);
        }

        public ConfigurationDetail RoleArnDetail
        {
            get => _roleArn;
            private set => SetProperty(ref _roleArn, value);
        }

        public bool CreateNewRole
        {
            get => (bool) (_createNewRole?.Value ?? true);
            set
            {
                if (_createNewRole != null && !_createNewRole.Value.Equals(value))
                {
                    _createNewRole.Value = value;
                    NotifyPropertyChanged(nameof(CreateNewRole));
                }
            }
        }

        public override void ClearChildren()
        {
            RoleArnDetail = null;
            _createNewRole = null;
            base.ClearChildren();
        }

        public override void AddChild(ConfigurationDetail child)
        {
            base.AddChild(child);

            if (child.Id == "RoleArn")
            {
                RoleArnDetail = child;
            }
            else if (child.Id == "CreateNew")
            {
                _createNewRole = child;
                NotifyPropertyChanged(nameof(CreateNewRole));
            }
        }
    }
}
