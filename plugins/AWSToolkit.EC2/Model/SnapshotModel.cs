using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class SnapshotModel
    {
        public const string NO_SNAPSHOT_ID = "--- No Snapshot ---";

        string _snapshotId;
        string _description;
        string _size;
        string _name;

        public SnapshotModel(string snapshotId, string description, string size, string name)
        {
            _snapshotId = snapshotId;
            _description = description;
            _size = size;
            _name = name;
        }

        public SnapshotModel(string snapshotId, string description, string size) : this(snapshotId, description, size, String.Empty) { }

        public string SnapshotId
        {
            get { return _snapshotId; }
        }

        public string Description
        {
            get { return _description; }
        }

        public string Size
        {
            get { return _size; }
        }

        public string Name
        {
            get { return _name; }
        }

        public string DisplayString
        {
            get
            {
                if (null == _snapshotId)
                    return Description;

                return String.Format("{0} - {1}", _snapshotId, 
                    _description.Length > 30 ? _description.Substring(0, 30) : _description);
            }
        }
    }
}
