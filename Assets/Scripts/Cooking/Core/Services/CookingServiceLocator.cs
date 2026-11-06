using System;
using System.Collections.Generic;
using UnityEngine;

namespace CookAndDeliver.Cooking.Core.Services
{
    // Central service locator managing shared gameplay services
    public static class CookingServiceLocator
    {
        static readonly Dictionary<Type, IService> services = new Dictionary<Type, IService>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            services.Clear();
        }

        public static void Register<T>(T service) where T : class, IService
        {
            var type = typeof(T);
            if (services.ContainsKey(type))
            {
                services[type] = service;
            }
            else
            {
                services.Add(type, service);
            }
        }

        public static void Unregister<T>() where T : class, IService
        {
            var type = typeof(T);
            if (services.ContainsKey(type))
            {
                services.Remove(type);
            }
        }

        public static T Get<T>() where T : class, IService
        {
            var type = typeof(T);
            if (services.TryGetValue(type, out var service))
            {
                return service as T;
            }
            Debug.LogError($"[CookingServiceLocator] Service {type.Name} not found!");
            return null;
        }

        public static bool TryGet<T>(out T service) where T : class, IService
        {
            var type = typeof(T);
            if (services.TryGetValue(type, out var s))
            {
                service = s as T;
                return true;
            }
            service = null;
            return false;
        }
    }
}
