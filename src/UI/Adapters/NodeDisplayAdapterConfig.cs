using System;
using System.IO;
using SunEyeVision.Plugin.SDK.Logging;
using SunEyeVision.UI.Services.Logging;
using SunEyeVision.Workflow;

namespace SunEyeVision.UI.Adapters
{
    /// <summary>
    /// 节点显示适配器配置类
    /// 用于注册和初始化所有节点显示适配器。
    /// </summary>
    public static class NodeDisplayAdapterConfig
    {
        private static bool _isInitialized = false;

    /// <summary>
    /// 初始化所有节点显示适配器。
    /// </summary>
    public static void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        // 注册图像源节点适配器。
        NodeDisplayAdapterFactory.RegisterAdapter("ImageSource", new ImageSourceNodeDisplayAdapter());

        // 注册视频源节点适配器。
        NodeDisplayAdapterFactory.RegisterAdapter("VideoSource", new VideoSourceNodeDisplayAdapter());

        // 注册多集合节点适配器。
        NodeDisplayAdapterFactory.RegisterAdapter("MultiCollection", new MultiCollectionNodeDisplayAdapter());

        // 注册处理节点适配器。
        NodeDisplayAdapterFactory.RegisterAdapter("Processing", new ProcessingNodeDisplayAdapter());

        // 注册AI分析节点适配器。
        NodeDisplayAdapterFactory.RegisterAdapter("AIAnalysis", new AIAnalysisNodeDisplayAdapter());

        // 可以继续添加其他类型的适配器...

        _isInitialized = true;
    }

        /// <summary>
        /// 重置适配器配置。
        /// </summary>
        public static void Reset()
        {
            NodeDisplayAdapterFactory.Clear();
            _isInitialized = false;
        }
    }

    /// <summary>
    /// 服务初始化扩展方法类。
    /// </summary>
    public static class ServiceInitializer
    {
        private static UILogWriter? _uiLogWriter;
        private static ProjectManager? _projectManager;

        /// <summary>
        /// 获取 UI 日志写入器（供 LogPanelViewModel 使用）
        /// </summary>
        /// <remarks>
        /// 在应用启动时预创建，确保日志从一开始就能显示到UI。
        /// </remarks>
        public static UILogWriter UILogWriter => _uiLogWriter ?? throw new InvalidOperationException("UILogWriter 未初始化，请先调用 InitializeServices()");

        /// <summary>
        /// 获取项目管理器
        /// </summary>
        public static ProjectManager ProjectManager => _projectManager ?? throw new InvalidOperationException("ProjectManager 未初始化，请先调用 InitializeProjectManager()");

        /// <summary>
        /// 初始化所有服务。
        /// </summary>
        public static void InitializeServices()
        {
            // 初始化显示适配器配置。
            NodeDisplayAdapterConfig.Initialize();

            // 初始化插件日志器 - 让工具项目可以访问全局日志器
            InitializePluginLogger();
        }

        /// <summary>
        /// 初始化项目管理器
        /// </summary>
        /// <param name="solutionsDirectory">解决方案目录，默认为solutions/</param>
        public static void InitializeProjectManager(string? solutionsDirectory = null)
        {
            if (_projectManager != null)
            {
                return;
            }

            var logger = VisionLogger.Instance;
            logger.Info("正在初始化项目管理器...", "ServiceInitializer");

            // 如果未指定解决方案目录，使用默认目录
            if (string.IsNullOrWhiteSpace(solutionsDirectory))
            {
                solutionsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "solutions");
            }

            // 创建项目管理器
            _projectManager = new ProjectManager(solutionsDirectory);
            logger.Info($"项目管理器初始化完成，目录: {_projectManager.SolutionsDirectory}", "ServiceInitializer");
        }

        /// <summary>
        /// 初始化插件日志器
        /// </summary>
        private static void InitializePluginLogger()
        {
            System.Diagnostics.Debug.WriteLine("[ServiceInitializer] InitializePluginLogger 开始...");

            var logger = VisionLogger.Instance;
            System.Diagnostics.Debug.WriteLine($"[ServiceInitializer] VisionLogger.Instance 获取成功, WriterCount={logger.WriterCount}");

            // 添加默认日志写入器（确保日志从一开始就有输出）
            // 1. Debug写入器 - 输出到VS调试窗口
            var debugWriter = new DebugLogWriter();
            logger.AddWriter(debugWriter);
            System.Diagnostics.Debug.WriteLine($"[ServiceInitializer] DebugLogWriter 已添加");

            // 2. 文件写入器 - 持久化到文件
            var fileWriter = new FileLogWriter();
            logger.AddWriter(fileWriter);
            System.Diagnostics.Debug.WriteLine($"[ServiceInitializer] FileLogWriter 已添加, 日志目录: {fileWriter.CurrentLogFile}");

            // 3. UI写入器 - 提前创建，确保日志从启动开始就能显示到UI
            _uiLogWriter = new UILogWriter();
            logger.AddWriter(_uiLogWriter);
            System.Diagnostics.Debug.WriteLine($"[ServiceInitializer] UILogWriter 已添加");

            // 设置 PluginLogger 的提供者
            Plugin.SDK.Logging.PluginLogger.SetProvider(logger);
            System.Diagnostics.Debug.WriteLine($"[ServiceInitializer] PluginLogger.SetProvider 已调用");

            // 测试日志
            logger.Info("日志系统初始化完成", "ServiceInitializer");
            System.Diagnostics.Debug.WriteLine($"[ServiceInitializer] 测试日志已发送, WriterCount={logger.WriterCount}");
        }
    }
}
