namespace SunEyeVision.UI.Adapters
{
    /// <summary>
    /// 节点显示适配器配置类
    /// 用于注册和初始化所有节点显示适配�?
    /// </summary>
    public static class NodeDisplayAdapterConfig
    {
        private static bool _isInitialized = false;

    /// <summary>
    /// 初始化所有节点显示适配�?
    /// </summary>
    public static void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        // 注册图像源节点适配�?
        NodeDisplayAdapterFactory.RegisterAdapter("ImageSource", new ImageSourceNodeDisplayAdapter());

        // 注册视频源节点适配�?
        NodeDisplayAdapterFactory.RegisterAdapter("VideoSource", new VideoSourceNodeDisplayAdapter());

        // 注册多集合节点适配�?
        NodeDisplayAdapterFactory.RegisterAdapter("MultiCollection", new MultiCollectionNodeDisplayAdapter());

        // 注册处理节点适配�?
        NodeDisplayAdapterFactory.RegisterAdapter("Processing", new ProcessingNodeDisplayAdapter());

        // 注册AI分析节点适配�?
        NodeDisplayAdapterFactory.RegisterAdapter("AIAnalysis", new AIAnalysisNodeDisplayAdapter());

        // 可以继续添加其他类型的适配�?..

        _isInitialized = true;
    }

        /// <summary>
        /// 重置适配器配�?
        /// </summary>
        public static void Reset()
        {
            NodeDisplayAdapterFactory.Clear();
            _isInitialized = false;
        }
    }

    /// <summary>
    /// 服务初始化扩展方�?
    /// </summary>
    public static class ServiceInitializer
    {
        /// <summary>
        /// 初始化所有服�?
        /// </summary>
        public static void InitializeServices()
        {
            // 初始化显示适配器配�?
            NodeDisplayAdapterConfig.Initialize();
        }
    }
}
