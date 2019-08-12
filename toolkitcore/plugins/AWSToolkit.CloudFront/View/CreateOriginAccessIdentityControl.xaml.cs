using System;
using System.Windows;
using Amazon.AWSToolkit.CommonUI;

using Amazon.AWSToolkit.CloudFront.Controller;
using Amazon.AWSToolkit.CloudFront.Model;

namespace Amazon.AWSToolkit.CloudFront.View
{
    /// <summary>
    /// Interaction logic for CreateOriginAccessIdentityControl.xaml
    /// </summary>
    public partial class CreateOriginAccessIdentityControl : BaseAWSControl
    {
        CreateOriginAccessIdentityModel _model;
        CreateOriginAccessIdentityController _controller;
        public CreateOriginAccessIdentityControl(CreateOriginAccessIdentityController controller)
        {
            this._controller = controller;
            this._model = controller.Model;
            this.DataContext = this._model;
            InitializeComponent();
        }

        public override string Title => "Create Identity";

        public override bool OnCommit()
        {
            try
            {
                this._controller.Persist();
                return true;
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating origin access identity: " + e.Message);
                return false;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this._ctlComment.Focus();
        }

    }
}
