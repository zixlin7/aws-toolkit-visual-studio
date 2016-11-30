using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.EC2.LaunchWizard.PageControllers;
using Amazon.AWSToolkit.EC2.Model;

namespace Amazon.AWSToolkit.EC2.LaunchWizard.PageUI
{
    /// <summary>
    /// Interaction logic for StoragePage.xaml
    /// </summary>
    public partial class StoragePage
    {
        public StoragePage()
        {
            InitializeComponent();
        }

        public StoragePage(IAWSWizardPageController controller)
            : this()
        {
            this.PageController = controller as StoragePageController;
            DataContext = PageController.Model;
        }

        public StoragePageController PageController { get; set; }

        private void StorageVolumeDelete_OnClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            // even though IsSynchronizedWithCurrentItem is set for the list, I've noticed that
            // the current item isn't consistent when clicking the delete button, so I've relied
            // on binding the volume's temp id to the button tag
            var volumeId = (sender as Button).Tag as string;
            PageController.Model.RemoveVolume(volumeId);
        }

        private void AddVolume_OnClick(object sender, RoutedEventArgs e)
        {
            PageController.Model.AddVolume();
        }

        private void VolumeType_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var currentItem = _storageList.Items.CurrentItem;
            if (currentItem == null)
            {
                if (_storageList.Items.Count == 1) // effect of root entry being added on page startup
                    currentItem = _storageList.Items[0];
                else
                    return;
            }

            // manually updating the visibility of the iops fields via the data template was easier than 
            // getting the binding expressions to work!
            var currentListBoxItem = (ListBoxItem)(_storageList.ItemContainerGenerator.ContainerFromItem(currentItem));
            var dataContext = currentListBoxItem.DataContext as InstanceLaunchStorageVolume;

            var contentPresenter = UIUtils.FindVisualChild<ContentPresenter>(currentListBoxItem);
            var dataTemplate = contentPresenter.ContentTemplate;

            // null control instances can happen as controls are instantiated when adding new listbox item, we use bindings to
            // do the initial setup then this event handler to manage subsequent change
            var nonEditableIopsControl = (TextBlock)dataTemplate.FindName("nonEditableIops", contentPresenter);
            var editableIopsControl = (TextBox)dataTemplate.FindName("editableIops", contentPresenter);

            if (nonEditableIopsControl != null)
            {
                nonEditableIopsControl.Text = dataContext.StaticIops; // cannot get binding to work :-(
                nonEditableIopsControl.Visibility = dataContext.IsIopsCompatibleDevice ? Visibility.Hidden : Visibility.Visible;
            }

            if (editableIopsControl != null)
                editableIopsControl.Visibility = dataContext.IsIopsCompatibleDevice ? Visibility.Visible : Visibility.Hidden;
        }
    }
}
