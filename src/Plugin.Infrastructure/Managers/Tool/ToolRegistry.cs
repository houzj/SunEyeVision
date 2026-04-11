using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.Logging;
using SunEyeVision.Core.Services.Serialization;

namespace SunEyeVision.Plugin.Infrastructure.Managers.Tool
{
    /// <summary>
    /// 工具注册中心 - 管理所有已注册的工具
    /// </summary>
    /// <remarks>
    /// 优化后的设计：
    /// - 只管理工具类型字典（简化为1个字典）
    /// - 元数据按需创建（延迟创建，减少内存占用）
    /// - 从 [Tool] 特性自动提取基本信息
    /// </remarks>
    public static class ToolRegistry
    {
        private static readonly Dictionary<string, Type> _toolTypes = new();
        private static readonly Dictionary<string, ToolMetadata> _metadataCache = new();
        private static readonly object _lock = new();

        // ========================================
        // 缓存结构：多个缓存池（方案B）
        // ========================================
        
        /// <summary>
        /// 数值类型缓存池
        /// </summary>
        private static readonly Dictionary<string, List<ToolPropertyMetadata>> _numericTypeCache = new();
        
        /// <summary>
        /// 图像类型缓存池
        /// </summary>
        private static readonly Dictionary<string, List<ToolPropertyMetadata>> _imageTypeCache = new();
        
        /// <summary>
        /// 形状类型缓存池
        /// </summary>
        private static readonly Dictionary<string, List<ToolPropertyMetadata>> _shapeTypeCache = new();
        
        /// <summary>
        /// 文本类型缓存池
        /// </summary>
        private static readonly Dictionary<string, List<ToolPropertyMetadata>> _textTypeCache = new();
        
        /// <summary>
        /// 列表类型缓存池
        /// </summary>
        private static readonly Dictionary<string, List<ToolPropertyMetadata>> _listTypeCache = new();
        
        /// <summary>
        /// 其他类型缓存池
        /// </summary>
        private static readonly Dictionary<string, List<ToolPropertyMetadata>> _otherTypeCache = new();

        /// <summary>
        /// 注册工具类型（从 [Tool] 特性提取元数据）
        /// </summary>
        /// <param name="toolType">工具类型</param>
        public static void RegisterTool(Type toolType)
        {
            if (toolType == null)
                throw new ArgumentNullException(nameof(toolType));

            var attr = toolType.GetCustomAttribute<ToolAttribute>();
            if (attr == null)
                throw new ArgumentException($"类型 {toolType.Name} 缺少 [Tool] 特性");

            lock (_lock)
            {
                _toolTypes[attr.Id] = toolType;
                
                // 提取参数类型信息（用于诊断）
                Type? paramsType = null;
                Type? resultType = null;
                foreach (var iface in toolType.GetInterfaces())
                {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IToolPlugin<,>))
                    {
                        var genericArgs = iface.GetGenericArguments();
                        if (genericArgs.Length >= 2)
                        {
                            paramsType = genericArgs[0];
                            resultType = genericArgs[1];
                            break;
                        }
                    }
                }
                
                // 主动注册参数类型到多态序列化配置
                if (paramsType != null && typeof(ToolParameters).IsAssignableFrom(paramsType))
                {
                    RegisterParameterTypeToRegistry(paramsType);
                }
                
                // ✅ 提取并缓存属性元数据
                ExtractPropertyMetadata(attr.Id, resultType);
                
