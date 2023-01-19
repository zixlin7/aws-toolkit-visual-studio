using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.IdentityManagement.Controller;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Navigator;

namespace Amazon.AWSToolkit.IdentityManagement.View
{
    /// <summary>
    /// Interaction logic for EditGroupControl.xaml
    /// </summary>
    public partial class EditGroupControl : BaseAWSControl
    {
        EditGroupController _controller;

        public EditGroupControl(EditGroupController controller)
        {
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(onDataContextChanged);
            this._controller = controller;
            InitializeComponent();
        }

        public override string Title => "Group: " + this._controller.Model.NewName;

        public override string UniqueId => "IAM:GROUP:" + this._controller.Model.OriginalName;

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }

        public override void OnEditorOpened(bool success)
        {
            ToolkitFactory.Instance.TelemetryLogger.RecordIamOpenGroup(new IamOpenGroup()
            {
                Result = success ? Result.Succeeded : Result.Failed,
            });
        }
        
        void onDirtyChange(object sender, TextChangedEventArgs e)
        {
            if (this.IsEnabled)
                this._controller.Model.IsDirty = true;
        }

        void onSave(object sender, RoutedEventArgs e)
        {
            var result = SaveChanges();
            _controller.RecordEditGroup(result);
        }

        private ActionResults SaveChanges()
        {
            try
            {
                var nameChange = _controller.Model.OriginalName.Equals(_controller.Model.NewName);
                _controller.Persist();
                _controller.Model.IsDirty = false;
                base.NotifyPropertyChanged("Title");
                return new ActionResults().WithSuccess(true);
            }
            catch (Exception ex)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error saving group: " + ex.Message);
                return ActionResults.CreateFailed(ex);
            }
        }

        void onDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            INotifyPropertyChanged notify = this.DataContext as INotifyPropertyChanged;
            if (notify == null)
                return;

            notify.PropertyChanged += new PropertyChangedEventHandler(onPropertyChanged);
        }

        void onPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("IsDirty"))
                return;

            onDirtyChange(sender, null);
        }

        private void onPolicyChange(object sender, EventArgs e)
        {
            onDirtyChange(sender, null);
        }

        void onRefreshClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.Refresh();
            }
            catch (Exception ex)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing group: " + ex.Message);
            }
        }
    }
}
