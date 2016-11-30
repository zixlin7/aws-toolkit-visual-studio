using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.RDS.Model
{
    public class CreateSqlServerDBModel : BaseModel
    {
        string _dbInstance;
        public string DBInstance
        {
            get { return _dbInstance; }
            set
            {
                _dbInstance = value;
                base.NotifyPropertyChanged("DBInstance");
            }
        }

        string _userName;
        public string UserName
        {
            get { return _userName; }
            set
            {
                _userName = value;
                base.NotifyPropertyChanged("UserName");
            }
        }

        string _password;
        public string Password
        {
            get { return _password; }
            set
            {
                _password = value;
                base.NotifyPropertyChanged("Password");
            }
        }

        string _dbName;
        public string DBName
        {
            get { return _dbName; }
            set
            {
                _dbName = value;
                base.NotifyPropertyChanged("DBName");
            }
        }
    }
}
