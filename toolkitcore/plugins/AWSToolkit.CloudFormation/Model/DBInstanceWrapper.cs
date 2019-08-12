using Amazon.RDS.Model;

namespace Amazon.AWSToolkit.CloudFormation.Model
{
    public class DBInstanceWrapper
    {
        DBInstance _dbInstance;

        public DBInstanceWrapper(DBInstance dbInstance)
        {
            this._dbInstance = dbInstance;
        }

        public DBInstance NativeDBInstance => this._dbInstance;

        public string FormattedAllocatedStorage => this._dbInstance.AllocatedStorage + " GiB";
    }
}
