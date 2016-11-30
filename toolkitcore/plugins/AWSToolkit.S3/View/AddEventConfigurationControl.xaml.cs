using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Amazon.AWSToolkit.S3.Controller;

using log4net;

namespace Amazon.AWSToolkit.S3.View
{
    /// <summary>
    /// Interaction logic for AddEventConfigurationControl.xaml
    /// </summary>
    public partial class AddEventConfigurationControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(AddEventConfigurationControl));

        public const int SERVICE_TYPE_SNS_INDEX = 0;
        public const int SERVICE_TYPE_SQS_INDEX = 1;
        public const int SERVICE_TYPE_LAMBDA_INDEX = 2;

        public enum ServiceType { SNS, SQS, Lambda };

        AddEventConfigurationController _controller;

        public AddEventConfigurationControl(AddEventConfigurationController controller)
        {
            InitializeComponent();
            this._controller = controller;

            SendTo_SelectionChanged(this, null);
        }

        public override string Title
        {
            get
            {
                return "Add Event Configuration";
            }
        }

        public override bool Validated()
        {
            string noun = null;
            switch (this.Service)
            {
                case ServiceType.Lambda:
                    noun = "function";
                    break;
                case ServiceType.SNS:
                    noun = "topic";
                    break;
                case ServiceType.SQS:
                    noun = "queue";
                    break;
            }

            if (string.IsNullOrEmpty(this._ctlResources.Text))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError(string.Format("A {0} must be selected/", noun));
                return false;
            }
            if (string.IsNullOrEmpty(this._ctlEvents.Text))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("An event must be selected/");
                return false;
            }

            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                this._controller.Persist();
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error adding event configuration", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error adding event configuration: " + e.Message);
                return false;
            }
        }

        private void SendTo_SelectionChanged(object sender, SelectionChangedEventArgs evnt)
        {
            if (this._controller == null)
                return;

            try
            {
                IList<string> resources = null;
                switch (this.Service)
                {
                    case ServiceType.Lambda:
                        AlterHiddenControls(System.Windows.Visibility.Collapsed, System.Windows.Visibility.Visible, "Update Lambda Function to allow S3 to invoke the function.");
                        this._ctlResourceLabel.Text = "Function:";
                        resources = this._controller.GetLambdaArns();
                        break;
                    case ServiceType.SNS:
                        AlterHiddenControls(System.Windows.Visibility.Collapsed, System.Windows.Visibility.Visible, "Update SNS Topic to allow S3 to publish to the topic.");
                        this._ctlResourceLabel.Text = "Topic:";
                        resources = this._controller.GetTopicArns();
                        break;
                    case ServiceType.SQS:
                        AlterHiddenControls(System.Windows.Visibility.Collapsed, System.Windows.Visibility.Visible, "Update SQS Queue to allow S3 to send messages to the queue.");
                        this._ctlResourceLabel.Text = "Queue:";
                        resources = this._controller.GetQueueArns();
                        break;
                }

                this._ctlResources.Items.Clear();
                foreach (var resource in resources)
                {
                    this._ctlResources.Items.Add(resource);
                }
                if (this._ctlResources.Items.Count > 0)
                    this._ctlResources.SelectedIndex = 0;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error updating resources", e); 
                ToolkitFactory.Instance.ShellProvider.ShowError("Error updating resources: " + e.Message);
            }
        }

        private void AlterHiddenControls(Visibility iamVisibility, Visibility policyPermissions, string message)
        {
            this._ctlAlterPolicy.Visibility = policyPermissions;
            this._ctlAlterPolicyMessage.Text = message;
        }

        public ServiceType Service
        {
            get
            {
                switch (this._ctlSourceType.SelectedIndex)
                {
                    case SERVICE_TYPE_SNS_INDEX:
                        return ServiceType.SNS;
                    case SERVICE_TYPE_SQS_INDEX:
                        return ServiceType.SQS;
                    case SERVICE_TYPE_LAMBDA_INDEX:
                        return ServiceType.Lambda;
                    default:
                        return ServiceType.SNS;
                }
            }
        }


        public string Prefix
        {
            get { return this._ctlPrefix.Text; }
        }

        public string Suffix
        {
            get { return this._ctlSuffix.Text; }
        }

        public string ResourceArn
        {
            get { return this._ctlResources.SelectedItem as string; }
        }

        public bool AddPermissions
        {
            get { return this._ctlAlterPolicy.IsChecked.GetValueOrDefault(); }
        }

        public string Event
        {
            get 
            {
                if (!(this._ctlEvents.SelectedItem is ComboBoxItem))
                    return null;

                return ((ComboBoxItem)this._ctlEvents.SelectedItem).Content.ToString(); 
            }
        }
    }
}
