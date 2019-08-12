﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Lambda.WizardPages.PageUI;
using Amazon.Lambda.Model;

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

        public string CodeSizeFormatted => string.Format("{0} bytes", this.CodeSize.ToString("0,0."));

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
                if (string.IsNullOrEmpty(Runtime) || !Runtime.ToString().StartsWith("dotnetcore", StringComparison.OrdinalIgnoreCase))
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

        ObservableCollection<EnvironmentVariable> _environmentVariables = new ObservableCollection<EnvironmentVariable>();
        public ObservableCollection<EnvironmentVariable> EnvironmentVariables => this._environmentVariables;

        public VpcConfigDetail VpcConfig { get; internal set; }

        public string KMSKeyArn { get; internal set; }

    }
}
