using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Publish.ViewModels;

namespace Amazon.AWSToolkit.Publish.Views
{
    /// <summary>
    /// View panel that is shown while a publish is in progress.
    /// Data bound to <see cref="PublishProjectViewModel"/>
    /// </summary>
    public partial class PublishApplicationView : UserControl
    {
        public PublishApplicationView()
        {
            InitializeComponent();

            Loaded += PublishApplicationView_Loaded;
            Unloaded += PublishApplicationView_Unloaded;
        }

        private void PublishApplicationView_Loaded(object sender, RoutedEventArgs e)
        {
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private void PublishApplicationView_Unloaded(object sender, RoutedEventArgs e)
        {
            CompositionTarget.Rendering -= CompositionTarget_Rendering;
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            // For UI performance reasonse, refresh the publish output messages once per frame instead of with each text change
            // Reference: https://blog.benoitblanchon.fr/wpf-high-speed-mvvm/
            if (DataContext is PublishProjectViewModel vm)
            {
                vm.RefreshMessages();
            }
            else
            {
#if DEBUG
                if (Debugger.IsAttached)
                {
                    Debug.Assert(false, $"The DataContext for this view is not the expected type. Wanted PublishProjectViewModel, got {DataContext?.GetType()}");
                }
#endif
            }
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!(sender is DependencyObject control))
            {
                return;
            }

            var scrollViewer = UIUtils.FindVisualParent<ScrollViewer>(control);

            scrollViewer?.ScrollToEnd();
        }
    }
}
