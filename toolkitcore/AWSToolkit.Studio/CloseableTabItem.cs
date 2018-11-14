using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Shared;

namespace Amazon.AWSToolkit.Studio
{
    public class CloseableTabItem : TabItem
    {
        static CloseableTabItem()
        {
            //This OverrideMetadata call tells the system that this element wants to provide a style that is different than its base class.
            //This style is defined in themes\ClosableTabItem.xaml
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CloseableTabItem),
                new FrameworkPropertyMetadata(typeof(CloseableTabItem)));
        }

        public static readonly RoutedEvent CloseTabEvent =
            EventManager.RegisterRoutedEvent("CloseTab", RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(CloseableTabItem));

        public event RoutedEventHandler CloseTab
        {
            add { AddHandler(CloseTabEvent, value); }
            remove { RemoveHandler(CloseTabEvent, value); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            Button closeButton = base.GetTemplateChild("PART_Close") as Button;
            if (closeButton != null)
                closeButton.Click += new System.Windows.RoutedEventHandler(closeButton_Click);
        }

        void closeButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.RaiseEvent(new RoutedEventArgs(CloseTabEvent, this));
        }

        public void Add(IAWSToolkitControl editorControl)
        {
            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.Children.Add(editorControl.UserControl);
            Grid.SetColumn(editorControl.UserControl, 0);
            Grid.SetRow(editorControl.UserControl, 0);

            editorControl.UserControl.Width = double.NaN;
            editorControl.UserControl.Height = double.NaN;
            editorControl.UserControl.VerticalContentAlignment = VerticalAlignment.Stretch;
            editorControl.UserControl.HorizontalContentAlignment = HorizontalAlignment.Stretch;

            this.Header = editorControl.Title;
            this.Content = grid;

            HostedEditor = editorControl;
        }

        public IAWSToolkitControl HostedEditor
        {
            get;
            private set;
        }
    }
}
