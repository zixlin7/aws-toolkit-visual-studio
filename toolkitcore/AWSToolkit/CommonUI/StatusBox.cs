using System;
using System.Windows;
using System.Windows.Controls;

namespace Amazon.AWSToolkit.CommonUI
{
    public class StatusBox : ContentControl
    {
        static StatusBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(StatusBox), new FrameworkPropertyMetadata(typeof(StatusBox)));
        }

        private static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
            nameof(Status),
            typeof(bool?),
            typeof(StatusBox),
            new FrameworkPropertyMetadata()
            {
                BindsTwoWayByDefault = true,
                DefaultValue = null
            });

        public bool? Status
        {
            get => (bool?) GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }
    }
}
