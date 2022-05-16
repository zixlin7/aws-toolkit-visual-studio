using System.Text;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.Publish.ViewModels
{
    public class DeploymentMessageGroup : BaseModel
    {
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        public string Message => _messageBuilder.ToString();

        private string _name;
        private string _description;
        private bool _isExpanded = true;
        private readonly StringBuilder _messageBuilder = new StringBuilder();

        public void AppendLine(string text)
        {
            _messageBuilder.AppendLine(text);
            NotifyPropertyChanged(nameof(Message));
        }
    }
}
