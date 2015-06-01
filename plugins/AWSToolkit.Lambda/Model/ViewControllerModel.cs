using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Media;

using Amazon.AWSToolkit.CommonUI;


namespace Amazon.AWSToolkit.Lambda.Model
{
    public class ViewFunctionModel : BaseModel
    {
        public ViewFunctionModel(string functionName, string functionArn)
        {
            this.FunctionName = functionName;
            this.FunctionArn = functionArn;
        }

        bool _isDirty;
        public bool IsDirty
        {
            get { return this._isDirty; }
            set
            {
                this._isDirty = value;
                base.NotifyPropertyChanged("IsDirty");
            }
        }

        protected override void NotifyPropertyChanged(string propertyName)
        {
            if (!string.Equals("IsDirty", propertyName, StringComparison.InvariantCultureIgnoreCase))
                this.IsDirty = true;

            base.NotifyPropertyChanged(propertyName);
        }

        string _functionName;
        public string FunctionName
        {
            get { return this._functionName; }
            set
            {
                this._functionName = value;
                this.NotifyPropertyChanged("FunctionName");
            }
        }

        long _codeSize;
        public long CodeSize
        {
            get { return this._codeSize; }
            set
            {
                this._codeSize = value;
                this.NotifyPropertyChanged("CodeSize");
                this.NotifyPropertyChanged("CodeSizeFormatted");
            }
        }

        public string CodeSizeFormatted
        {
            get { return string.Format("{0} bytes", this.CodeSize.ToString("0,0.")); }
        }

        string _description;
        public string Description
        {
            get { return this._description; }
            set
            {
                this._description = value;
                this.NotifyPropertyChanged("Description");
            }
        }

        string _functionArn;
        public string FunctionArn
        {
            get { return this._functionArn; }
            set
            {
                this._functionArn = value;
                this.NotifyPropertyChanged("FunctionArn");
            }
        }

        string _handler;
        public string Handler
        {
            get { return this._handler; }
            set
            {
                this._handler = value;
                this.NotifyPropertyChanged("Handler");
            }
        }

        DateTime _lastModified;
        public DateTime LastModified
        {
            get { return this._lastModified; }
            set
            {
                this._lastModified = value;
                this.NotifyPropertyChanged("LastModified");
            }
        }

        int _memorySize;
        public int MemorySize
        {
            get { return this._memorySize; }
            set
            {
                this._memorySize = value;
                this.NotifyPropertyChanged("MemorySize");
            }
        }

        string _role;
        public string Role
        {
            get { return this._role; }
            set
            {
                this._role = value;
                this.NotifyPropertyChanged("Role");
            }
        }

        int _timeout;
        public int Timeout
        {
            get { return this._timeout; }
            set
            {
                this._timeout = value;
                this.NotifyPropertyChanged("Timeout");
            }
        }

        ObservableCollection<EventSourceWrapper> _eventSources = new ObservableCollection<EventSourceWrapper>();
        public ICollection<EventSourceWrapper> EventSources
        {
            get { return this._eventSources; }
        }

        EventSourceWrapper _selectedEventSource;
        public EventSourceWrapper SelectedEventSource 
        {
            get { return this._selectedEventSource; }
            set
            {
                this._selectedEventSource = value;
                base.NotifyPropertyChanged("SelectedEventSource");
            }
        }

        ObservableCollection<LogStreamWrapper> _logs = new ObservableCollection<LogStreamWrapper>();
        public ObservableCollection<LogStreamWrapper> Logs
        {
            get { return this._logs; }
        }

    }
}
