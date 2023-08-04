using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using Amazon.AWSToolkit.CommonUI.Converters;

using log4net;

namespace Amazon.AWSToolkit.CommonUI
{
    /// <summary>
    /// Interaction logic for ValidationErrorText.xaml
    /// </summary>
    public partial class ValidationErrorText : UserControl
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ValidationErrorText));

        public ValidationErrorText()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Identifies the SourceElement dependency property.
        /// </summary>
        public static readonly DependencyProperty SourceElementProperty = DependencyProperty.Register(
            nameof(SourceElement),
            typeof(DependencyObject),
            typeof(ValidationErrorText),
            new FrameworkPropertyMetadata(SourceElementProperty_PropertyChangedCallback));


        /// <summary>
        /// Gets or sets the control to listen to validation errors for.
        /// </summary>
        /// <remarks>
        /// While this property can be set any way a dependency property can, it is easy to using {Binding}
        /// and {x:Reference} XAML markup extensions to assign a control with an x:Name.  As the type of
        /// this property is DependencyProperty, it can listen to a wide-range of WPF elements, not just controls.
        /// </remarks>
        public DependencyObject SourceElement
        {
            get => (DependencyObject) GetValue(SourceElementProperty);
            set => SetValue(SourceElementProperty, value);
        }


        private static void SourceElementProperty_PropertyChangedCallback(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            // Bindings are set here instead of in XAML as the Source of these particular bindings must be the SourceElement and
            // there isn't a good way to do that with existing XAML markup extensions and constructs.  A word of warning before
            // considering setting the DataContext of this control to SourceElement, this will impact bindings in consumer code
            // of other properties of this control.  Those bindings will suddenly apply to the SourceElement rather than the
            // default DataContext (likely a view model) as the consumer would expect.

            var @this = (ValidationErrorText) d;

            BindingOperations.SetBinding(@this.LayoutGrid, Grid.VisibilityProperty,
                new Binding()
                {
                    Converter = new BooleanToVisibilityConverter(),
                    Mode = BindingMode.OneWay,
                    Path = new PropertyPath("(Validation.HasError)"),
                    Source = e.NewValue
                });
            BindingOperations.GetBindingExpression(@this.LayoutGrid, Grid.VisibilityProperty).UpdateTarget();

            BindingOperations.SetBinding(@this.ErrorItemsControl, ItemsControl.ItemsSourceProperty,
                new Binding()
                {
                    Mode = BindingMode.OneWay, Path = new PropertyPath("(Validation.Errors)"), Source = e.NewValue
                });
            BindingOperations.GetBindingExpression(@this.ErrorItemsControl, ItemsControl.ItemsSourceProperty)
                .UpdateTarget();
        }

        // This field must be public to be visible to the XAML processor.
        public readonly static RelayValueConverter.ConvertDelegate ConvertNullToCollapsed =
            (object value, Type targetType, object parameter, CultureInfo culture) =>
            {
                return value == null ? Visibility.Collapsed : Visibility.Visible;
            };
    }
}
