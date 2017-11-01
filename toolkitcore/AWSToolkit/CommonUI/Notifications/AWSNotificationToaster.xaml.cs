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
using System.Windows.Media.Animation;
using System.Windows.Shapes;

using System.Windows.Forms;
using System.Drawing;

namespace Amazon.AWSToolkit.CommonUI.Notifications
{
    /// <summary>
    /// Presents a fade-in/out 'toaster' notification above the Windows task
    /// notification area.
    /// </summary>
    public partial class AWSNotificationToaster : Window
    {
        DoubleAnimation _fadeInAnimation;
        DoubleAnimation _fadeOutAnimation;
        bool _initialActivation = true;

        TimeSpan _fadeOutDelayStart = TimeSpan.Parse("0:0:2");
        Duration _fadeInDuration = new Duration(TimeSpan.Parse("0:0:2"));
        // use a slower fade-out so user has chance to hover over link and cancel fade
        Duration _fadeOutDuration = new Duration(TimeSpan.Parse("0:0:4"));

        public AWSNotificationToaster()
        {
            InitializeComponent();
            var logo = ThemeUtil.GetLogoImageSource("logo_aws_small");
            if (logo != null)
            {
                _ctlLogo.Source = logo;
            }
        }

        public void ShowNotification(FrameworkElement toasterContent, string headerText)
        {
            this._header.Text = headerText;
            this._toasterContent.Content = toasterContent;

            System.Drawing.Rectangle rc = Screen.PrimaryScreen.WorkingArea;
            Left = rc.Right - (Width + 10);
            Top = rc.Bottom - (Height + 10);

            Show();
        }

        public void ShowNotification(FrameworkElement toasterContent, string headerText, Duration fadeInDuration)
        {
            this._fadeInDuration = fadeInDuration;
            ShowNotification(toasterContent, headerText);
        }

        private void awsToaster_Activated(object sender, EventArgs e)
        {
            // get this even if user clicks the toaster, as well as due to our
            // initial Show() - starting animation after Show() doesn't work
            if (_initialActivation)
            {
                _initialActivation = false;
                StartFadeIn();
            }
        }

        private void StartFadeIn()
        {
            _fadeInAnimation = new DoubleAnimation();
            _fadeInAnimation.From = 0.0;
            _fadeInAnimation.To = 1.0;
            _fadeInAnimation.AutoReverse = false;
            _fadeInAnimation.Duration = this._fadeInDuration;
            _fadeInAnimation.AccelerationRatio = 1;
            _fadeInAnimation.Completed += new EventHandler(FadeInAnimation_Completed);

            BeginAnimation(Window.OpacityProperty, _fadeInAnimation);
        }

        private void StartDelayedFadeOut()
        {
            _fadeOutAnimation = new DoubleAnimation();
            _fadeOutAnimation.From = 1.0;
            _fadeOutAnimation.To = 0.0;
            _fadeOutAnimation.AutoReverse = false;
            _fadeOutAnimation.Duration = this._fadeOutDuration;
            // allow the toaster to remain fully visible for a short time before fade commences,
            // but fade without acceleration to give the user extra time to retain us if needed
            _fadeOutAnimation.BeginTime = this._fadeOutDelayStart;
            _fadeOutAnimation.Completed += new EventHandler(FadeOutAnimation_Completed);

            BeginAnimation(Window.OpacityProperty, _fadeOutAnimation);
        }

        void FadeInAnimation_Completed(object sender, EventArgs e)
        {
            _fadeInAnimation = null;
            if (!IsMouseOver)
                StartDelayedFadeOut();
        }

        void FadeOutAnimation_Completed(object sender, EventArgs e)
        {
            _fadeOutAnimation = null;
            if (!IsMouseOver)
                Close();
        }

        private void awsToaster_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // cancelling an animation including stopping timeline is somewhat non-intuitive
            // but this seems to work with least harm!
            if (_fadeInAnimation != null || _fadeOutAnimation != null)
            {
                BeginAnimation(Window.OpacityProperty, null);
                _fadeInAnimation = null;
                _fadeOutAnimation = null;
            }

            this.Opacity = 1.0;
        }

        private void awsToaster_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            StartDelayedFadeOut();
        }
    }
}
