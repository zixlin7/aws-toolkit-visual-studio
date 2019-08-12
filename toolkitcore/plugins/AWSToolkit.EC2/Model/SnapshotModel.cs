using System;

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

        public string SnapshotId => _snapshotId;

        public string Description => _description;

        public string Size => _size;

        public string Name => _name;

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
