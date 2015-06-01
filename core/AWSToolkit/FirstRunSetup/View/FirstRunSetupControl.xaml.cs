using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.FirstRunSetup.Model;
using Amazon.AWSToolkit.FirstRunSetup.Controller;
using System.Diagnostics;
using System.Timers;
using log4net;

namespace Amazon.AWSToolkit.FirstRunSetup.View
{
    /// <summary>
    /// Interaction logic for FirstRunSetupControl.xaml
    /// </summary>
    public partial class FirstRunSetupControl : BaseAWSControl
    {
        readonly FirstRunSetupController _controller;
        readonly ILog _logger = LogManager.GetLogger(typeof(FirstRunSetupControl));

        public FirstRunSetupControl()
            : this(null)
        {
        }

        public FirstRunSetupControl(FirstRunSetupController controller)
        {
            this._controller = controller;
            this.DataContext = this._controller.Model;

            string toolkitLocation = System.IO.Path.GetDirectoryName(this.GetType().Assembly.Location);
            try
            {
                InitializeComponent();

                _mediaElement.Source = new Uri(System.IO.Path.Combine(toolkitLocation, this._controller.Model.MediaContent), UriKind.Absolute);
                _mediaElement.Play();
            }
            catch (Exception e)
            {
                _logger.ErrorFormat("Caught exception trying to load and play intro video, exception details: {0}", e);
                _mediaElement.Visibility = Visibility.Hidden;
                _playbackErrorMessage.Visibility = Visibility.Visible;
            }
        }

        public override bool Validated()
        {
            var model = this._controller.Model;
            return !string.IsNullOrEmpty(model.AccessKey) && !string.IsNullOrEmpty(model.SecretKey) && !string.IsNullOrEmpty(model.DisplayName);
        }

        public override bool OnCommit()
        {
            this._controller.Persist();
            return true;
        }

        public bool HasMediaPlayerSited
        {
            get { return _mediaElement != null; }    
        }

        void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this._controller.Model.OpenExplorerOnExit = false;
            ShutdownWindowResources();
            Window.GetWindow(this).Close();
        }

        void AWSConsoleLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo((sender as Hyperlink).NavigateUri.ToString()));
            e.Handled = true;
        }

        void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            this._controller.Persist();
            ShutdownWindowResources();
            Window.GetWindow(this).Close();
        }

        void GetStartedButton_Click(object sender, RoutedEventArgs evt)
        {
            try
            {
                if (_mediaElement != null)
                    _mediaElement.Pause();
            }
            catch (Exception e)
            {
                _logger.ErrorFormat("Caught exception trying to pause intro video, exception details: {0}", e);
            }

            // should animate this...
            _panel1.Visibility = Visibility.Hidden;
            _panel2.Visibility = Visibility.Visible;
        }

        void ShutdownWindowResources()
        {
            try
            {
                if (_mediaElement != null)
                {
                    _mediaElement.Stop();
                    _mediaElement.Close();
                }
            }
            catch (Exception e)
            {
                _logger.ErrorFormat("Caught exception trying to stop and close intro video, exception details: {0}", e);
            }
        }

        private void OnFieldTextChanged(object sender, TextChangedEventArgs e)
        {
            // this can fire during setup binding, before btn exists
            if (IsInitialized)
                _saveAndCloseBtn.IsEnabled = this.Validated();
        }

        // When the media playback is finished. Stop() the media to seek to media start
        // and restart
        private void Element_MediaEnded(object sender, EventArgs e)
        {
            try
            {
                // if we got the event we obviously got a media element on construction, 
                // so no null check needed here
                _mediaElement.Stop();
                _mediaElement.Play();
            }
            catch
            {
                // not going to log this one, since we should be able to replay if we started the vid
            }
        }

        private void _backToVideoBtn_Click(object sender, RoutedEventArgs e)
        {
            // should animate this...
            _panel2.Visibility = Visibility.Hidden;
            _panel1.Visibility = Visibility.Visible;

            if (_mediaElement != null && _mediaElement.Visibility == Visibility.Visible)
                _mediaElement.Play();
        }
    }
}
