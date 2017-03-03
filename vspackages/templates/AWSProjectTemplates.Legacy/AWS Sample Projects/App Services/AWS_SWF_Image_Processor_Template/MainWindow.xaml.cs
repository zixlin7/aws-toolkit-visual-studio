/*******************************************************************************
* Copyright 2009-2013 Amazon.com, Inc. or its affiliates. All Rights Reserved.
* 
* Licensed under the Apache License, Version 2.0 (the "License"). You may
* not use this file except in compliance with the License. A copy of the
* License is located at
* 
* http://aws.amazon.com/apache2.0/
* 
* or in the "license" file accompanying this file. This file is
* distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
* KIND, either express or implied. See the License for the specific
* language governing permissions and limitations under the License.
*******************************************************************************/
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

using Microsoft.Win32;

using $safeprojectname$.SWF;

using Amazon;
using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;

using Amazon.S3;
using Amazon.S3.Model;


namespace $safeprojectname$
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        CancellationTokenSource _cancellationSource;
        StartWorkflowExecutionProcessor _startProcessor;

        public MainWindow()
        {
            InitializeComponent();

            if (!IsConfigSet())
            {
                this.Close();
                return;
            }

            try
            {
                Utils.Setup();
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to setup SWF Domain and Activity: " + e.Message);
                this.Close();
            }

            this._cancellationSource = new CancellationTokenSource();

            new ImageProcessWorkflowWorker(new VirtualConsole(this.ctlWorkflowConsole)).Start(this._cancellationSource.Token);
            new ImageActivityWorker(new VirtualConsole(this.ctlActivityConsole)).Start(this._cancellationSource.Token);

            this._startProcessor = new StartWorkflowExecutionProcessor(new VirtualConsole(this.ctlStartWorkflow));
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.DefaultExt = "jpg";
            openDialog.Filter = "JPEG Images (.jpg)|*.jpg|PNG Images (.png)|*.png";
            openDialog.Title = "Select Image to Process";

            if (openDialog.ShowDialog(this).GetValueOrDefault())
            {
                this._ctlImageToProcess.Text = openDialog.FileName;
            }
        }

        private void StartWorkflowExecution_Click(object sender, RoutedEventArgs evnt)
        {
            if (string.IsNullOrWhiteSpace(this._ctlS3Bucket.Text))
            {
                MessageBox.Show(this, "Setting the S3 Bucket is required before starting this work flow", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(this._ctlImageToProcess.Text))
            {
                MessageBox.Show(this, "Setting an image to process is required before starting this work flow", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!File.Exists(this._ctlImageToProcess.Text))
            {
                MessageBox.Show(this, "The image to process does not exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this._startProcessor.StartWorkflowExecution(this._ctlS3Bucket.Text, this._ctlImageToProcess.Text);
        }

        private bool IsConfigSet()
        {
            if (string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["AWSProfileName"]))
            {
                MessageBox.Show(this, "AWSProfileName is missing from app.config and must be set before running this application.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["AWSRegion"]))
            {
                MessageBox.Show(this, "AWSRegion is missing from app.config and must be set before running this application.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }
    }
}
