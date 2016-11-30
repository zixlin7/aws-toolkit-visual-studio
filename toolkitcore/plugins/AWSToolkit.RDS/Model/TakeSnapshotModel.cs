using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            private set;
        }

        string _snapshotName;
        public string SnapshotName
        {
            get { return this._snapshotName; }
            set
            {
                this._snapshotName = value;
                base.NotifyPropertyChanged("SnapshotName");
            }
        }
    }
}
