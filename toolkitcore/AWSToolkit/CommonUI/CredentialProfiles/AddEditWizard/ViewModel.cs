using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using Amazon.AWSToolkit.Collections;
using Amazon.AWSToolkit.CommonUI.Converters;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Behaviors;
using Amazon.AWSToolkit.Context;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard
{
    /// <summary>
    /// Base view model for root view models (i.e. those typically associated with dialogs and windows)
    /// </summary>
    /// <remarks>
    /// As root view models are created from code rather than in XAML, they are responsible for setting
    /// up the ServiceProvider during construction.  This class is provided as a convenience that implements
    /// the pattern as well as an extension point for root view models in the future.
    ///
    /// While the view model currently doesn't support a mechanism for disposal, one way would be to provide
    /// a disposal service via ServiceProvider to all view models that can notify them when to clean up.  If
    /// such a mechanism were needed, support should be added in this class.
    /// </remarks>
    public abstract class RootViewModel : ViewModel
    {
        protected RootViewModel(ToolkitContext toolkitContext)
        {
            ServiceProvider = new ServiceProvider();
            ServiceProvider.SetService(toolkitContext);
        }
    }

    /// <summary>
    /// Base view model class that provides support for service provider and lifecycle methods.
    /// </summary>
    /// <remarks>
    /// A ServiceProvider is used to decouple references and calls between the various and
    /// deeply nested view models.  This avoids the need to rely on making chained calls up
    /// through parents or trying to rely on WPF features such as RoutedEvents and RoutedCommands
    /// that don't work well with view models.
    ///
    /// The lifecycle methods are managed by the Mvvm.ViewModel attachment to a view.  The lifecycle
    /// methods are:  RegisterServicesAsync, InitializeAsync, ViewLoadedAsync, ViewUnloadedAsync,
    /// and ViewVisibilityChangedAsync.  These are call asynchronously and do not block the view
    /// or XAML processing.  For more details, see the docs for each of these methods.
    /// </remarks>
    public abstract class ViewModel : BaseModel
    {
        private ServiceProvider _serviceProvider;

        /// <summary>
        /// The ServiceProvider used by the view model
        /// </summary>
        /// <remarks>
        /// The ServiceProvider can be used for dependency resolution, but is intended to be used to
        /// retrieve services to communicate with other view models in a view model graph.
        /// </remarks>
        public ServiceProvider ServiceProvider
        {
            get => _serviceProvider;
            set => SetProperty(ref _serviceProvider, value);
        }

        /// <summary>
        /// Returns an instance of ToolkitContext.
        /// </summary>
        /// <remarks>
        /// ToolkitContext is such an important object throughout the toolkit, it is always available
        /// to view models as a convenience.
        /// 
        /// Derived view model base classes may provide protected getters to commonly used services that
        /// may be used by many of their subclasses. These are typically added as expression-bodied members
        /// with the service type name (without the leading "I" for interfaces) as the property name.
        /// </remarks>
        public ToolkitContext ToolkitContext => ServiceProvider.RequireService<ToolkitContext>();

        /// <summary>
        /// Creates a new view model.
        /// </summary>
        /// <remarks>
        /// Root view models should provide a public contstructor that requires a ToolkitContext.  They
        /// should create a ServiceProvider and call SetService with the ToolkitContext.  These root
        /// view models are instantiated outside of XAML, but can be bound to their view via the Mvvm.ViewModel
        /// attached property.  This is necessary for the view model to participate in lifecycle calls.
        /// This can be performed with: <c>Mvvm.SetViewModel(view, viewModel);</c>
        ///
        /// For non-root view models, no constructor is needed, but if a constructor does exist, a
        /// parameterless public constructor must exist as well to support creation via XAML.
        ///
        /// The view model may perform initialization in the constructor, but non-root view models will not
        /// have access to a ServiceProvider at this point.  Root view models will as they are required
        /// to create and store a service provider in the ServiceProvider property.  Root view models may derive
        /// from RootViewModel as a convenience for this pattern.
        /// </remarks>
        protected ViewModel() { }

        internal enum LifecycleMethods
        {
            RegisterServicesAsync,
            /**
             * InitializeAsync
             * 
             * This is a pseudo-lifecycle method as it is implemented in CallLifecycleMethod to
             * be called on the first ViewLoaded event just prior to calling ViewLoadedAsync.
             * Mvvm.ViewModel processing is unaware of this lifecycle method.
             */
            ViewLoadedAsync,
            ViewShownAsync,
            ViewHiddenAsync,
            ViewUnloadedAsync
        }

        private readonly SemaphoreSlim _lifecycleSemaphore = new SemaphoreSlim(1, 1);

        private bool _initializeAsyncCalled;

        internal async Task CallLifecycleMethod(LifecycleMethods method)
        {
            try
            {
                await _lifecycleSemaphore.WaitAsync();

                switch (method)
                {
                    case LifecycleMethods.RegisterServicesAsync:
                        await RegisterServicesAsync();
                        break;
                    case LifecycleMethods.ViewLoadedAsync:
                        if (!_initializeAsyncCalled)
                        {
                            _initializeAsyncCalled = true;
                            await InitializeAsync();
                        }
                        await ViewLoadedAsync();
                        break;
                    case LifecycleMethods.ViewShownAsync:
                        await ViewShownAsync();
                        break;
                    case LifecycleMethods.ViewHiddenAsync:
                        await ViewHiddenAsync();
                        break;
                    case LifecycleMethods.ViewUnloadedAsync:
                        await ViewUnloadedAsync();
                        break;
                }
            }
            finally
            {
                _lifecycleSemaphore.Release();
            }
        }

        /// <summary>
        /// First call of lifecycle for view model to add services it provides to the service provider.
        /// </summary>
        /// <returns>A Task to be awaited.</returns>
        /// <exception cref="InvalidOperationException">Always unexpected condition; indicates a bug.</exception>
        /// <remarks>
        /// If derived view model classes will provide service(s), they should be registered via ServiceProvider.SetService
        /// in the override of this method.
        ///
        /// While services can be added to the ServiceProvider at any time, it is best to add them during the call to
        /// this method as this helps ensure that all services will be available to all view models in the graph by the
        /// time InitializeAsync is called.
        ///
        /// Aside from a RootViewModel, the ServiceProvider will not be available in the view model until this step in
        /// the lifecycle.
        /// 
        /// This method is guaranteed to be called only once for the lifetime of the view model instance.
        /// 
        /// When overriding this method, always call <c>await base.RegisterServicesAsync()</c> as the first instruction
        /// of the method body.
        /// </remarks>
        public virtual Task RegisterServicesAsync()
        {
            if (ServiceProvider == null)
            {
                throw new InvalidOperationException("ServiceProvider must already be initialized.");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Second call of lifecycle for the view model to bootstrap itself.
        /// </summary>
        /// <returns>A Task to be awaited.</returns>
        /// <remarks>
        /// Code that would typically go in a constructor or Initialize() method should be added here.  When this method is
        /// called all services added during the call to RegisterServicesAsync of all view models in the graph are now available.
        /// The view model can now start using services from the ServiceProvider (via calls to GetService/RequireService) to
        /// communicate to other view models.
        ///
        /// While initialization code may be added in a derived view model class constructor, it must be a public parameterless
        /// constructor and cannot rely upon the ServiceProvider or any services being available.  Typically, no constructor
        /// should be defined in a derived view model and bootstrapping of the view model should occur in this method.
        ///
        /// This method is guaranteed to be called only once for the lifetime of the view model instance.
        /// 
        /// When overriding this method, always call <c>await base.InitializeAsync()</c> as the first instruction of the method body.
        /// </remarks>
        public virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called during the lifecycle when the view is loaded.
        /// </summary>
        /// <returns>A Task to be awaited.</returns>
        /// <remarks>
        /// View models may take action here when the view is loaded such as waiting to access a resource until the view is ready to
        /// be interacted with, beginning activities that should only occur while the view is in use, etc.
        /// 
        /// This call may occur multiple times depending on what the view is embedded in.  For example, a view in a document in the
        /// document well will be loaded each time the document tab is opened and unloaded when a different document tab is selected.
        ///
        /// When overriding this method, always call <c>await base.ViewLoadedAsync()</c> as the first instruction of the method body.
        /// 
        /// See <see cref="https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/object-lifetime-events?view=netframeworkdesktop-4.8">
        /// Object Lifetime Events</see> for more details.
        /// </remarks>
        public virtual Task ViewLoadedAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called during the lifecycle when the view is shown. 
        /// </summary>
        /// <returns>A Task to be awaited.</returns>
        /// <remarks>
        /// Similar to loaded, but more lightweight, this call is made when the view becomes visible.  Visibility may become affected by
        /// changes to the Visbility property, the view being covered up by z-order, etc.  The view model may choose to take action
        /// when it is know that the user can see the view.
        ///
        /// This method may be called multiple times during the life of the view model.
        /// 
        /// When overriding this method, always call <c>await base.ViewShownAsync()</c> as the first instruction of the method body.
        /// 
        /// See <see cref="https://learn.microsoft.com/en-us/dotnet/api/system.windows.uielement.isvisible?view=netframework-4.7.2">
        /// UIElement.IsVisible Property</see> for more details.
        /// </remarks>
        public virtual Task ViewShownAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called during the lifecycle when the view is hidden. 
        /// </summary>
        /// <returns>A Task to be awaited.</returns>
        /// <remarks>
        /// Similar to unloaded, but more lightweight, this call is made when the view becomes invisible.  Visibility may become affected by
        /// changes to the Visbility property, the view being covered up by z-order, etc.  The view model may choose to take action
        /// when it is know that the user cannot see the view, even if temporarily.
        /// 
        /// This method may be called multiple times during the life of the view model.
        /// 
        /// When overriding this method, always call <c>await base.ViewHiddenAsync()</c> as the first instruction of the method body.
        /// 
        /// See <see cref="https://learn.microsoft.com/en-us/dotnet/api/system.windows.uielement.isvisible?view=netframework-4.7.2">
        /// UIElement.IsVisible Property</see> for more details.
        /// </remarks>
        public virtual Task ViewHiddenAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called during the lifecycle when the view is unloaded.
        /// </summary>
        /// <returns>A Task to be awaited.</returns>
        /// <remarks>
        /// View models may take action here when the view is unloaded such as stopping long running processes, releasing access
        /// to resources, cleaning up temporary files, etc.
        ///
        /// This call should not be considered to terminate the lifecycle of the view model unless the view is guaranteed to be
        /// embedded in a containing control that only unloads once.  Consider better options for making view models aware that they
        /// can dispose of themselves such as a service from the RootViewModel that notifies when the view model graph is being
        /// released/torn-down.
        /// 
        /// This call may occur multiple times depending on what the view is embedded in.  For example, a view in a document in the
        /// document well will be loaded each time the document tab is opened and unloaded when a different document tab is selected.
        ///
        /// When overriding this method, always call <c>await base.ViewUnloadedAsync()</c> as the first instruction of the method body.
        /// 
        /// See <see cref="https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/object-lifetime-events?view=netframeworkdesktop-4.8">
        /// Object Lifetime Events</see> for more details.
        /// </remarks>
        public virtual Task ViewUnloadedAsync()
        {
            return Task.CompletedTask;
        }

        // Provide standard converters used across many view models
        /// <summary>
        /// Converts null to false and anything else to true.
        /// </summary>
        public static readonly RelayValueConverter.ConvertDelegate NullToFalse =
            (object value, Type targetType, object parameter, CultureInfo culture) =>
            value != null;

        /// <summary>
        /// Returns the view model associated with the view when Mvvm.ViewModel used.
        /// </summary>
        public static readonly RelayValueConverter.ConvertDelegate ViewToViewModel =
            (object value, Type targetType, object parameter, CultureInfo culture) =>
            {
                if (value is IEnumerable items)
                {
                    var viewModels = new ObservableCollection<ViewModel>();
                    viewModels.AddAll(items.Cast<DependencyObject>().Select(view => Mvvm.GetViewModel(view)));
                    return viewModels;
                }
                else
                {
                    return Mvvm.GetViewModel(value as DependencyObject);
                }
            };

        /// <summary>
        /// Negates the provided boolean value.
        /// </summary>
        public static readonly RelayValueConverter.ConvertDelegate NegateBool =
            (object value, Type targetType, object parameter, CultureInfo culture) =>
            !(bool) value;

        /// <summary>
        /// Converts a boolean true/false to a Visibility Visible/Hidden respectively.
        /// </summary>
        public static readonly RelayValueConverter.ConvertDelegate FalseToHidden =
            (object value, Type targetType, object parameter, CultureInfo culture) =>
            ((bool) value) ? Visibility.Visible : Visibility.Hidden;
    }
}
