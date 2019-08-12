using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.RDS.Model
{
    public class DeleteDBInstanceModel : BaseModel
    {

        public DeleteDBInstanceModel(string dbIdentifier)
        {
            this.DBIdentifier = dbIdentifier;
        }

        public string DBIdentifier
        {
            get;
        }

        bool _createFinalSnapshot = true;
        public bool CreateFinalSnapshot
        {
            get => this._createFinalSnapshot;
            set
            {
                this._createFinalSnapshot = value;
                base.NotifyPropertyChanged("CreateFinalSnapshot");
            }
        }

        string _finalSnapshotName;
        public string FinalSnapshotName
        {
            get => this._finalSnapshotName;
            set
            {
                this._finalSnapshotName = value;
                base.NotifyPropertyChanged("FinalSnapshotName");
            }
        }
    }
}
