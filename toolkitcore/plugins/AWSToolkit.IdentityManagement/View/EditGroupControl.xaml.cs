using System;
using System.Collections.Generic;
using System.ComponentModel;
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

using Amazon.AWSToolkit.Navigator.Node;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.JobTracker;
using Amazon.AWSToolkit.IdentityManagement.Model;
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

        public override string Title
        {
            get
            {
                return "Group: " + this._controller.Model.NewName;
            }
        }

        public override string UniqueId
        {
            get
            {
                return "IAM:GROUP:" + this._controller.Model.OrignalName;
            }
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

        void onDirtyChange(object sender, TextChangedEventArgs e)
        {
            if (this.IsEnabled)
                this._controller.Model.IsDirty = true;
        }

        void onSave(object sender, RoutedEventArgs e)
        {
            try
            {
                bool nameChange = this._controller.Model.OrignalName.Equals(this._controller.Model.NewName);
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
