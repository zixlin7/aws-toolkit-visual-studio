using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.SNS.Model
{
    public class EditDisplayNameModel : BaseModel
    {
        string _topicARN;
        string _displayName;

        public EditDisplayNameModel()
        {
        }

        public EditDisplayNameModel(string topicARN, string displayName)
        {
            this._topicARN = topicARN;
            this._displayName = displayName;
        }

        public string TopicARN
        {
            get { return this._topicARN; }
            set
            {
                this._topicARN = value;
                base.NotifyPropertyChanged("TopicARN");
            }
        }

        public string DisplayName
        {
            get { return this._displayName; }
            set
            {
                this._displayName = value;
                base.NotifyPropertyChanged("DisplayName");
            }
        }


    }
}
