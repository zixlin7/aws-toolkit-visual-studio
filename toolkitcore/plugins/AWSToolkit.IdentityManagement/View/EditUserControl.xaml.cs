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

        public override string Title
        {
            get
            {
                return "User: " + this._controller.Model.NewName;
            }
        }

        public override string UniqueId
        {
            get
            {
                return "IAM:User:" + this._controller.Model.OriginalName;
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
            try
            {
                bool nameChange = this._controller.Model.OriginalName.Equals(this._controller.Model.NewName);
                this._controller.Persist();
                this._controller.Model.IsDirty = false;
                this._ctlTwoListMoverGroups.ResetDirty();
                base.NotifyPropertyChanged("Title");
            }
            catch(Exception ex)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error saving user: " + ex.Message);
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
