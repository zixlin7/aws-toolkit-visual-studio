using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using Amazon.CloudFront;
using Amazon.CloudFront.Model;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.CloudFront.Model
{
    public class ViewInvalidationRequestsModel : BaseModel
    {

        ObservableCollection<InvalidationSummaryWrapper> _summaries;
        public ObservableCollection<InvalidationSummaryWrapper> Summaries
        {
            get { return this._summaries; }
            set
            {
                this._summaries = value;
                base.NotifyPropertyChanged("Summaries");
            }
        }
    }
}
