using System;
using System.Collections.Generic;

namespace Game.Unity
{
    /// Minimal service locator (no DI framework required).
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();

        public static void Register<T>(T instance) where T : class =>
            _services[typeof(T)] = instance;

        public static T Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var svc)) return (T)svc;
            throw new Exception($"[ServiceLocator] Service not registered: {typeof(T).Name}");
        }

        public static bool TryGet<T>(out T result) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var svc)) { result = (T)svc; return true; }
            result = null;
            return false;
        }

        public static void Clear() => _services.Clear();
    }
}
