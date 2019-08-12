using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Amazon.AWSToolkit.CommonUI;

using Amazon.ElasticBeanstalk.Model;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Model
{
    public class ApplicationStatusModel : BaseModel
    {
        ApplicationDescription _description;
        public ApplicationStatusModel(string ApplicationName)
        {
            this._description = new ApplicationDescription();
            this._description.ApplicationName = ApplicationName;
        }

        public void SetApplicationDescription(ApplicationDescription description)
        {
            this._description = description;
            base.NotifyPropertyChanged("ApplicationName");
            base.NotifyPropertyChanged("Description");
            base.NotifyPropertyChanged("DateCreated");
            base.NotifyPropertyChanged("DateUpdated");
        }

        public string ApplicationName
        {
            get => this._description.ApplicationName;
            set
            {
                this._description.ApplicationName = value;
                base.NotifyPropertyChanged("ApplicationName");
            }
        }

        public string Description
        {
            get => this._description.Description;
            set
            {
                this._description.Description = value;
                base.NotifyPropertyChanged("Description");
            }
        }

        public DateTime DateCreated
        {
            get => this._description.DateCreated;
            set
            {
                this._description.DateCreated = value;
                base.NotifyPropertyChanged("DateCreated");
            }
        }

        public DateTime DateUpdated
        {
            get => this._description.DateUpdated;
            set
            {
                this._description.DateUpdated = value;
                base.NotifyPropertyChanged("DateUpdated");
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

        ObservableCollection<ApplicationVersionDescriptionWrapper> _versions = new ObservableCollection<ApplicationVersionDescriptionWrapper>();
        public ObservableCollection<ApplicationVersionDescriptionWrapper> Versions
        {
            get => this._versions;
            set
            {
                this._versions = value;
                base.NotifyPropertyChanged("Versions");
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
    }
}
