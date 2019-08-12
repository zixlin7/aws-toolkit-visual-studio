﻿using System.Windows;
using Amazon.AWSToolkit.CloudFront.Model;
using Amazon.AWSToolkit.CloudFront.Controller;

namespace Amazon.AWSToolkit.CloudFront.View.Components
{
    /// <summary>
    /// Interaction logic for S3OriginControl.xaml
    /// </summary>
    public partial class S3OriginControl
    {
        BaseDistributionConfigEditorController _controller;
        public S3OriginControl()
        {
            InitializeComponent();
        }

        public void Initialize(BaseDistributionConfigEditorController controller)
        {
            this._controller = controller;
            this._ctlRequireHttps.Visibility = ShowRequireHttps;
        }

        protected void onCreateBucketClick(object sender, RoutedEventArgs e)
        {
            this._controller.CreateDistributionBucket();
        }

        protected void onCreateOriginAccessIdentityClick(object sender, RoutedEventArgs e)
        {
            this._controller.CreateOriginAccessIdentity();
        }

        public Visibility ShowRequireHttps
        {
            get
            {
                if (this._controller != null && this._controller.BaseModel is CreateDistributionModel)
                    return Visibility.Visible;

                return Visibility.Hidden;
            }
        }

    }
}
