using System;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI;
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

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }

        public override string Title => "Topic: " + this._title;

        public void SetTitle(string title)
        {
            this._title = title;
        }

        public override string UniqueId => this._uniqueId;

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
