using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Lambda.WizardPages.PageUI;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using static Amazon.Lambda.LastUpdateStatus;
using static Amazon.Lambda.State;


namespace Amazon.AWSToolkit.Lambda.Model
{
    public class ViewFunctionModel : BaseModel
    {
        public const string CodeSizeNotApplicable = "Not Applicable";

        public ViewFunctionModel(string functionName, string functionArn)
        {
            this.FunctionName = functionName;
            this.FunctionArn = functionArn;
            this.PropertyChanged += OnPropertyChanged;
        }

        bool _isDirty;

        public bool IsDirty
        {
            get => this._isDirty;
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
            get => this._functionName;
            set
            {
                this._functionName = value;
                this.NotifyPropertyChanged("FunctionName");
            }
        }

        long _codeSize;

        public long CodeSize
        {
            get => this._codeSize;
            set
            {
                this._codeSize = value;
                this.NotifyPropertyChanged("CodeSize");
                this.NotifyPropertyChanged("CodeSizeFormatted");
            }
        }

        bool _isEnabledActiveTracing;

        public bool IsEnabledActiveTracing
        {
            get => this._isEnabledActiveTracing;
            set
            {
                this._isEnabledActiveTracing = value;
                this.NotifyPropertyChanged("IsEnabledActiveTracing");
            }
        }

        string _dlqTargetArn;

        public string DLQTargetArn
        {
            get => this._dlqTargetArn;
            set
            {
                this._dlqTargetArn = value;
                this.NotifyPropertyChanged("DLQTargetArn");
            }
        }

        public string CodeSizeFormatted => GetFormattedCodeSize();

        string _description;

        public string Description
        {
            get => this._description;
            set
            {
                this._description = value;
                this.NotifyPropertyChanged("Description");
            }
        }

        string _functionArn;

        public string FunctionArn
        {
            get => this._functionArn;
            set
            {
                this._functionArn = value;
                this.NotifyPropertyChanged("FunctionArn");
            }
        }

        string _handler;

        public string Handler
        {
            get => this._handler;
            set
            {
                this._handler = value;
                this.NotifyPropertyChanged("Handler");
            }
        }

        // even though this cannot be changed by the user, implement as a 
        // property with change notifications so the UI can self-update
        // through data bind on the HandlerDescription property.
        Amazon.Lambda.Runtime _runtime;

        public Amazon.Lambda.Runtime Runtime
        {
            get => this._runtime;
            set
            {
                this._runtime = value;
                NotifyPropertyChanged("Runtime");
                NotifyPropertyChanged("HandlerDescriptionInfo");
            }
        }

        const string NonDotNetCoreHandlerDescription
            = "The function that Lambda calls to begin executing prefixed by the file name containing the function. "
              + "The function name can be found at \"exports.<handlername> = function(..)\" in your code body.";

        const string DotNetCoreHandlerDescription
            = "The Lambda handler field for .NET functions is <assembly>::<type>::<method>. "
              + "The handler field indicates to Lambda the .NET code to call for each invocation.";

        public string HandlerDescriptionInfo
        {
            get
            {
                if (string.IsNullOrEmpty(Runtime) ||
                    !Runtime.ToString().StartsWith("dotnetcore", StringComparison.OrdinalIgnoreCase))
                    return NonDotNetCoreHandlerDescription;

                return DotNetCoreHandlerDescription;
            }
        }

        DateTime _lastModified;

        public DateTime LastModified
        {
            get => this._lastModified;
            set
            {
                this._lastModified = value;
                this.NotifyPropertyChanged("LastModified");
            }
        }

        int _memorySize;

        public int MemorySize
        {
            get => this._memorySize;
            set
            {
                this._memorySize = value;
                this.NotifyPropertyChanged("MemorySize");
            }
        }

        private PackageType _packageType = PackageType.Zip;

        public PackageType PackageType
        {
            get => _packageType;
            set { SetProperty(ref _packageType, value, () => PackageType); }
        }


        private string _architectures;
        /// <summary>
        /// Represents the architectures associated with the lambda function
        /// </summary>
        public string Architectures
        {
            get => _architectures;
            set { SetProperty(ref _architectures, value, () => Architectures); }
        }

        string _role;

        public string Role
        {
            get => this._role;
            set
            {
                this._role = value;
                this.NotifyPropertyChanged("Role");
            }
        }

        int _timeout;

        public int Timeout
        {
            get => this._timeout;
            set
            {
                this._timeout = value;
                this.NotifyPropertyChanged("Timeout");
            }
        }

        string _lastUpdateStatus;

        public string LastUpdateStatus
        {
            get => this._lastUpdateStatus;
            set
            {
                this._lastUpdateStatus = value;
                base.NotifyPropertyChanged(nameof(LastUpdateStatus));
            }
        }

        string _lastUpdateStatusReason;

        public string LastUpdateStatusReason
        {
            get => this._lastUpdateStatusReason;
            set
            {
                this._lastUpdateStatusReason = value;
                base.NotifyPropertyChanged(nameof(LastUpdateStatusReason));
            }
        }

        string _lastUpdateStatusReasonCode;

        public string LastUpdateStatusReasonCode
        {
            get => this._lastUpdateStatusReasonCode;
            set
            {
                this._lastUpdateStatusReasonCode = value;
                base.NotifyPropertyChanged(nameof(LastUpdateStatusReasonCode));
            }
        }

        string _state;

        public string State
        {
            get => this._state;
            set
            {
                this._state = value;
                base.NotifyPropertyChanged(nameof(State));
            }
        }

