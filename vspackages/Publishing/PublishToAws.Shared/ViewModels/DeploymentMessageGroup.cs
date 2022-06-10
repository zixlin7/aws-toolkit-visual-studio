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

        public string Message
        {
            get
            {
                lock (_builderSync)
                {
                    return _messageBuilder.ToString();
                }
            }
        }

        private string _name;
        private string _description;
        private bool _isExpanded = true;
        private readonly StringBuilder _messageBuilder = new StringBuilder();
        private volatile bool _messageNeedsRefresh = false;
        private readonly object _refreshSync = new object();
        private readonly object _builderSync = new object();

        /// <summary>
        /// For performance reasons, we don't notify property changes for Message with each update.
        /// Message only raises an event if needed when <see cref="RefreshMessage"/> is called.
        /// </summary>
        public void AppendLine(string text)
        {
            lock (_builderSync)
            {
                _messageBuilder.AppendLine(text);
            }

            lock (_refreshSync)
            {
                _messageNeedsRefresh = true;
            }
        }

        public void RefreshMessage()
        {
            if (_messageNeedsRefresh)
            {
                lock (_refreshSync)
                {
                    NotifyPropertyChanged(nameof(Message));
                    _messageNeedsRefresh = false;
                }
            }
        }
    }
}
