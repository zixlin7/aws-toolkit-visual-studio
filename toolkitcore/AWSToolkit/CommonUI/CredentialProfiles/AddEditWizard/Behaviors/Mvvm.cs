using System;
using System.Windows;
using System.Windows.Data;

using Amazon.AWSToolkit.Tasks;

using log4net;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Behaviors
{
    /// <summary>
    /// Provides attachable behaviors to support Model-View-ViewModel development in XAML.
    /// </summary>
    public static class Mvvm
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(Mvvm));

        #region ParentServiceProvider attached property

        /// <summary>
        /// Do not set directly in XAML.  Supports parent service provider handling in ServiceProvider.
        /// </summary>
        public static readonly DependencyProperty ParentServiceProviderProperty = DependencyProperty.RegisterAttached(
            "ParentServiceProvider",
            typeof(ServiceProvider),
            typeof(Mvvm),
            new FrameworkPropertyMetadata(ParentServiceProvider_PropertyChanged));

        private static void ParentServiceProvider_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var viewModel = GetViewModel(d);
            if (viewModel != null && e.NewValue is ServiceProvider serviceProvider)
            {
                if (viewModel.ServiceProvider == null)
                {
                    viewModel.ServiceProvider = serviceProvider;
                }
                else
                {
                    viewModel.ServiceProvider.ParentServiceProvider = serviceProvider;
                }

                // Call this method as soon as the ServiceProvider is available to ensure view models can register
                // their services early.
                viewModel.CallLifecycleMethod(ViewModel.LifecycleMethods.RegisterServicesAsync).LogExceptionAndForget();
            }
        }

        public static ServiceProvider GetParentServiceProvider(DependencyObject d)
        {
            return (ServiceProvider) d.GetValue(ParentServiceProviderProperty);
        }

        public static void SetParentServiceProvider(DependencyObject d, ServiceProvider value)
        {
            d.SetValue(ParentServiceProviderProperty, value);
        }
        #endregion

        #region ViewModel attached property

        /// <summary>
        /// Attaches the view model to the view and manages the view model lifecycle method calls.
        /// </summary>
        /// <remarks>
        /// View models may be created directly in XAML and this behavior will attach them to their views
        /// and manage the lifecycle calls for them.  View models should support parameterless public constructors
        /// and use the associated ServiceProvider for accessing dependencies.  Additional properties on the
        /// view model may be assigned in XAML as any object created in XAML.
        /// 
        /// This property is not bindable as it sets the DataContext of the view which would cause the binding
        /// to rebind to the new DataContext.  While careful binding to a source outside of the inherited
        /// DataContext could prevent this behavior, it's not worth the overhead and confusion.
        /// 
        /// See the ViewModel class for details on the view model lifecycle.
        /// </remarks>
        /// <example>
        /// <local:MyView
        ///     Background="ElectricBlue"
        ///     PropertyOnMyView="custom stuff">
        ///     <behaviors:Mvvm.ViewModel>
        ///         <local:MyViewModel
        ///             SomePropertyToSet="42" />
        ///     </behaviors:Mvvm.ViewModel>
        /// </local:MyView>
        /// </example>
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.RegisterAttached(
            "ViewModel",
            typeof(ViewModel),
            typeof(Mvvm),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.NotDataBindable, ViewModel_PropertyChanged));

        public static ViewModel GetViewModel(DependencyObject d)
        {
            return (ViewModel) d.GetValue(ViewModelProperty);
        }

        public static void SetViewModel(DependencyObject d, ViewModel value)
        {
            d.SetValue(ViewModelProperty, value);
        }

        private static void ViewModel_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = ToView(d);
            if (view == null)
            {
                return;
            }

            var viewModel = e.NewValue as ViewModel;
            if (e.NewValue != null)
            {
                view.DataContext = viewModel;
                view.Loaded -= View_Loaded;
                view.Loaded += View_Loaded;
                view.Unloaded -= View_Unloaded;
                view.Unloaded += View_Unloaded;

                if (GetParentServiceProvider(d) == null && view.GetBindingExpression(ParentServiceProviderProperty) == null)
                {
                    // Try to bind to parent's ServiceProvider
                    view.SetBinding(ParentServiceProviderProperty,
                        new Binding($"DataContext.{nameof(ViewModel.ServiceProvider)}")
                        {
                            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(FrameworkElement), 1)
                        });
                }

                if (viewModel.ServiceProvider != null)
                {
                    // Call this method as soon as the ServiceProvider is available to ensure view models can register
                    // their services early.
                    viewModel.CallLifecycleMethod(ViewModel.LifecycleMethods.RegisterServicesAsync).LogExceptionAndForget();
                }
            }
        }
        #endregion

        private static void View_Loaded(object sender, RoutedEventArgs e)
        {
            if (TryCallOnView(sender, v => v.IsVisibleChanged += View_IsVisibleChanged, out var view))
            {
                // By the time the loaded events start firing, all of the binding has completed, so all view models
                // have had RegisterServicesAsync applied.  Similarly, all initial ViewLoadedAsync calls will be made as
                // the events move through the visual tree.
                // https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/object-lifetime-events?view=netframeworkdesktop-4.8
                TryCallOnViewModel(view, ViewModel.LifecycleMethods.ViewLoadedAsync, out _);
            }
        }

        private static void View_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var method = (bool) e.NewValue ?
                ViewModel.LifecycleMethods.ViewShownAsync :
                ViewModel.LifecycleMethods.ViewHiddenAsync;

            TryCallOnViewModel(ToView(sender), method, out _);
        }

        private static void View_Unloaded(object sender, RoutedEventArgs e)
        {
            TryCallOnViewModel(ToView(sender), ViewModel.LifecycleMethods.ViewUnloadedAsync, out _);
        }

        private static bool TryCallOnView(object viewCandidate, Action<FrameworkElement> call, out FrameworkElement view)
        {
            view = ToView(viewCandidate);
            if (view == null)
            {
                _logger.Error("Unable to complete call on view.");
                return false;
            }

            call(view);
            return true;
        }

        private static bool TryCallOnViewModel(FrameworkElement view, ViewModel.LifecycleMethods method, out ViewModel viewModel)
        {
            viewModel = null;
            if (view == null)
            {
                _logger.Error("Unable to complete call on view model due to null view.");
                return false;
            }

            viewModel = GetViewModel(view);
            if (viewModel == null)
            {
                _logger.Error("Unable to get view model to complete call on view model.");
                return false;
            }

            viewModel.CallLifecycleMethod(method).LogExceptionAndForget();
            return true;
        }

        private static FrameworkElement ToView(object viewCandidate)
        {
            if (viewCandidate == null)
            {
                _logger.Error("Cannot convert null value to view.");
                return null;
            }

            if (!(viewCandidate is FrameworkElement view))
            {
                _logger.Error("View must derive directly or indirectly from FrameworkElement.");
                return null;
            }

            return view;
        }
    }
}
