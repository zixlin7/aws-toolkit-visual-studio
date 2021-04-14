using System.Collections.ObjectModel;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.S3.Model
{
    public class InvokeLambdaFunctionModel : BaseModel
    {
        public InvokeLambdaFunctionModel()
        {
            this.Regions = new ObservableCollection<ToolkitRegion>();
            this.Functions = new ObservableCollection<string>();
            this.EventTypes = new ObservableCollection<string>();
        }

        public ToolkitRegion SelectedRegion { get; set; }
        public ObservableCollection<ToolkitRegion> Regions { get; set; }

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
