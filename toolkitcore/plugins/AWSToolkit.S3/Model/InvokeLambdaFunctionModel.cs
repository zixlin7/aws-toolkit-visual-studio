using System.Collections.ObjectModel;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.S3.Model
{
    public class InvokeLambdaFunctionModel : BaseModel
    {
        public InvokeLambdaFunctionModel()
        {
            this.Regions = new ObservableCollection<RegionEndPointsManager.RegionEndPoints>();
            this.Functions = new ObservableCollection<string>();
            this.EventTypes = new ObservableCollection<string>();
        }

        public RegionEndPointsManager.RegionEndPoints SelectedRegion { get; set; }
        public ObservableCollection<RegionEndPointsManager.RegionEndPoints> Regions { get; set; }

        private string _selectedFunction;
        public string SelectedFunction { 
            get => this._selectedFunction;
            set
            {
                this._selectedFunction = value;
                base.NotifyPropertyChanged("SelectedFunction");
            }
        }
        public ObservableCollection<string> Functions { get; set; }

        public string SelectedEventType { get; set; }
        public ObservableCollection<string> EventTypes { get; set; }

        public bool GetLatestProperties { get; set; }
        public bool GroupInvokes { get; set; }
    }
}
