using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Controller;
using Amazon.AWSToolkit.Util;
using Amazon.EC2.Model;
using AMIImage = Amazon.EC2.Model.Image;

using log4net;


namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for AMIPermissionControl.xaml
    /// </summary>
    public partial class AMIPermissionControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(AttachVolumeControl));

        AMIPermissionController _controller;

        public AMIPermissionControl(AMIPermissionController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.PreviewKeyDown += new KeyEventHandler(onKeyDown);
        }

        public override string Title => "Set AMI Permissions";

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }

        public override bool OnCommit()
        {
            try
            {
                this._controller.SavePermissions();
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error saving permissions", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error saving permissions: " + e.Message);
                return false;
            }
        }

        void OnAddPermission(object sender, RoutedEventArgs e)
        {
            this._controller.Model.UserIds.Add(new MutableString());
            this._ctlDataGrid.SelectedIndex = this._controller.Model.UserIds.Count - 1;

            DataGridHelper.PutCellInEditMode(this._ctlDataGrid, this._ctlDataGrid.SelectedIndex, 0);
        }

        void OnRemovePermission(object sender, RoutedEventArgs e)
        {
            var toBeRemoved = new MutableString[this._ctlDataGrid.SelectedItems.Count];
            this._ctlDataGrid.SelectedItems.CopyTo(toBeRemoved, 0);
            foreach (var item in toBeRemoved)
            {
                this._controller.Model.UserIds.Remove((MutableString)item);
            }
        }

        // For allowing to paste in a list of account ids.
        void onKeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers == ModifierKeys.Control) && e.Key == Key.V)
            {
                string text = Clipboard.GetText();
                if (!checkIfListOfIds(text))
                    return;

                foreach (var token in text.Split('\n'))
                {
                    if (token.Trim().Length > 0)
                    {
                        // If the current cell is empty then use that before adding more values.
                        var current = this._ctlDataGrid.SelectedItem as MutableString;
                        if (current != null && string.IsNullOrEmpty(current.Value))
                            current.Value = token.Trim();
                        else
                            this._controller.Model.UserIds.Add(new MutableString(token.Trim()));
                    }
                }

                e.Handled = true;
            }
        }

        bool checkIfListOfIds(string text)
        {
            if (text == null)
                return false;

            text = text.Trim();
            if (text.Length == 0)
                return false;

            var tokens = text.Split('\n');
            // If there is only one token let the normal paste logic handle it.
            if (tokens.Length <= 1)
                return false;

            foreach (var token in tokens)
            {
                string trimmed = token.Trim();
                if (trimmed.Length == 0)
                    continue;

                if (isAccountId(trimmed))
                    return false;
            }

            return true;
        }

        bool isAccountId(string id)
        {
            // id is 12 of no hypens or 14 is does have hyphens
            if (id.Length < 12 || id.Length > 14)
                return false;

            foreach (var c in id)
            {
                if (!Char.IsDigit(c) || c != '-')
                    return false;
            }

            return true;
        }

        private void _otherAmiSelector_DropDownOpened(object sender, EventArgs e)
        {
            if (_otherAmiSelector.ItemsSource == null)
            {
                _otherAmiSelector.Cursor = Cursors.Wait;

                try
                {
                    List<AMIImage> images = new List<AMIImage>();
                    var request = new DescribeImagesRequest() { Owners = new List<string>() { "self" } };
                    var response = this._controller.EC2Client.DescribeImages(request);

                    if (response.Images != null && response.Images.Count > 0)
                    {
                        foreach (AMIImage img in response.Images)
                        {
                            if (img.ImageId != this._controller.Model.Image.ImageId)
                                images.Add(img);
                        }

                        _otherAmiSelector.ItemsSource = images;
                    }
                    else
                        ToolkitFactory.Instance.ShellProvider.ShowMessage("Copy Launch Permissions", "You have no other images to copy launch permissions from.");
                }
                catch(Exception exc)
                {
                    LOGGER.ErrorFormat("Caught exception querying for owned ami's, '{0}'", exc.Message);
                }

                _otherAmiSelector.Cursor = Cursors.Arrow;
            }
        }

        private void _otherAmiSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            e.Handled = true;

            if (e.AddedItems.Count == 0)
                return;

            _ctlDataGrid.Cursor = Cursors.Wait;

            AMIImage ami = e.AddedItems[0] as AMIImage;
            try
            {
                var response = this._controller.EC2Client.DescribeImageAttribute(new DescribeImageAttributeRequest()
                {
                    ImageId = ami.ImageId,
                    Attribute = "launchPermission"
                });

                if (response.ImageAttribute.LaunchPermissions.Count > 0)
                {
                    int added = 0;
                    int ignored = 0;
                    foreach (var launch in response.ImageAttribute.LaunchPermissions.OrderBy(x => x.UserId))
                    {
                        if (string.IsNullOrEmpty(launch.UserId))
                            continue;

                        var user = new MutableString(launch.UserId);
                        if (!this._controller.Model.UserIds.Contains(user))
                        {
                            this._controller.Model.UserIds.Add(user);
                            added++;
                        }
                        else
                            ignored++;
                    }

                    if (added != 0)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendFormat("Copied {0} launch permission(s) from the selected AMI.", added);
                        if (ignored > 0)
                            sb.AppendFormat("\r\r{0} permission(s) were already set on the target AMI and were ignored.", ignored);

                        ToolkitFactory.Instance.ShellProvider.ShowMessage("Copy Launch Permissions", sb.ToString());
                    }
                    else
                        ToolkitFactory.Instance.ShellProvider.ShowMessage("Copy Launch Permissions",
                                                                                "All launch permissions for the selected AMI were already set on the target AMI.");
                }
                else
                    ToolkitFactory.Instance.ShellProvider.ShowMessage("Copy Launch Permissions",
                                                                            "The selected AMI has no launch permissions to copy.");
            }
            catch (Exception exc)
            {
                LOGGER.ErrorFormat("Caught exception querying for permissions on selected ami '{0}', exception '{1}'", ami.ImageId, exc.Message);
                ToolkitFactory.Instance.ShellProvider.ShowError("Copy Launch Permissions", "Unable to retrieve launch permissions for the selected AMI.");
            }

            _ctlDataGrid.Cursor = Cursors.Arrow;

        }
    }
}
