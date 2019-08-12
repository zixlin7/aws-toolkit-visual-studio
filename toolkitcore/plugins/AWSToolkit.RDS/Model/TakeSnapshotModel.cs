using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.RDS.Model
{
    public class TakeSnapshotModel : BaseModel
    {
        public TakeSnapshotModel(string dbIdentifier)
        {
            this.DBIdentifier = dbIdentifier;
        }

        public string DBIdentifier
        {
            get;
        }

        string _snapshotName;
        public string SnapshotName
        {
            get => this._snapshotName;
            set
            {
                this._snapshotName = value;
                base.NotifyPropertyChanged("SnapshotName");
            }
        }
    }
}
