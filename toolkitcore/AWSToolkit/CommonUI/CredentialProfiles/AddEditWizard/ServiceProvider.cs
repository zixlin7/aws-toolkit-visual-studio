using System;
using System.Collections.Generic;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard
{
    /// <summary>
    /// Provides a basic service provider for ViewModel classes to share services between.
    /// </summary>
    /// <remarks>
    /// This class provides a basic implementation of a service provider and follows the pattern
    /// and naming convention of many IoT/DI containers.  It could be replaced with one of those
    /// in the future with little modification.
    ///
    /// While this class supports IServiceProvider for completeness, the more specific overloads
    /// of GetService are more commonly used.  Additionally, there are methods for adding services
    /// to the provider.  The service provider does not currently support resolution through a
    /// service provider hierarchy as some do, but it can be added in the future if needed.
    /// The service provider does not currently support removing/unregistering services now either
    /// to keep it simple as it is not a requirement.  This could potentially be added in the future
    /// as well.
    ///
    /// Services may be referenced by type alone or by type and name when multiple instances of the
    /// same type are registered with the provider.
    ///
    /// See the ViewModel class for further details on how the ServiceProvider is used.
    /// </remarks>
    public class ServiceProvider : IServiceProvider
    {
        private readonly IDictionary<string, object> _services = new Dictionary<string, object>();

        /// <summary>
        /// If requested service not found, attempts to return from parent service provider.
        /// </summary>
        /// <remarks>
        /// This behavior chains so that each service provider will continue looking to its
        /// parent service provider for a service until null (i.e. root service provider) is
        /// encountered.  If null, no additional attempts are made to return requested service.  
        /// </remarks>
        public ServiceProvider ParentServiceProvider { get; set; }

        public ServiceProvider() { }

        private string ToKey(Type type, string name)
        {
            return $"{type.AssemblyQualifiedName}${name}";
        }

        /// <summary>
        /// Attempts to return the requested service by type and optional name.
        /// </summary>
        /// <typeparam name="T">Type of service to return.</typeparam>
        /// <param name="name">Optional.  Name of service to return.</param>
        /// <returns>Service instance if found, otherwise null.</returns>
        /// <remarks>
        /// If a service must exist or an exception should be thrown, use a RequireService overload instead.
        /// </remarks>
        public T GetService<T>(string name = null)
        {
            return (T) GetService(typeof(T), name);
        }

        /// <summary>
        /// Attempts to return the requested service by type.
        /// </summary>
        /// <param name="serviceType">Type of service to return.</param>
        /// <returns>Service instance if found, otherwise null.</returns>
        /// <remarks>
        /// If a service must exist or an exception should be thrown, use a RequireService overload instead.
        /// This overload implements IServiceProvider.GetService(Type serviceType).
        /// <see cref="https://learn.microsoft.com/en-us/dotnet/api/system.iserviceprovider.getservice?view=netframework-4.7.2"/>
        /// </remarks>
        public object GetService(Type serviceType)
        {
            return GetService(serviceType, null);
        }

        /// <summary>
        /// Attempts to return the requested service by type and optional name.
        /// </summary>
        /// <param name="serviceType">Type of service to return.</param>
        /// <param name="name">Optional.  Name of service to return.</param>
        /// <returns>Service instance if found, otherwise null.</returns>
        /// <remarks>
        /// If a service must exist or an exception should be thrown, use a RequireService overload instead.
        /// </remarks>
        public virtual object GetService(Type serviceType, string name = null)
        {
            ThrowOnNullServiceType(serviceType);
            return _services.TryGetValue(ToKey(serviceType, name), out var service) ?
                service :
                ParentServiceProvider?.GetService(serviceType, name);
        }

        /// <summary>
        /// Attempts to return the requested service by type and optional name or throws exception if not found.
        /// </summary>
        /// <typeparam name="T">Type of service to return.</typeparam>
        /// <param name="name">Optional.  Name of service to return.</param>
        /// <returns>Service instance if found, otherwise throws NotImplementedException.</returns>
        /// <remarks>
        /// If a service is optional, use a GetService overload that will return null if the service isn't found.
        /// </remarks>
        public T RequireService<T>(string name = null)
        {
            return (T) RequireService(typeof(T), name);
        }

        /// <summary>
        /// Attempts to return the requested service by type and optional name or throws exception if not found.
        /// </summary>
        /// <param name="serviceType">Type of service to return.</param>
        /// <param name="name">Optional.  Name of service to return.</param>
        /// <returns>Service instance if found, otherwise throws NotImplementedException.</returns>
        /// <remarks>
        /// If a service is optional, use a GetService overload that will return null if the service isn't found.
        /// </remarks>
        public object RequireService(Type serviceType, string name = null)
        {
            ThrowOnNullServiceType(serviceType);
            return GetService(serviceType, name) ?? throw new NotImplementedException($"Service {serviceType.Name} is not found.");
        }

        /// <summary>
        /// Registers a service by type and optional name.
        /// </summary>
        /// <param name="serviceType">Type of service to return.</param>
        /// <param name="name">Optional.  Name of service to return.</param>
        /// <remarks>
        /// If a service with the same type and optional name already exists, it will be overwritten.
        /// 
        /// If that behavior is not desireable, consider adding a new set of overloads such as
        /// TrySetService that will fail to add the service if it already exists.
        /// </remarks>
        public void SetService<T>(T service, string name = null)
        {
            SetService(typeof(T), service, name);
        }

        /// <summary>
        /// Registers a service by type and optional name.
        /// </summary>
        /// <param name="serviceType">Type of service to return.</param>
        /// <param name="name">Optional.  Name of service to return.</param>
        /// <remarks>
        /// If a service with the same type and optional name already exists, it will be overwritten.
        /// 
        /// If that behavior is not desireable, consider adding a new set of overloads such as
        /// TrySetService that will fail to add the service if it already exists.
        /// </remarks>
        public virtual void SetService(Type serviceType, object service, string name = null)
        {
            ThrowOnNullServiceType(serviceType);

#pragma warning disable IDE0016 // Null check simplification would case adding null service to _services
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }
#pragma warning restore IDE0016 // Null check simplification would case adding null service to _services

            _services[ToKey(serviceType, name)] = service;
        }

        protected void ThrowOnNullServiceType(Type serviceType)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }
        }
    }
}
