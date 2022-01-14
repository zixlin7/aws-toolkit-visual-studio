using System.Diagnostics;
using System.Windows.Input;

namespace Amazon.AWSToolkit.Publish.Models.Configuration
{
    [DebuggerDisplay("EC2 Instance Type: {InstanceTypeId}")]
    public class Ec2InstanceConfigurationDetail : ConfigurationDetail
    {
        private ICommand _selectInstanceType;

        public ICommand SelectInstanceType
        {
            get => _selectInstanceType;
            set => SetProperty(ref _selectInstanceType, value);
        }

        public string InstanceTypeId
        {
            get => (string) Value;
            set
            {
                if (!Value?.Equals(value) ?? value != null)
                {
                    Value = value;
                    NotifyPropertyChanged(nameof(InstanceTypeId));
                }
            }
        }
    }
}
