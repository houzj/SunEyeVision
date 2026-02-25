namespace SunEyeVision.UI.Adapters
{
    /// <summary>
    /// 节点显示适配器工具类
    /// </summary>
    public static class NodeDisplayAdapterFactory
    {
        private static readonly Dictionary<string, INodeDisplayAdapter> _adapters = new Dictionary<string, INodeDisplayAdapter>();
        private static readonly INodeDisplayAdapter _defaultAdapter = new DefaultNodeDisplayAdapter();

        /// <summary>
        /// 注册节点显示适配器
        /// </summary>
        /// <param name="algorithmType">算法类型</param>
        /// <param name="adapter">适配器实例。</param>
        public static void RegisterAdapter(string algorithmType, INodeDisplayAdapter adapter)
        {
            _adapters[algorithmType] = adapter;
        }

        /// <summary>
        /// 获取节点显示适配器
        /// </summary>
        /// <param name="algorithmType">算法类型</param>
        /// <returns>适配器实例，如果未找到则返回默认适配器。</returns>
        public static INodeDisplayAdapter GetAdapter(string algorithmType)
        {
            return _adapters.TryGetValue(algorithmType, out var adapter) ? adapter : _defaultAdapter;
        }

        /// <summary>
        /// 清空所有已注册的适配器。
        /// </summary>
        public static void Clear()
        {
            _adapters.Clear();
        }
    }
}
