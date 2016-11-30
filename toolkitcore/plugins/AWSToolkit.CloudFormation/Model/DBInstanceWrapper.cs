using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;

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

        public DBInstance NativeDBInstance
        {
            get { return this._dbInstance; }
        }

        public string FormattedAllocatedStorage
        {
            get { return this._dbInstance.AllocatedStorage + " GiB"; }
        }
    }
}
