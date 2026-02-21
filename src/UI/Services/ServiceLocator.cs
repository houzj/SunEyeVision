using System;
using SunEyeVision.Core.Events;
using SunEyeVision.UI.Services;

namespace SunEyeVision.UI.Services
{
    /// <summary>
    /// 简单的服务定位器（单例模式）
    /// </summary>
    public class ServiceLocator
    {
        private static readonly Lazy<ServiceLocator> _instance = new Lazy<ServiceLocator>(() => new ServiceLocator());
        public static ServiceLocator Instance => _instance.Value;

        private readonly System.Collections.Generic.Dictionary<Type, object> _services = new System.Collections.Generic.Dictionary<Type, object>();

        private ServiceLocator()
        {
            // 注册默认服务
            Register<IEventBus>(new EventBus(new ConsoleLogger()));
            Register<UIEventPublisher>(new UIEventPublisher(GetService<IEventBus>()));
        }

        public void Register<T>(T service) where T : class
        {
            var serviceType = typeof(T);
            if (_services.ContainsKey(serviceType))
            {
                _services[serviceType] = service;
            }
            else
            {
                _services.Add(serviceType, service);
            }
        }

        public T GetService<T>() where T : class
        {
            var serviceType = typeof(T);
            if (_services.TryGetValue(serviceType, out var service))
            {
                return service as T;
            }
            return null;
        }
    }
}
