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
    /// Interaction logic for EditUserControl.xaml
    /// </summary>
    public partial class EditUserControl : BaseAWSControl
    {
        EditUserController _controller;

        public EditUserControl(EditUserController controller)
        {
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(onDataContextChanged);
            this._controller = controller;
            InitializeComponent();
            this._ctlTwoListMoverGroups.OnDirty += new EventHandler(onTwoListMoverDirty);
            this._ctlAccessKeys.SetController(this._controller);
        }

        public override string Title => "User: " + this._controller.Model.NewName;

        public override string UniqueId => "IAM:User:" + this._controller.Model.OriginalName;

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();            
            return this._controller.Model;
        }

        public override void OnEditorOpened(bool success)
        {
            ToolkitFactory.Instance.TelemetryLogger.RecordIamOpenUser(new IamOpenUser()
            {
                Result = success ? Result.Succeeded : Result.Failed,
            });
        }

        void onDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this._ctlTwoListMoverGroups.DataContext = this._controller.Model;
            this._controller.Model.IsDirty = false;
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

        void onTwoListMoverDirty(object sender, EventArgs e)
        {
            this._controller.Model.IsDirty = true;
        }

        void onDirtyChange(object sender, TextChangedEventArgs e)
        {
            if(this.IsEnabled)
                this._controller.Model.IsDirty = true;
        }

        void onSave(object sender, RoutedEventArgs e)
        {
            var result = SaveChanges();
            _controller.RecordEditUser(result);
        }

        private ActionResults SaveChanges()
        {
            try
            {
                var nameChange = _controller.Model.OriginalName.Equals(_controller.Model.NewName);
                _controller.Persist();
                _controller.Model.IsDirty = false;
                _ctlTwoListMoverGroups.ResetDirty();
                base.NotifyPropertyChanged("Title");
                return new ActionResults().WithSuccess(true);
            }
            catch (Exception ex)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error saving user: " + ex.Message);
                return ActionResults.CreateFailed(ex);
            }
        }

        void onRefreshClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.Refresh();
                this._ctlPolices.BuildTabs();
            }
            catch (Exception ex)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing user: " + ex.Message);
            }
        }
    }
}
