using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Win32;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.JobTracker;
using Amazon.AWSToolkit.SNS.Model;
using Amazon.AWSToolkit.SNS.Controller;


namespace Amazon.AWSToolkit.SNS.View
{
    /// <summary>
    /// Interaction logic for ViewTopicControl.xaml
    /// </summary>
    public partial class ViewTopicControl : BaseAWSView
    {
        ViewTopicController _controller;
        ViewTopicModel _model;
        string _title = "";
        string _uniqueId = "";


        public ViewTopicControl()
            : this(null, null)
        {
        }

        public ViewTopicControl(ViewTopicController controller, ViewTopicModel model)
        {
            this._controller = controller;
            this._model = model;
            InitializeComponent();
            importSubscriptions();
        }

        public override bool SupportsBackGroundDataLoad
        {
            get { return true; }
        }

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }

        public override string Title
        {
            get {return "Topic: " + this._title;}
        }

        public void SetTitle(string title)
        {
            this._title = title;
        }

        public override string UniqueId
        {
            get {return this._uniqueId;}
        }

        public void SetUniqueId(string uniqueId)
        {
            this._uniqueId = uniqueId;
        }

        private void onPublishToTopicClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.PublishToTopic();
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error publishing to topic: " + e.Message);
            }
        }

        private void onDisplayNameClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.EditDisplayName();
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error editing name: " + e.Message);
            }
        }



        private void importSubscriptions()
        {
            ViewSubscriptionsControl control = this._controller.CreateSubscriptionControl();
            if (control is IPropertySupport)
            {
                var propSup = control as IPropertySupport;
                propSup.OnPropertyChange += new PropertySourceChange(onSubscriptionPropertyChange);
            }

            this._ctlGrid.Children.Add(control.UserControl);
            Grid.SetColumn(control.UserControl, 0);
            Grid.SetColumnSpan(control.UserControl, 2);
            Grid.SetRow(control.UserControl, 3);

            control.UserControl.Width = double.NaN;
            control.UserControl.Height = double.NaN;
            control.UserControl.VerticalContentAlignment = VerticalAlignment.Stretch;
            control.UserControl.HorizontalContentAlignment = HorizontalAlignment.Stretch;

        }

        void onSubscriptionPropertyChange(object sender, bool forceShow, System.Collections.IList propertyObjects)
        {
            this.PropageProperties(forceShow, propertyObjects);
        }
    }
}
