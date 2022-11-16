using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

using Amazon.AWSToolkit.CommonUI.Converters;

using log4net;

namespace Amazon.AWSToolkit.CommonUI.Components
{
    /// <summary>
    /// Represents a control that can be used to display validation error messages of another control.
    /// </summary>
    /// <remarks>
    /// This control allows for easy siting and automated display of validation error messages for another
    /// DependencyObject-derived type.  The left-hand icon can be customized and a retry hyperlink is displayed
    /// if a retry ICommand is set.  The SourceElement allows {Binding} or {x:Reference} assignment of the
    /// control that this control should display validation errors for.  This control can display multiple
    /// error messages.
    ///
    /// Error messages are listened for on the SourceElement using the WPF Validation class.  While validation
    /// rules can be used in XAML, this class also works well with the BaseModel.DataErrorInfo property that
    /// provides IDataErrorInfo/INotifyDataErrorInfo functionality to all view models derived from BaseModel.
    /// </remarks>
    /// <seealso cref="https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/data-binding-overview?view=netframeworkdesktop-4.8#data-validation"/>
    /// <seealso cref="https://learn.microsoft.com/en-us/dotnet/api/system.windows.controls.validation?view=netframework-4.7.2"/>
    public partial class ValidationErrorsBox : UserControl
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ValidationErrorsBox));

        /// <summary>
        /// Identifies the Icon dependency property.
        /// </summary>
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
            nameof(Icon),
            typeof(Image),
            typeof(ValidationErrorsBox));

        /// <summary>
        /// Identifies the RetryCommand dependency property.
        /// </summary>
        public static readonly DependencyProperty RetryCommandProperty = DependencyProperty.Register(
            nameof(RetryCommand),
            typeof(ICommand),
            typeof(ValidationErrorsBox));

        /// <summary>
        /// Identifies the SourceElement dependency property.
        /// </summary>
        public static readonly DependencyProperty SourceElementProperty = DependencyProperty.Register(
            nameof(SourceElement),
            typeof(DependencyObject),
            typeof(ValidationErrorsBox),
            new FrameworkPropertyMetadata(SourceElementProperty_PropertyChangedCallback));

        /// <summary>
        /// Get or sets the icon to display on the left-hand side of the error messages.
        /// </summary>
        public Image Icon
        {
            get => (Image) GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <summary>
        /// Get or sets the ICommand to perform a retry on validation error via a user-clickable hyperlink.
        /// </summary>
        /// <remarks>
        /// If this property is not set, the retry hyperlink is not displayed.  This is desired behavior for
        /// terminal failures and cases where the user is entirely responsible for resolving the validation
        /// error such as invalid input.
        /// </remarks>
        public ICommand RetryCommand
        {
            get => (ICommand) GetValue(RetryCommandProperty);
            set => SetValue(RetryCommandProperty, value);
        }

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

        /// <summary>
        /// Initializes a new instance of the ValidationErrorsBox class.
        /// </summary>
        public ValidationErrorsBox()
        {
            InitializeComponent();
        }

        private static void SourceElementProperty_PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Bindings are set here instead of in XAML as the Source of these particular bindings must be the SourceElement and
            // there isn't a good way to do that with existing XAML markup extensions and constructs.  A word of warning before
            // considering setting the DataContext of this control to SourceElement, this will impact bindings in consumer code
            // of other properties of this control.  Those bindings will suddenly apply to the SourceElement rather than the
            // default DataContext (likely a view model) as the consumer would expect.

            var @this = (ValidationErrorsBox) d;

            BindingOperations.SetBinding(@this.LayoutGrid, Grid.VisibilityProperty, new Binding()
            {
                Converter = new BooleanToVisibilityConverter(),
                Mode = BindingMode.OneWay,
                Path = new PropertyPath("(Validation.HasError)"),
                Source = e.NewValue
            });
            BindingOperations.GetBindingExpression(@this.LayoutGrid, Grid.VisibilityProperty).UpdateTarget();

            BindingOperations.SetBinding(@this.ErrorItemsControl, ItemsControl.ItemsSourceProperty, new Binding()
            {
                Mode = BindingMode.OneWay,
                Path = new PropertyPath("(Validation.Errors)"),
                Source = e.NewValue
            });
            BindingOperations.GetBindingExpression(@this.ErrorItemsControl, ItemsControl.ItemsSourceProperty).UpdateTarget();
        }

        // This field must be public to be visible to the XAML processor.
        public readonly static RelayValueConverter.ConvertDelegate ConvertNullToCollapsed =
            (object value, Type targetType, object parameter, CultureInfo culture) =>
            {
                return value == null ? Visibility.Collapsed : Visibility.Visible;
            };
    }
}
