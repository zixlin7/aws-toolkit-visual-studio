using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.IdentityManagement.Controller;

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

        void onDirtyChange(object sender, TextChangedEventArgs e)
        {
            if (this.IsEnabled)
                this._controller.Model.IsDirty = true;
        }

        void onSave(object sender, RoutedEventArgs e)
        {
            try
            {
                bool nameChange = this._controller.Model.OriginalName.Equals(this._controller.Model.NewName);
                this._controller.Persist();
                this._controller.Model.IsDirty = false;
                base.NotifyPropertyChanged("Title");
            }
            catch (Exception ex)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error saving group: " + ex.Message);
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
