using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.ElasticBeanstalk.Nodes;
using Amazon.AWSToolkit.ElasticBeanstalk.View;
using Amazon.AWSToolkit.ElasticBeanstalk.Model;
using Amazon.AWSToolkit;

using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;

using log4net;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Controller
{
    public class ApplicationStatusController : BaseContextCommand
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(ApplicationStatusController));

        readonly object UPDATE_EVENT_LOCK_OBJECT = new object();

        IAmazonElasticBeanstalk _beanstalkClient;
        ApplicationViewModel _ApplicationModel;

        ApplicationStatusModel _statusModel;

        public override ActionResults Execute(IViewModel model)
        {
            this._ApplicationModel = model as ApplicationViewModel;
            if (this._ApplicationModel == null)
                return new ActionResults().WithSuccess(false);

            this._beanstalkClient = this._ApplicationModel.BeanstalkClient;

            this._statusModel = new ApplicationStatusModel(this._ApplicationModel.Application.ApplicationName);

            ApplicationStatusControl control = new ApplicationStatusControl(this); 

            ToolkitFactory.Instance.ShellProvider.OpenInEditor(control);

            return new ActionResults()
                    .WithSuccess(true);
        }

        public ApplicationStatusModel Model
        {
            get { return this._statusModel; }
        }

        public void LoadModel()
        {
            Refresh();
        }

        public void Refresh()
        {
            try
            {
                refreshApplicationProperties();
                refreshEvents();
                refreshVersions();
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error Refreshing",
                    string.Format("Error refreshing application {0}: {1}", this.Model.ApplicationName, e.Message));
            }
        }

        void refreshApplicationProperties()
        {
            var response = this._beanstalkClient.DescribeApplications(new DescribeApplicationsRequest() { ApplicationNames = new List<string>() { this._statusModel.ApplicationName } });
            if (response.Applications.Count == 0)
                return;

            var application = response.Applications[0];
            this._statusModel.SetApplicationDescription(application);
        }

        void refreshEvents()
        {
            var request = new DescribeEventsRequest() { ApplicationName = this._statusModel.ApplicationName };

            var response = this._beanstalkClient.DescribeEvents(request);

            ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
            {
                lock (UPDATE_EVENT_LOCK_OBJECT)
                {
                    this._statusModel.Events.Clear();
                    foreach (var evnt in response.Events.OrderBy(x => x.EventDate))
                    {
                        var wrapper = new EventWrapper(evnt);
                        this._statusModel.UnfilteredEvents.Insert(0, wrapper);

                        if (wrapper.PassClientFilter(this._statusModel.TextFilter, true))
                            this._statusModel.Events.Insert(0, wrapper);
                    }
                }
            }));
        }

        void refreshVersions()
        {
            var request = new DescribeApplicationVersionsRequest() { ApplicationName = this._statusModel.ApplicationName };

            var response = this._beanstalkClient.DescribeApplicationVersions(request);

            ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
            {
                this._statusModel.Versions.Clear();
                foreach (var version in response.ApplicationVersions)
                {
                    this._statusModel.Versions.Add(new ApplicationVersionDescriptionWrapper(version));
                }
            }));
        }

        public void ReapplyFilter()
        {
            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
            {
                lock (UPDATE_EVENT_LOCK_OBJECT)
                {
                    this._statusModel.Events.Clear();

                    foreach (var evnt in this._statusModel.UnfilteredEvents)
                    {
                        if (evnt.PassClientFilter(this._statusModel.TextFilter, true))
                            this._statusModel.Events.Add(evnt);
                    }
                }
            }));
        }

        public void DeleteVersion(ApplicationVersionDescriptionWrapper version)
        {
            var deleteController = new DeleteApplicationVersionController();
            if(deleteController.Execute(this._beanstalkClient, this._statusModel.ApplicationName, version.VersionLabel))
            {
                this._statusModel.Versions.Remove(version);
                this.refreshEvents();
            }
        }

        public void DeployedVersion(ApplicationVersionDescriptionWrapper version)
        {
            var deployController = new DeployApplicationVersionController();
            if (deployController.Execute(this._ApplicationModel, version.VersionLabel))
            {
                this.refreshEvents();
            }
        }
    }
}
