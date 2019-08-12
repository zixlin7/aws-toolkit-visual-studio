using System.Collections.ObjectModel;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.CloudFront.Model
{
    public class ViewInvalidationRequestsModel : BaseModel
    {

        ObservableCollection<InvalidationSummaryWrapper> _summaries;
        public ObservableCollection<InvalidationSummaryWrapper> Summaries
        {
            get => this._summaries;
            set
            {
                this._summaries = value;
                base.NotifyPropertyChanged("Summaries");
            }
        }
    }
}
