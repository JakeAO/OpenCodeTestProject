using System;
using System.Collections.Generic;

namespace OCTP.Core
{
    /// <summary>
    /// Thread-safe Service Locator for managing and accessing game services.
    /// Provides centralized service registration and retrieval.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, IGameService> _services = new Dictionary<Type, IGameService>();
        private static readonly object _lock = new object();

        /// <summary>
        /// Registers a service instance with the locator.
        /// </summary>
        /// <typeparam name="T">The service type to register.</typeparam>
        /// <param name="service">The service instance to register.</param>
        /// <exception cref="ArgumentNullException">Thrown when service is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when a service of this type is already registered.</exception>
        public static void Register<T>(T service) where T : IGameService
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            lock (_lock)
            {
                Type serviceType = typeof(T);
                if (_services.ContainsKey(serviceType))
                {
                    throw new InvalidOperationException($"Service of type {serviceType.Name} is already registered.");
                }

                _services[serviceType] = service;
            }
        }

        /// <summary>
        /// Registers a service instance with the locator.
        /// </summary>
        /// <param name="service">The service instance to register.</param>
        /// <param name="serviceTypes">The service types to register the instance as.</param>
        /// <exception cref="ArgumentNullException">Thrown when service is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when a service of this type is already registered.</exception>
        public static void Register(object service, params Type[] serviceTypes)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if(!typeof(IGameService).IsAssignableFrom(service.GetType()))
            {
                throw new ArgumentException($"Service instance of type {service.GetType().Name} does not implement IGameService.");
            }

            lock (_lock)
            {
                foreach (Type serviceType in serviceTypes)
                {
                    if(!serviceType.IsAssignableFrom(service.GetType()))
                    {
                        throw new ArgumentException($"Service instance of type {service.GetType().Name} cannot be assigned to {serviceType.Name}.");
                    }

                    if (_services.ContainsKey(serviceType))
                    {
                        throw new InvalidOperationException($"Service of type {serviceType.Name} is already registered.");
                    }

                    _services[serviceType] = (IGameService)service;
                }
            }
        }

        /// <summary>
        /// Retrieves a registered service instance.
        /// </summary>
        /// <typeparam name="T">The service type to retrieve.</typeparam>
        /// <returns>The registered service instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the service is not registered.</exception>
        public static T Get<T>() where T : IGameService
        {
            lock (_lock)
            {
                Type serviceType = typeof(T);
                if (!_services.TryGetValue(serviceType, out IGameService service))
                {
                    throw new InvalidOperationException($"Service of type {serviceType.Name} is not registered.");
                }

                return (T)service;
            }
        }

        /// <summary>
        /// Attempts to retrieve a registered service instance.
        /// </summary>
        /// <typeparam name="T">The service type to retrieve.</typeparam>
        /// <param name="service">The retrieved service instance, or default if not found.</param>
        /// <returns>True if the service was found; otherwise, false.</returns>
        public static bool TryGet<T>(out T service) where T : IGameService
        {
            lock (_lock)
            {
                Type serviceType = typeof(T);
                if (_services.TryGetValue(serviceType, out IGameService gameService))
                {
                    service = (T)gameService;
                    return true;
                }

                service = default;
                return false;
            }
        }

        /// <summary>
        /// Checks if a service of the specified type is registered.
        /// </summary>
        /// <typeparam name="T">The service type to check.</typeparam>
        /// <returns>True if the service is registered; otherwise, false.</returns>
        public static bool Has<T>() where T : IGameService
        {
            lock (_lock)
            {
                return _services.ContainsKey(typeof(T));
            }
        }

        /// <summary>
        /// Clears all registered services. Primarily used for testing and cleanup.
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _services.Clear();
            }
        }
    }
}
