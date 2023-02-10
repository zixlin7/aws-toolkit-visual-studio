using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.IdentityManagement.Model;
using Amazon.AWSToolkit.IdentityManagement.Controller;
using Amazon.AWSToolkit.Navigator;

namespace Amazon.AWSToolkit.IdentityManagement.View.Components
{
    /// <summary>
    /// Interaction logic for AccessKeysControl.xaml
    /// </summary>
    public partial class AccessKeysControl
    {
        EditUserController _controller;
        public AccessKeysControl()
        {
            InitializeComponent();
        }

        public void SetController(EditUserController controller)
        {
            this._controller = controller;
        }

        public void onCreate(object sender, RoutedEventArgs e)
        {
            var result = CreateAccessKey();
            _controller.RecordCreateAccessKey(result);
        }

        private ActionResults CreateAccessKey()
        {
            try
            {
                var accessKeyModel = _controller.CreateNewAccessKeys();
                var control = new AccessKeyDetailsControl { DataContext = accessKeyModel };

                ToolkitFactory.Instance.ShellProvider.ShowModal(control, MessageBoxButton.OK);
               
                if (!accessKeyModel.PersistSecretKeyLocal)
                {
                    accessKeyModel.SecretKey = null;
                }

                return new ActionResults().WithSuccess(true);
            }
            catch (Exception ex)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError(
                    "Error creating access key: " + ex.Message);
                return ActionResults.CreateFailed(ex);
            }
        }

        private void onDelete(object sender, RoutedEventArgs e)
        {
            var result = DeleteAccessKey();
            _controller.RecordDeleteAccessKey(result);
        }

        private ActionResults DeleteAccessKey()
        {
            if (this._ctlAccessKeys.SelectedItems.Count == 0)
            {
                return ActionResults.CreateFailed(new ToolkitException("Unable to get delete access key data", ToolkitException.CommonErrorCode.InternalMissingServiceState));
            }

            if (!ToolkitFactory.Instance.ShellProvider.Confirm("Delete Access Keys",
                    string.Format("Are you sure you want to delete the {0} selected key(s)?",
                        this._ctlAccessKeys.SelectedItems.Count)))
            {
                return ActionResults.CreateCancelled();
            }

            List<AccessKeyModel> accessKeyModels = new List<AccessKeyModel>();
            try
            {
                foreach (AccessKeyModel accessKeyModel in this._ctlAccessKeys.SelectedItems)
                {
                    this._controller.DeleteAccessKey(accessKeyModel);
                    accessKeyModels.Add(accessKeyModel);
                }
            }
            catch (Exception ex)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError(
                    "Error deleting access key: " + ex.Message);
                return ActionResults.CreateFailed(ex);
            }

            EditUserModel model = this.DataContext as EditUserModel;
            foreach (var accessKeyModel in accessKeyModels)
            {
                model.AccessKeys.Remove(accessKeyModel);
            }

            return new ActionResults().WithSuccess(true);
        }

        private void onChangeStatus(object sender, RoutedEventArgs e)
        {
            if (!(sender is Control))
                return;

            string statusToChangeTo = string.Empty;
            try
            {
                Control ctrl = sender as Control;
                AccessKeyModel model = ctrl.DataContext as AccessKeyModel;
                statusToChangeTo = model.Status == AccessKeyModel.STATUS_ACTIVE ? AccessKeyModel.STATUS_INACTIVE : AccessKeyModel.STATUS_ACTIVE;

                this._controller.UpdateAccessKey(model.AccessKey, statusToChangeTo);
                model.Status = statusToChangeTo;
            }
            catch (Exception ex)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError(
                    string.Format("Error making key {0}: {1}", statusToChangeTo, ex.Message));
            }
        }
    }
}
