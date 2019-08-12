using System.Collections.ObjectModel;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.DynamoDB.Util;

namespace Amazon.AWSToolkit.DynamoDB.Model
{
    public class StartLocalDynamoDBModel : BaseModel
    {

        int _port = DynamoDBLocalManager.Instance.LastConfiguredPort;
        public int Port
        {
            get => this._port;
            set
            {
                this._port = value;
                base.NotifyPropertyChanged("Port");
                base.NotifyPropertyChanged("ExampleConnectCode");
            }
        }

        string _javaPath;
        public string JavaPath
        {
            get => this._javaPath;
            set
            {
                this._javaPath = value;
                base.NotifyPropertyChanged("JavaPath");
            }
        }

        bool _startNew = true;
        public bool StartNew
        {
            get => this._startNew;
            set
            {
                this._startNew = value;
                base.NotifyPropertyChanged("StartNew");
            }
        }

        ObservableCollection<DynamoDBLocalManager.DynamoDBLocalVersion> _versions;
        public ObservableCollection<DynamoDBLocalManager.DynamoDBLocalVersion> Versions
        {
            get => this._versions;
            set
            {
                this._versions = value;
                base.NotifyPropertyChanged("Versions");
            }
        }

        DynamoDBLocalManager.DynamoDBLocalVersion _selectedVersion;
        public DynamoDBLocalManager.DynamoDBLocalVersion SelectedVersion
        {
            get => this._selectedVersion;
            set
            {
                this._selectedVersion = value;
                base.NotifyPropertyChanged("SelectedVersion");
                CheckInstallState();
            }
        }

        public bool IsInstallPossible
        {
            get
            {
                if (this.SelectedVersion == null || this.SelectedVersion.IsInstalled)
                    return false;

                return true;
            }
        }

        public bool IsUninstallPossible
        {
            get
            {
                if (this.SelectedVersion == null || !this.SelectedVersion.IsInstalled)
                    return false;

                return true;
            }
        }

        public void CheckInstallState()
        {
            base.NotifyPropertyChanged("IsInstallPossible");
            base.NotifyPropertyChanged("IsUninstallPossible");
        }

        public string ExampleConnectCode
        {
            get
            {
                var code = 
                    "var config = new AmazonDynamoDBConfig\r\n" +
                    "{\r\n" +
                    "   ServiceURL = \"http://localhost:" + this.Port + "/\"\r\n" +
                    "}\r\n" +
                    "\r\n" +
                    "// Access key and secret key are not required\r\n" +
                    "// when connecting to DynamoDB Local and\r\n" +
                    "// are left empty in this sample.\r\n" +
                    "var client = new AmazonDynamoDBClient(\"\", \"\", config);\r\n";

                return code;
            }
        }
    }
}

