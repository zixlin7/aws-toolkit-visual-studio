﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;

using Amazon.CloudFormation.Model;

namespace Amazon.AWSToolkit.CloudFormation.Model
{
    public class ViewStackModel : BaseModel
    {
        public ViewStackModel(string stackName)
        {
            this._stackname = stackName;
            this.Outputs.CollectionChanged += onOutputCollectionChanged;
        }

        void onOutputCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            base.NotifyPropertyChanged("ApplicationUrl");
        }

        string _stackname;
        public string StackName
        {
            get { return this._stackname; }
            set
            {
                this._stackname = value;
                base.NotifyPropertyChanged("StackName");
                base.NotifyPropertyChanged("DisplayStackName");
            }
        }

        string _stackId;
        public string StackId
        {
            get { return this._stackId; }
            set
            {
                this._stackId = value;
                base.NotifyPropertyChanged("StackId");
                base.NotifyPropertyChanged("DisplayStackName");
            }
        }

        DateTime _created;
        public DateTime Created
        {
            get { return this._created; }
            set
            {
                this._created = value;
                base.NotifyPropertyChanged("Created");
            }
        }

        string _status;
        public string Status
        {
            get { return this._status; }
            set
            {
                this._status = value;
                base.NotifyPropertyChanged("Status");
                base.NotifyPropertyChanged("StatusColor");
            }
        }

        public SolidColorBrush StatusColor
        {
            get
            {
                Color clr;
                switch (this.Status)
                {
                    case CloudFormationConstants.CreateFailedStatus:
                    case CloudFormationConstants.UpdateRollbackCompleteStatus:
                    case CloudFormationConstants.UpdateRollbackFailedStatus:
                    case CloudFormationConstants.UpdateRollbackInProgressStatus:
                    case CloudFormationConstants.RollbackFailedStatus:
                    case CloudFormationConstants.DeleteFailedStatus:
                        clr = Colors.Red;
                        break;

                    case CloudFormationConstants.DeleteCompleteStatus:
                        clr = ThemeUtil.GetCurrentTheme() == VsTheme.Dark ? Colors.White : Colors.Black;
                        break;

                    case CloudFormationConstants.CreateInProgressStatus:
                    case CloudFormationConstants.UpdateInProgressStatus:
                    case CloudFormationConstants.UpdateRollbackCompleteCleanupInProgressStatus:
                    case CloudFormationConstants.DeleteInProgressStatus:
                    case CloudFormationConstants.RollbackInProgressStatus:
                        clr = Colors.Orange;
                        break;

                    default:
                        clr = Colors.Green;
                        break;
                }
                return new SolidColorBrush(clr);
            }
        }

        string _statusReason;
        public string StatusReason
        {
            get { return this._statusReason; }
            set
            {
                this._statusReason = value;
                base.NotifyPropertyChanged("StatusReason");
            }
        }

        string _snsTopic;
        public string SNSTopic
        {
            get { return this._snsTopic; }
            set
            {
                this._snsTopic = value;
                base.NotifyPropertyChanged("SNSTopic");
            }
        }

        string _createTimeout;
        public string CreateTimeout
        {
            get { return this._createTimeout; }
            set
            {
                this._createTimeout = value;
                base.NotifyPropertyChanged("CreateTimeout");
            }
        }

        bool _rollbackOnFailure;
        public bool RollbackOnFailure
        {
            get { return this._rollbackOnFailure; }
            set
            {
                this._rollbackOnFailure = value;
                base.NotifyPropertyChanged("RollbackOnFailure");
            }
        }

        string _description;
        public string Description
        {
            get { return this._description; }
            set
            {
                this._description = value;
                base.NotifyPropertyChanged("Description");
            }
        }

        CloudFormationTemplateWrapper _templateWrapper;
        public CloudFormationTemplateWrapper TemplateWrapper
        {
            get { return this._templateWrapper; }
            set
            {
                this._templateWrapper = value;
                this._parameters = null;

                base.NotifyPropertyChanged("TemplateWrapper");
                base.NotifyPropertyChanged("HideVSToolkitDeployedFields");
                base.NotifyPropertyChanged("TemplateBody");
                base.NotifyPropertyChanged("TemplateParameters");
            }
        }

        public Visibility VSToolkitDeployedFieldsVisibility
        {
            get
            {
                if (this._templateWrapper == null)
                    return Visibility.Collapsed;

                return this._templateWrapper.IsVSToolkitDeployed ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public string ApplicationUrl
        {
            get
            {
                if (this.Outputs == null)
                    return null;

                foreach (var output in this.Outputs)
                {
                    if (string.Equals("URL", output.OutputKey))
                        return output.OutputValue;
                }

                return null;
            }
        }

        public string TemplateBody
        {
            get
            {
                if (this._templateWrapper == null)
                    return null;

                return this._templateWrapper.TemplateContent;
            }
        }

        ObservableCollection<CloudFormationTemplateWrapper.TemplateParameter> _parameters = new ObservableCollection<CloudFormationTemplateWrapper.TemplateParameter>();
        public ObservableCollection<CloudFormationTemplateWrapper.TemplateParameter> TemplateParameters
        {
            get
            {
                if (this._templateWrapper == null || this._templateWrapper.Parameters == null)
                    return null;

                if (this._parameters == null)
                {
                    this._parameters = new ObservableCollection<CloudFormationTemplateWrapper.TemplateParameter>();
                    foreach (var param in this._templateWrapper.Parameters.Values.OrderBy(x => x.Name))
                    {
                        this._parameters.Add(param);
                    }
                }

                return _parameters;
            }
        }

        ObservableCollection<Output> _outputs = new ObservableCollection<Output>();
        public ObservableCollection<Output> Outputs
        {
            get { return this._outputs; }
            set
            {
                this._outputs = value;
                base.NotifyPropertyChanged("Outputs");
                base.NotifyPropertyChanged("ApplicationUrl");
            }
        }

        List<StackEventWrapper> _unfilteredEvents = new List<StackEventWrapper>();
        public List<StackEventWrapper> UnfilteredEvents
        {
            get { return this._unfilteredEvents; }
            set
            {
                this._unfilteredEvents = value;
                base.NotifyPropertyChanged("UnfilteredEvents");
            }
        }

        ObservableCollection<StackEventWrapper> _events = new ObservableCollection<StackEventWrapper>();
        public ObservableCollection<StackEventWrapper> Events
        {
            get { return this._events; }
            set
            {
                this._events = value;
                base.NotifyPropertyChanged("Events");
            }
        }

        string _eventTextFilter;
        public string EventTextFilter
        {
            get { return this._eventTextFilter; }
            set
            {
                this._eventTextFilter = value;
                base.NotifyPropertyChanged("EventTextFilter");
            }
        }

        ObservableCollection<RunningInstanceWrapper> _instances = new ObservableCollection<RunningInstanceWrapper>();
        public ObservableCollection<RunningInstanceWrapper> Instances
        {
            get { return this._instances; }
        }

        ObservableCollection<ResourceWrapper> _resources = new ObservableCollection<ResourceWrapper>();
        public ObservableCollection<ResourceWrapper> Resources
        {
            get { return this._resources; }
        }
    }
}
