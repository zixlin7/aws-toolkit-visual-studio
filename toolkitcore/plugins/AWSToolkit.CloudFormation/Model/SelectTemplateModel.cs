using System.Collections.Generic;
using System.IO;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.CloudFormation.Model
{
    public class SelectTemplateModel : BaseModel
    {
        string _stackName;
        public string StackName
        {
            get => this._stackName;
            set
            {
                this._stackName = value;
                base.NotifyPropertyChanged("StackName");
            }
        }

        bool _useLocalFile = true;
        public bool UseLocalFile
        {
            get => this._useLocalFile;
            set
            {
                this._useLocalFile = value;
                this._useSampleTemplate = !value;
                base.NotifyPropertyChanged("UseLocalFile");
                base.NotifyPropertyChanged("UseSampleTemplate");
            }
        }

        string _localFile;
        public string LocalFile
        {
            get => this._localFile;
            set
            {
                this._localFile = value;
                base.NotifyPropertyChanged("LocalFile");
            }
        }

        bool _useSampleTemplate;
        public bool UseSampleTemplate
        {
            get => this._useSampleTemplate;
            set
            {
                this._useSampleTemplate = value;
                this._useLocalFile = !value;
                base.NotifyPropertyChanged("UseSampleTemplate");
                base.NotifyPropertyChanged("UseLocalFile");
            }
        }

        TemplateLocation _sampleTemplate;
        public TemplateLocation SampleTemplate
        {
            get => this._sampleTemplate;
            set
            {
                this._sampleTemplate = value;
                base.NotifyPropertyChanged("SampleTemplate");
            }
        }

        string _snsTopic;
        public string SNSTopic
        {
            get => this._snsTopic;
            set
            {
                this._snsTopic = value;
                base.NotifyPropertyChanged("SNSTopic");
            }
        }

        string _creationTimeout = "None";
        public string CreationTimeout
        {
            get 
            {
                if (_creationTimeout == "None")
                    return this._creationTimeout;
                else
                    return this._creationTimeout.Substring(0, this._creationTimeout.IndexOf(' '));
            }
            set
            {
                this._creationTimeout = value;
                base.NotifyPropertyChanged("SelectedCreationTimeout");
            }
        }

        bool _rollbackOnFailure = true;
        public bool RollbackOnFailure
        {
            get => this._rollbackOnFailure;
            set
            {
                this._rollbackOnFailure = value;
                base.NotifyPropertyChanged("RollbackOnFailure");
            }
        }


        public bool HasValidStackName => IsValidStackName(this.StackName);


        public static bool IsValidStackName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            if (name.Length > 255)
                return false;

            if (!char.IsLetter(name[0]))
                return false;

            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '-')
                    continue;

                return false;
            }

            return true;
        }

        public bool HasValidLocalFile
        {
            get
            {
                if (string.IsNullOrEmpty(this.LocalFile))
                    return false;
                if (!File.Exists(this.LocalFile))
                    return false;


                return true;
            }
        }


        List<TemplateLocation> _templates;
        public List<TemplateLocation> Templates
        {
            get
            {
                if (this._templates == null)
                    this._templates = new List<TemplateLocation>();
                return this._templates;
            }
        }

        public class TemplateLocation
        {
            public string Name
            {
                get;
                set;
            }

            public string Location
            {
                get;
                set;
            }
        }
    }
}
