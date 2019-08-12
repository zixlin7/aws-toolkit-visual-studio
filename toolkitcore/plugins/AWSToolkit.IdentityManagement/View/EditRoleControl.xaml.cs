using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.IdentityManagement.Controller;


namespace Amazon.AWSToolkit.IdentityManagement.View
{
    /// <summary>
    /// Interaction logic for EditRoleControl.xaml
    /// </summary>
    public partial class EditRoleControl : BaseAWSControl
    {
        EditRoleController _controller;

        public EditRoleControl(EditRoleController controller)
        {
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(onDataContextChanged);
            this._controller = controller;
            InitializeComponent();
        }

        public override string Title => "Role: " + this._controller.Model.NewName;

        public override string UniqueId => "IAM:ROLE:" + this._controller.Model.OriginalName;

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
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing role: " + ex.Message);
            }
        }
    }
}
