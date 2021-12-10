using System.Windows.Input;

namespace Amazon.AWSToolkit.Publish.Models
{
    public class IamRoleConfigurationDetail : ConfigurationDetail
    {
        public static class ChildDetailIds
        {
            public const string CreateNew = "CreateNew";
            public const string RoleArn = "RoleArn";
        }

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

        protected override void RemoveChild(ConfigurationDetail child)
        {
            switch (child.Id)
            {
                case ChildDetailIds.RoleArn:
                    RoleArnDetail = null;
                    break;
                case ChildDetailIds.CreateNew:
                    _createNewRole = null;
                    break;
            }

            base.RemoveChild(child);
        }

        public override void AddChild(ConfigurationDetail child)
        {
            base.AddChild(child);

            switch (child.Id)
            {
                case ChildDetailIds.RoleArn:
                    RoleArnDetail = child;
                    break;
                case ChildDetailIds.CreateNew:
                    _createNewRole = child;
                    NotifyPropertyChanged(nameof(CreateNewRole));
                    break;
            }
        }
    }
}
