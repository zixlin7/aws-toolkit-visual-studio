using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media;

using Amazon.AWSToolkit.CommonUI;


namespace Amazon.AWSToolkit.ElasticBeanstalk.Model
{
    public class EnvironmentStatusModel : BaseModel
    {
        public EnvironmentStatusModel(string environmentId, string environmentName)
        {
            this._environmentId = environmentId;
            this._environmentName = environmentName;
        }

        string _environmentId;
        public string EnvironmentId
        {
            get => this._environmentId;
            set
            {
                this._environmentId = value;
                base.NotifyPropertyChanged("EnvironmentId");
            }
        }

        string _environmentName;
        public string EnvironmentName
        {
            get => this._environmentName;
            set
            {
                this._environmentName = value;
                base.NotifyPropertyChanged("EnvironmentName");
            }
        }

        string _description;
        public string Description
        {
            get => this._description;
            set
            {
                this._description = value;
                base.NotifyPropertyChanged("Description");
            }
        }

        DateTime _dateCreated;
        public DateTime DateCreated
        {
            get => this._dateCreated;
            set
            {
                this._dateCreated = value;
                base.NotifyPropertyChanged("DateCreated");
            }
        }

        DateTime _dateUpdated;
        public DateTime DateUpdated
        {
            get => this._dateUpdated;
            set
            {
                this._dateUpdated = value;
                base.NotifyPropertyChanged("DateUpdated");
            }
        }

        DateTime _lastEventTimestamp;
        public DateTime LastEventTimestamp
        {
            get => this._lastEventTimestamp;
            set
            {
                this._lastEventTimestamp = value;
                base.NotifyPropertyChanged("LastEventTimestamp");
            }
        }

        DateTime _resourcesUpdated;
        public DateTime ResourcesUpdated
        {
            get => this._resourcesUpdated;
            set
            {
                this._resourcesUpdated = value;
                base.NotifyPropertyChanged("ResourcesUpdated");
            }
        }

        string _applicationName;
        public string ApplicationName
        {
            get => this._applicationName;
            set
            {
                this._applicationName = value;
                base.NotifyPropertyChanged("ApplicationName");
            }
        }

        string _versionLabel;
        public string VersionLabel
        {
            get => this._versionLabel;
            set
            {
                this._versionLabel = value;
                base.NotifyPropertyChanged("VersionLabel");
            }
        }

        string _containerType;
        public string ContainerType
        {
            get => this._containerType;
            set
            {
                this._containerType = value;
                base.NotifyPropertyChanged("ContainerType");
            }
        }

        string _endPointURL;
        public string EndPointURL
        {
            get => this._endPointURL;
            set
            {
                this._endPointURL = value;
                base.NotifyPropertyChanged("EndPointURL");
            }
        }

        string _status;
        public string Status
        {
            get => this._status;
            set
            {
                this._status = value;
                base.NotifyPropertyChanged("Status");
                base.NotifyPropertyChanged("CompositeStatusHealth");
                base.NotifyPropertyChanged("CompositeStatusHealthColor");
            }
        }

        string _health;
        public string Health
        {
            get => this._health;
            set
            {
                this._health = value;
                base.NotifyPropertyChanged("Health");
                base.NotifyPropertyChanged("CompositeStatusHealth");
                base.NotifyPropertyChanged("CompositeStatusHealthColor");
            }
        }

        public string CompositeStatusHealth
        {
            get
            {
                // If the environment is ready then use the health status
                if (BeanstalkConstants.STATUS_READY.Equals(this.Status))
                {
                    switch (this.Health)
                    {
                        case BeanstalkConstants.HEALTH_GREEN:
                            return "Environment is healthy";
                        case BeanstalkConstants.HEALTH_RED:
                        case BeanstalkConstants.HEALTH_YELLOW:
                            return "Environment is not responsive";
                        default:
                            return this.Status;
                    }
                }

                return this.Status;
            }
        }

        public SolidColorBrush CompositeStatusHealthColor
        {
            get
            {
                Color clr;
                if (BeanstalkConstants.STATUS_READY.Equals(this.Status))
                {
                    switch (this.Health)
                    {
                        case BeanstalkConstants.HEALTH_GREEN:
                            clr = Colors.Green;
                            break;

                        case BeanstalkConstants.HEALTH_RED:
                        case BeanstalkConstants.HEALTH_YELLOW:
                            clr = Colors.Red;
                            break;

                        default:
                            clr = ThemeUtil.GetCurrentTheme() == VsTheme.Dark 
                                ? Colors.White
                                : new Color() { A = 255 };
                            break;
                    }
                }
                else
                {
                    clr = ThemeUtil.GetCurrentTheme() == VsTheme.Dark
                        ? Colors.White
                        : new Color() { A = 255 };
                }

                return new SolidColorBrush(clr);
            }
        }


        string _cname;
        public string CNAME
        {
            get => this._cname;
            set
            {
                this._cname = value;
                base.NotifyPropertyChanged("CNAME");
            }
        }

        List<EventWrapper> _unfilteredEvents = new List<EventWrapper>();
        public List<EventWrapper> UnfilteredEvents
        {
            get => this._unfilteredEvents;
            set
            {
                this._unfilteredEvents = value;
                base.NotifyPropertyChanged("UnfilteredEvents");
            }
        }


        ObservableCollection<EventWrapper> _events = new ObservableCollection<EventWrapper>();
        public ObservableCollection<EventWrapper> Events
        {
            get => this._events;
            set
            {
                this._events = value;
                base.NotifyPropertyChanged("Events");
            }
        }

        ObservableCollection<InstanceWrapper> _instances = new ObservableCollection<InstanceWrapper>();
        public ObservableCollection<InstanceWrapper> Instances
        {
            get => this._instances;
            set
            {
                this._instances = value;
                base.NotifyPropertyChanged("Instances");
            }
        }

        ObservableCollection<LoadBalancerWrapper> _loadBalancers = new ObservableCollection<LoadBalancerWrapper>();
        public ObservableCollection<LoadBalancerWrapper> LoadBalancers
        {
            get => this._loadBalancers;
            set
            {
                this._loadBalancers = value;
                base.NotifyPropertyChanged("LoadBalancers");
            }
        } 
                
        ObservableCollection<AutoScalingGroupWrapper> _autoScalingGroups = new ObservableCollection<AutoScalingGroupWrapper>();
        public ObservableCollection<AutoScalingGroupWrapper> AutoScalingGroups
        {
            get => this._autoScalingGroups;
            set
            {
                this._autoScalingGroups = value;
                base.NotifyPropertyChanged("AutoScalingGroups");
            }
        } 

        ObservableCollection<MetricAlarmWrapper> _triggers = new ObservableCollection<MetricAlarmWrapper>();
        public ObservableCollection<MetricAlarmWrapper> Triggers
        {
            get => this._triggers;
            set
            {
                this._triggers = value;
                base.NotifyPropertyChanged("Triggers");
            }
        } 

        string _textFilter;
        public string TextFilter
        {
            get => this._textFilter;
            set
            {
                this._textFilter = value;
                base.NotifyPropertyChanged("TextFilter");
            }
        }

        EnvironmentConfigModel _configModel;
        public EnvironmentConfigModel ConfigModel
        {
            get
            {
                if (this._configModel == null)
                    this._configModel = new EnvironmentConfigModel("ConfigModel");
                return this._configModel;
            }
        }

        internal static string StringIfSet<T>(T value, string prefix, string postfix) where T : IComparable<T>
        {
            return value.CompareTo(default(T)) == 0 ? "" : String.Format("{0}{1}{2}", prefix, value, postfix);
        }
    }
}