                VisionLogger.Instance.Log(LogLevel.Success,
                    $"工具注册成功: {attr.DisplayName} (Id={attr.Id}, ParamsType={paramsType?.Name ?? "未指定"}, ResultType={resultType?.Name ?? "未指定"})",
                    "ToolRegistry");
            }
        }

        /// <summary>
        /// 注册参数类型到 ParameterTypeRegistry
        /// </summary>
        /// <param name="paramsType">参数类型</param>
        private static void RegisterParameterTypeToRegistry(Type paramsType)
        {
            VisionLogger.Instance.Log(LogLevel.Info,
                $"🔄 [ToolRegistry → ParameterTypeRegistry] 开始注册参数类型: {paramsType.FullName}",
                "ToolRegistry");
            
            try
            {
                // 从 [JsonDerivedType] 特性提取类型鉴别器
                var jsonDerivedAttr = paramsType.GetCustomAttribute<JsonDerivedTypeAttribute>();
                string typeDiscriminator = jsonDerivedAttr?.TypeDiscriminator?.ToString() ?? paramsType.Name;
                
                VisionLogger.Instance.Log(LogLevel.Info,
                    $"🔄 [ToolRegistry → ParameterTypeRegistry] 提取鉴别器: '{typeDiscriminator}' " +
                    $"(来源: {(jsonDerivedAttr != null ? "JsonDerivedType特性" : "类型名称")})",
                    "ToolRegistry");

                // 调用 ParameterTypeRegistry 进行注册
                VisionLogger.Instance.Log(LogLevel.Info,
                    $"🔄 [ToolRegistry → ParameterTypeRegistry] 调用 ParameterTypeRegistry.RegisterParameterType...",
                    "ToolRegistry");
                
                ParameterTypeRegistry.RegisterParameterType(paramsType, typeDiscriminator);

                VisionLogger.Instance.Log(LogLevel.Success,
                    $"✅ [ToolRegistry → ParameterTypeRegistry] 参数类型注册完成: {paramsType.Name} → '{typeDiscriminator}'",
                    "ToolRegistry");
            }
            catch (Exception ex)
            {
                VisionLogger.Instance.Log(LogLevel.Warning,
                    $"❌ [ToolRegistry → ParameterTypeRegistry] 参数类型注册失败: {paramsType.Name}, 错误: {ex.Message}",
                    "ToolRegistry");
            }
        }

        /// <summary>
        /// 获取工具元数据（带缓存）
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>工具元数据，未找到则返回null</returns>
        public static ToolMetadata? GetToolMetadata(string toolId)
        {
            lock (_lock)
            {
                if (!_toolTypes.TryGetValue(toolId, out var toolType))
                    return null;

                var attr = toolType.GetCustomAttribute<ToolAttribute>();
                if (attr == null)
                    return null;

                if (_metadataCache.TryGetValue(toolId, out var cachedMetadata))
                    return cachedMetadata;

                var metadata = ToolMetadata.FromToolType(
                    toolType,
                    attr.Id,
                    attr.DisplayName,
                    attr.Description,
                    attr.Icon,
                    attr.Category
                );

                // 提取 DebugWindowStyle 属性
                metadata.DebugWindowStyle = attr.DebugWindowStyle;

                _metadataCache[toolId] = metadata;
                return metadata;
            }
        }

        /// <summary>
        /// 获取所有工具元数据（带缓存）
        /// </summary>
        /// <returns>所有工具元数据列表</returns>
        public static List<ToolMetadata> GetAllToolMetadata()
        {
            lock (_lock)
            {
                return _toolTypes.Values.Select(toolType =>
                {
                    var attr = toolType.GetCustomAttribute<ToolAttribute>();
                    if (attr == null)
                        return null;

                    var toolId = attr.Id;
                    if (_metadataCache.TryGetValue(toolId, out var cachedMetadata))
                        return cachedMetadata;

                    var metadata = ToolMetadata.FromToolType(
                        toolType,
                        attr.Id,
                        attr.DisplayName,
                        attr.Description,
                        attr.Icon,
                        attr.Category
                    );

                    _metadataCache[toolId] = metadata;
                    return metadata;
                }).Where(m => m != null).ToList()!;
            }
        }

        /// <summary>
        /// 获取指定分类的工具元数据
        /// </summary>
        /// <param name="category">分类名称</param>
        /// <returns>该分类下的工具元数据列表</returns>
        public static List<ToolMetadata> GetToolsByCategory(string category)
        {
            return GetAllToolMetadata().Where(m => m.Category == category).ToList();
        }

        /// <summary>
        /// 创建工具实例
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>工具实例，未找到则返回null</returns>
        public static IToolPlugin? CreateToolInstance(string toolId)
        {
            lock (_lock)
            {
                if (_toolTypes.TryGetValue(toolId, out var type))
                {
                    return Activator.CreateInstance(type) as IToolPlugin;
                }
                return null;
            }
        }

        /// <summary>
        /// 创建参数实例
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>参数实例</returns>
        public static ToolParameters CreateParameters(string toolId)
        {
            VisionLogger.Instance.Log(LogLevel.Info, $"[ToolRegistry.CreateParameters] 开始创建参数实例 - toolId={toolId}", "ToolRegistry");

            var tool = CreateToolInstance(toolId);
            if (tool != null)
            {
                VisionLogger.Instance.Log(LogLevel.Info, $"[ToolRegistry.CreateParameters] 工具实例创建成功 - tool类型={tool.GetType().Name}, ParamsType={tool.ParamsType?.Name ?? "null"}", "ToolRegistry");

                if (tool.ParamsType == null)
                {
                    VisionLogger.Instance.Log(LogLevel.Error, $"[ToolRegistry.CreateParameters] 参数类型为null - toolId={toolId}", "ToolRegistry");
                    return new GenericToolParameters();
                }

                try
                {
                    var paramsInstance = (ToolParameters)Activator.CreateInstance(tool.ParamsType)!;
                    VisionLogger.Instance.Log(LogLevel.Success, $"[ToolRegistry.CreateParameters] 参数创建成功 - 类型={paramsInstance.GetType().FullName}", "ToolRegistry");
                    return paramsInstance;
                }
                catch (Exception ex)
                {
                    VisionLogger.Instance.Log(LogLevel.Error, $"[ToolRegistry.CreateParameters] 参数创建失败 - toolId={toolId}, ParamsType={tool.ParamsType.FullName}, 错误={ex.Message}", "ToolRegistry");
                    return new GenericToolParameters();
                }
            }

            VisionLogger.Instance.Log(LogLevel.Warning, $"[ToolRegistry.CreateParameters] 工具实例创建失败 - toolId={toolId}", "ToolRegistry");
            return new GenericToolParameters();
        }

        /// <summary>
        /// 创建调试窗口
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>调试窗口实例</returns>
        public static System.Windows.Window? CreateDebugWindow(string toolId)
        {
            var tool = CreateToolInstance(toolId);
            return tool?.HasDebugWindow == true ? tool.CreateDebugWindow() : null;
        }

        /// <summary>
        /// 检查工具是否存在
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>是否存在</returns>
        public static bool ToolExists(string toolId)
        {
            lock (_lock)
            {
                return _toolTypes.ContainsKey(toolId);
            }
        }

        /// <summary>
        /// 注销工具
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>是否注销成功</returns>
        public static bool UnregisterTool(string toolId)
        {
            lock (_lock)
            {
                var removed = _toolTypes.Remove(toolId);
                _metadataCache.Remove(toolId);
                ClearToolCache(toolId);
                return removed;
            }
        }
        
        // ========================================
        // 缓存清理方法
        // ========================================
        
        /// <summary>
        /// 清除所有缓存池
        /// </summary>
        public static void ClearAllCaches()
        {
            _numericTypeCache.Clear();
            _imageTypeCache.Clear();
            _shapeTypeCache.Clear();
            _textTypeCache.Clear();
            _listTypeCache.Clear();
            _otherTypeCache.Clear();
            
            VisionLogger.Instance.Log(LogLevel.Success, 
                "✅ 清除所有属性元数据缓存池", 
                "ToolRegistry");
        }
        
        /// <summary>
        /// 清除指定工具的缓存
        /// </summary>
        public static void ClearToolCache(string toolId)
        {
            _numericTypeCache.Remove(toolId);
            _imageTypeCache.Remove(toolId);
            _shapeTypeCache.Remove(toolId);
            _textTypeCache.Remove(toolId);
            _listTypeCache.Remove(toolId);
            _otherTypeCache.Remove(toolId);
            
            VisionLogger.Instance.Log(LogLevel.Success, 
                $"✅ 清除工具缓存: {toolId}", 
                "ToolRegistry");
        }
        
        // ========================================
        // 按分类查询方法
        // ========================================
        
        /// <summary>
        /// 按分类获取属性元数据
        /// </summary>
        public static List<ToolPropertyMetadata> GetMetadataByCategory(string toolId, OutputTypeCategory category)
        {
            return category switch
            {
                OutputTypeCategory.Numeric => _numericTypeCache.TryGetValue(toolId, out var numeric) ? numeric : new(),
                OutputTypeCategory.Image => _imageTypeCache.TryGetValue(toolId, out var image) ? image : new(),
                OutputTypeCategory.Shape => _shapeTypeCache.TryGetValue(toolId, out var shape) ? shape : new(),
                OutputTypeCategory.Text => _textTypeCache.TryGetValue(toolId, out var text) ? text : new(),
                OutputTypeCategory.List => _listTypeCache.TryGetValue(toolId, out var list) ? list : new(),
                OutputTypeCategory.Other => _otherTypeCache.TryGetValue(toolId, out var other) ? other : new(),
                _ => new()
            };
        }

        /// <summary>
        /// 清除所有已注册的工具和缓存
        /// </summary>
        public static void ClearAll()
        {
            lock (_lock)
            {
                _toolTypes.Clear();
                _metadataCache.Clear();
                ClearAllCaches();
            }
        }

        /// <summary>
        /// 获取已注册的工具数量
        /// </summary>
        /// <returns>工具数量</returns>
        public static int GetToolCount()
        {
            lock (_lock)
            {
                return _toolTypes.Count;
            }
        }

        /// <summary>
        /// 获取所有分类
        /// </summary>
        /// <returns>分类列表</returns>
        public static List<string> GetAllCategories()
        {
            return GetAllToolMetadata()
                .Select(m => m.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }

        /// <summary>
        /// 提取属性元数据到多个缓存池（方案B）
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <param name="resultType">结果类型</param>
        private static void ExtractPropertyMetadata(string toolId, Type? resultType)
        {
            if (resultType == null || !typeof(ToolResults).IsAssignableFrom(resultType))
            {
                VisionLogger.Instance.Log(LogLevel.Info,
                    $"⏭️ 跳过属性元数据提取: {toolId} (ResultType 为 null 或非 ToolResults 派生类)",
                    "ToolRegistry");
                return;
            }

            try
            {
                VisionLogger.Instance.Log(LogLevel.Info, 
                    $"开始提取属性元数据: {toolId}, 结果类型: {resultType.FullName}", 
                    "ToolRegistry");
                
                // 创建临时空实例（仅用于提取元数据）
                ToolResults? tempResult;
                try
                {
                    tempResult = Activator.CreateInstance(resultType) as ToolResults;
                }
                catch (Exception ex)
                {
                    VisionLogger.Instance.Log(LogLevel.Warning, 
                        $"⚠️ 创建临时实例失败: {toolId}, 错误: {ex.Message}", 
                        "ToolRegistry");
                    return;
                }
                
                if (tempResult == null)
                {
                    VisionLogger.Instance.Log(LogLevel.Warning, 
                        $"⚠️ 临时实例为 null: {toolId}", 
                        "ToolRegistry");
                    return;
                }
                
                // 为每个分类创建列表
                var numericProperties = new List<ToolPropertyMetadata>();
                var imageProperties = new List<ToolPropertyMetadata>();
                var shapeProperties = new List<ToolPropertyMetadata>();
                var textProperties = new List<ToolPropertyMetadata>();
                var listProperties = new List<ToolPropertyMetadata>();
                var otherProperties = new List<ToolPropertyMetadata>();
                
                // 遍历所有公共属性
                var properties = resultType.GetProperties(
                    BindingFlags.Public |
                    BindingFlags.Instance);
                
                int validPropertyCount = 0;
                
                foreach (var prop in properties)
                {
                    // 跳过索引属性
                    if (prop.GetIndexParameters().Length > 0)
                        continue;
                    
                    // 跳过基类 ToolResults 的属性
                    if (prop.DeclaringType == typeof(ToolResults))
                        continue;
                    
                    // 跳过特殊属性
                    if (prop.Name == "Status" ||
                        prop.Name == "ErrorMessage" ||
                        prop.Name == "ExecutionTimeMs" ||
                        prop.Name == "Timestamp" ||
                        prop.Name == "ToolName" ||
                        prop.Name == "ToolId")
                    {
                        continue;
                    }
                    
                    // 获取 TreeName
                    var treeName = tempResult.GetPropertyTreeName(prop.Name);
                    
                    // 创建元数据
                    var metadata = new ToolPropertyMetadata
                    {
                        PropertyName = prop.Name,
                        DisplayName = GetDisplayName(prop.Name),
                        TreeName = string.IsNullOrEmpty(treeName) ? GetDisplayName(prop.Name) : treeName,
                        PropertyType = prop.PropertyType,
                        Description = GetPropertyDescription(prop)
                    };
                    
                    // 确定类型分类
                    var category = OutputTypeCategoryMapper.GetCategory(prop.PropertyType);
                    
                    // 添加到对应的缓存池
                    switch (category)
                    {
                        case OutputTypeCategory.Numeric:
                            numericProperties.Add(metadata);
                            break;
                        case OutputTypeCategory.Image:
                            imageProperties.Add(metadata);
                            break;
                        case OutputTypeCategory.Shape:
                            shapeProperties.Add(metadata);
                            break;
                        case OutputTypeCategory.Text:
                            textProperties.Add(metadata);
                            break;
                        case OutputTypeCategory.List:
                            listProperties.Add(metadata);
                            break;
                        case OutputTypeCategory.Other:
                            otherProperties.Add(metadata);
                            break;
                    }
                    
                    validPropertyCount++;
                    
                    VisionLogger.Instance.Log(LogLevel.Info,
                        $"  [ToolRegistry] 添加属性到 [{category}] 分类: {metadata.PropertyName} ({prop.PropertyType.Name})",
                        "ToolRegistry");
                }
                
                // 存入对应的缓存池
                if (numericProperties.Count > 0)
                    _numericTypeCache[toolId] = numericProperties;
                
                if (imageProperties.Count > 0)
                    _imageTypeCache[toolId] = imageProperties;
                
                if (shapeProperties.Count > 0)
                    _shapeTypeCache[toolId] = shapeProperties;
                
                if (textProperties.Count > 0)
                    _textTypeCache[toolId] = textProperties;
                
                if (listProperties.Count > 0)
                    _listTypeCache[toolId] = listProperties;
                
                if (otherProperties.Count > 0)
                    _otherTypeCache[toolId] = otherProperties;
                
                VisionLogger.Instance.Log(LogLevel.Success,
                    $"✅ 提取属性元数据完成: {toolId}, " +
                    $"共 {validPropertyCount} 个属性, " +
                    $"分类: Numeric={numericProperties.Count}, " +
                    $"Image={imageProperties.Count}, " +
                    $"Shape={shapeProperties.Count}, " +
                    $"Text={textProperties.Count}, " +
                    $"List={listProperties.Count}, " +
                    $"Other={otherProperties.Count}",
                    "ToolRegistry");
            }
            catch (Exception ex)
            {
                VisionLogger.Instance.Log(LogLevel.Warning,
                    $"⚠️ 提取属性元数据失败: {toolId}, 错误: {ex.Message}",
                    "ToolRegistry");
            }
        }

        /// <summary>
        /// 获取属性元数据（从变量池）
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <param name="propertyName">属性名</param>
        /// <returns>属性元数据，未找到返回 null</returns>
        public static ToolPropertyMetadata? GetPropertyMetadata(string toolId, string propertyName)
        {
            lock (_lock)
            {
                // 从所有分类缓存中查找属性
                var caches = new List<Dictionary<string, List<ToolPropertyMetadata>>>
                {
                    _numericTypeCache,
                    _imageTypeCache,
                    _shapeTypeCache,
                    _textTypeCache,
                    _listTypeCache,
                    _otherTypeCache
                };

                foreach (var cache in caches)
                {
                    if (cache.TryGetValue(toolId, out var propertyList))
                    {
                        var metadata = propertyList.FirstOrDefault(m => m.PropertyName == propertyName);
                        if (metadata != null)
                        {
                            return metadata;
                        }
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// 获取所有属性元数据（合并所有分类）
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>属性元数据列表，未找到返回空列表</returns>
        public static List<ToolPropertyMetadata> GetAllPropertyMetadata(string toolId)
        {
            var allMetadata = new List<ToolPropertyMetadata>();
            
            allMetadata.AddRange(_numericTypeCache.TryGetValue(toolId, out var numeric) ? numeric : new());
            allMetadata.AddRange(_imageTypeCache.TryGetValue(toolId, out var image) ? image : new());
            allMetadata.AddRange(_shapeTypeCache.TryGetValue(toolId, out var shape) ? shape : new());
            allMetadata.AddRange(_textTypeCache.TryGetValue(toolId, out var text) ? text : new());
            allMetadata.AddRange(_listTypeCache.TryGetValue(toolId, out var list) ? list : new());
            allMetadata.AddRange(_otherTypeCache.TryGetValue(toolId, out var other) ? other : new());
            
            return allMetadata;
        }

        /// <summary>
        /// 获取属性显示名称
        /// </summary>
        private static string GetDisplayName(string propertyName)
        {
            return propertyName;
        }

        /// <summary>
        /// 获取属性描述
        /// </summary>
        private static string? GetPropertyDescription(PropertyInfo prop)
        {
            // 可以从特性中读取描述
            return null;
        }
    }
}
