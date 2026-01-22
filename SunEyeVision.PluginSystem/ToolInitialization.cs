using System;
using System.Linq;
using System.Reflection;

namespace SunEyeVision.PluginSystem
{
    /// <summary>
    /// 工具插件初始化器 - 自动扫描和加载工具插件
    /// </summary>
    public static class ToolInitializer
    {
        /// <summary>
        /// 从指定程序集自动注册所有工具插件
        /// </summary>
        /// <param name="assembly">要扫描的程序集</param>
        public static void RegisterToolsFromAssembly(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            try
            {
                // 查找所有标记了 ToolPlugin 特性的类型
                var toolPluginTypes = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Contains(typeof(IToolPlugin)))
                    .Where(t => t.GetCustomAttribute<ToolPluginAttribute>() != null)
                    .ToList();

                foreach (var toolType in toolPluginTypes)
                {
                    try
                    {
                        // 创建插件实例
                        var plugin = Activator.CreateInstance(toolType) as IToolPlugin;
                        if (plugin != null)
                        {
                            // 初始化插件
                            plugin.Initialize();

                            // 注册到ToolRegistry
                            ToolRegistry.RegisterTool(plugin);

                            System.Diagnostics.Debug.WriteLine($"成功注册工具插件: {plugin.Name} ({plugin.PluginId})");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"注册工具插件失败 {toolType.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"扫描程序集失败 {assembly.GetName().Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// 从当前应用程序域的所有程序集注册工具插件
        /// </summary>
        public static void RegisterAllTools()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                try
                {
                    RegisterToolsFromAssembly(assembly);
                }
                catch
                {
                    // 忽略无法加载的程序集
                }
            }
        }

        /// <summary>
        /// 从特定命名空间的程序集注册工具插件
        /// </summary>
        /// <param name="namespacePattern">命名空间模式</param>
        public static void RegisterToolsByNamespace(string namespacePattern)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                try
                {
                    if (assembly.FullName?.Contains(namespacePattern) == true ||
                        assembly.GetTypes().Any(t => t.Namespace?.StartsWith(namespacePattern) == true))
                    {
                        RegisterToolsFromAssembly(assembly);
                    }
                }
                catch
                {
                    // 忽略无法加载的程序集
                }
            }
        }
    }
}
