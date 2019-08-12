using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.S3.Model
{
    public class RestoreObjectPromptModel : BaseModel
    {
        string _restoreDays;

        public RestoreObjectPromptModel()
        {
        }

        public string RestoreDays
        {
            get => this._restoreDays;
            set
            {
                this._restoreDays = value;
                this.NotifyPropertyChanged("RestoreDays");
            }
        }
    }
}
