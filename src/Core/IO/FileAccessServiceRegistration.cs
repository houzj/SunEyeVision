using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace SunEyeVision.Core.IO
{
    /// <summary>
    /// 文件访问服务注册扩展
    /// 提供简化的依赖注入配置
    /// 
    /// 使用方法�?
    /// <code>
    /// var services = new ServiceCollection();
    /// services.AddFileAccessServices();
    /// 
    /// // 或者自定义配置
    /// services.AddFileAccessServices(options => {
    ///     options.EnableVerboseLogging = true;
    /// });
    /// </code>
    /// </summary>
    public static class FileAccessServiceRegistration
    {
        /// <summary>
        /// 注册文件访问相关服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合（支持链式调用）</returns>
        public static IServiceCollection AddFileAccessServices(this IServiceCollection services)
        {
            // 注册单例文件访问管理�?
            services.AddSingleton<IFileAccessManager, FileAccessManager>();
            
            Debug.WriteLine("[FileAccessServiceRegistration] �?文件访问服务已注�?);
            
            return services;
        }
        
        /// <summary>
        /// 注册文件访问相关服务（带配置选项�?
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configure">配置回调</param>
        /// <returns>服务集合（支持链式调用）</returns>
        public static IServiceCollection AddFileAccessServices(
            this IServiceCollection services,
            Action<FileAccessOptions> configure)
        {
            var options = new FileAccessOptions();
            configure?.Invoke(options);
            
            // 注册配置选项
            services.AddSingleton(options);
            
            // 注册单例文件访问管理�?
            services.AddSingleton<IFileAccessManager, FileAccessManager>();
            
            if (options.EnableVerboseLogging)
            {
                Debug.WriteLine("[FileAccessServiceRegistration] �?文件访问服务已注册（详细日志已启用）");
            }
            else
            {
                Debug.WriteLine("[FileAccessServiceRegistration] �?文件访问服务已注�?);
            }
            
            return services;
        }
    }
    
    /// <summary>
    /// 文件访问服务配置选项
    /// </summary>
    public class FileAccessOptions
    {
        /// <summary>
        /// 是否启用详细日志（默认：false�?
        /// </summary>
        public bool EnableVerboseLogging { get; set; } = false;
        
        /// <summary>
        /// 延迟删除超时时间（默认：5分钟�?
        /// 超过此时间的待删除文件将被强制标记为已删�?
        /// </summary>
        public TimeSpan PendingDeletionTimeout { get; set; } = TimeSpan.FromMinutes(5);
    }
}
