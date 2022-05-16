using System.ComponentModel;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.SNS.Model
{
    public class CreateTopicModel : BaseModel, IDataErrorInfo
    {
        string _topicName;

        public CreateTopicModel()
        {
        }

        public string TopicName
        {
            get => this._topicName;
            set
            {
                this._topicName = value;
                this.NotifyPropertyChanged(nameof(TopicName));
            }
        }

        public IDataErrorInfo AsIDataErrorInfo => this;

        string IDataErrorInfo.this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(TopicName):
                        if (string.IsNullOrWhiteSpace(TopicName))
                        {
                            return "Topic Name cannot be blank";
                        }

                        if (TopicName.Length > SnsValidation.MaxTopicNameLength)
                        {
                            return $"Topic Name cannot be longer than {SnsValidation.MaxTopicNameLength}";
                        }

                        if (SnsValidation.InvalidTopicNameRegex.IsMatch(TopicName))
                        {
                            return
                                "Topic Name only supports alphanumeric characters, hyphens (-), and underscores (_).";
                        }

                        break;
                }

                return null;
            }
        }

        string IDataErrorInfo.Error => AsIDataErrorInfo[nameof(TopicName)];
    }
}