        string _stateReason;

        public string StateReason
        {
            get => this._stateReason;
            set
            {
                this._stateReason = value;
                base.NotifyPropertyChanged(nameof(StateReason));
            }
        }

        string _stateReasonCode;

        public string StateReasonCode
        {
            get => this._stateReasonCode;
            set
            {
                this._stateReasonCode = value;
                base.NotifyPropertyChanged(nameof(StateReasonCode));
            }
        }


        public bool CanInvoke
        {
            get
            {
                if (State != null && (State.Equals(Pending.Value) || State.Equals(Amazon.Lambda.State.Failed.Value)))
                {
                    return false;
                }

                return true;
            }
        }

        private string _imageCommand = string.Empty;

        public string ImageCommand
        {
            get => _imageCommand;
            set { SetProperty(ref _imageCommand, value, () => ImageCommand); }
        }

        private string _imageEntrypoint = string.Empty;

        public string ImageEntrypoint
        {
            get => _imageEntrypoint;
            set { SetProperty(ref _imageEntrypoint, value, () => ImageEntrypoint); }
        }

        private string _imageWorkingDirectory = string.Empty;

        public string ImageWorkingDirectory
        {
            get => _imageWorkingDirectory;
            set { SetProperty(ref _imageWorkingDirectory, value, () => ImageWorkingDirectory); }
        }

        private string _imageUri = string.Empty;

        public string ImageUri
        {
            get => _imageUri;
            set { SetProperty(ref _imageUri, value, () => ImageUri); }
        }

        string _invokeWarningText;

        //used for info banner to notify user about lambda status
        public string InvokeWarningText
        {
            get => this._invokeWarningText;
            set
            {
                this._invokeWarningText = value;
                base.NotifyPropertyChanged(nameof(InvokeWarningText));
            }
        }

        string _invokeWarningTooltip;

        //used for tooltip text to notify user about lambda status
        public string InvokeWarningTooltip
        {
            get => this._invokeWarningTooltip;
            set
            {
                this._invokeWarningTooltip = value;
                base.NotifyPropertyChanged(nameof(InvokeWarningTooltip));
            }
        }

        Visibility _invokeWarningVisibility;

        //determine info banner visibility
        public Visibility InvokeWarningVisibility
        {
            get => this._invokeWarningVisibility;
            set
            {
                this._invokeWarningVisibility = value;
                base.NotifyPropertyChanged(nameof(InvokeWarningVisibility));
            }
        }

        public Visibility UploadSourceVisibility
        {
            get
            {
                if (Runtime!=null && Runtime.Value.StartsWith("nodejs", StringComparison.OrdinalIgnoreCase))
                {
                    return Visibility.Visible;
                }

                return Visibility.Collapsed;
            }
        }


        ObservableCollection<EventSourceWrapper> _eventSources = new ObservableCollection<EventSourceWrapper>();
        public ICollection<EventSourceWrapper> EventSources => this._eventSources;

        EventSourceWrapper _selectedEventSource;

        public EventSourceWrapper SelectedEventSource
        {
            get => this._selectedEventSource;
            set
            {
                this._selectedEventSource = value;
                base.NotifyPropertyChanged("SelectedEventSource");
            }
        }

        ObservableCollection<LogStreamWrapper> _logs = new ObservableCollection<LogStreamWrapper>();
        public ObservableCollection<LogStreamWrapper> Logs => this._logs;

        ObservableCollection<EnvironmentVariable> _environmentVariables =
            new ObservableCollection<EnvironmentVariable>();

        public ObservableCollection<EnvironmentVariable> EnvironmentVariables => this._environmentVariables;

        public VpcConfigDetail VpcConfig { get; internal set; }

        public string KMSKeyArn { get; internal set; }

        public string GetFormattedCodeSize()
        {
            if (PackageType == PackageType.Zip)
            {
                return string.Format("{0} bytes", this.CodeSize.ToString("0,0."));
            }

            return CodeSizeNotApplicable;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(State)))
            {
                base.NotifyPropertyChanged(nameof(CanInvoke));
                UpdateInvokeFields();
            }

            if (e.PropertyName.Equals(nameof(LastUpdateStatus)))
            {
                UpdateInvokeFields();
            }

            if (e.PropertyName.Equals(nameof(PackageType)))
            {
                base.NotifyPropertyChanged(nameof(CodeSizeFormatted));
            }

            if (e.PropertyName.Equals(nameof(Runtime)))
            {
                base.NotifyPropertyChanged(nameof(UploadSourceVisibility));
            }
        }

        public void UpdateInvokeFields()
        {
            if (!CanInvoke)
            {
                InvokeWarningText = $"Lambda function cannot be invoked. {StateReason ?? string.Empty}";
                InvokeWarningVisibility = Visibility.Visible;
                InvokeWarningTooltip = InvokeWarningText;
                return;
            }

            if (LastUpdateStatus != null && (LastUpdateStatus.Equals(InProgress.Value) ||
                                             LastUpdateStatus.Equals(Amazon.Lambda.LastUpdateStatus.Failed.Value)))
            {
                InvokeWarningText = $"Lambda Update {LastUpdateStatus}. You might invoke an earlier version.";
                InvokeWarningVisibility = Visibility.Visible;
                InvokeWarningTooltip = InvokeWarningText;
                return;
            }

            InvokeWarningVisibility = Visibility.Collapsed;
            InvokeWarningTooltip = "Invoke Function";
        }
    }
}
