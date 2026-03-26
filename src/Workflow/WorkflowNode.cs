using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Windows;
using SunEyeVision.Plugin.SDK.Logging;
using SunEyeVision.Core.Models;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Models;
using SunEyeVision.Plugin.Infrastructure.Managers.Tool;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 工作流节点基类
    /// </summary>
    /// <remarks>
    /// 优化说明（2026-03-26 重构）：
    /// - 继承 ObservableObject，支持属性通知
    /// - UI 层专有属性使用 [JsonIgnore] 标记，不参与序列化
    /// - 消除 UI.Models.WorkflowNode 副本，直接使用此类
    /// - 单一数据源：Solution.Workflow.Nodes 直接被 UI 绑定
    ///
    /// 核心字段（序列化）：
    /// - Id, Name, DispName, ToolType
    /// - LocalIndex, GlobalIndex
    /// - Parameters, ParameterBindings
    /// - IsEnabled, PositionX, PositionY, Width, Height
    ///
    /// UI 专有属性（不序列化）：
    /// - IsSelected, IsVisible, Status
    /// - OutputCache, InputSource, LastResult
    /// - StyleConfig (存储为基本类型)
    /// </remarks>
    public class WorkflowNodeBase : ObservableObject
    {
        #region 核心字段（8个）

        /// <summary>
        /// 节点ID - 使用Guid确保全局唯一性和稳定性
        /// </summary>
        /// <remarks>
        /// 节点ID使用Guid,避免因显示名称修改导致ID变更的风险。
        /// 示例：e2a8c5d7-f3e2-4b1a-9c5d-8f7e6b5a4c3d
        /// </remarks>
        public string Id { get; set; }

        /// <summary>
        /// 全局序号 - 跨所有工作流共享的全局唯一递增序号
        /// </summary>
        /// <remarks>
        /// 全局序号用于标识节点创建的全局顺序，不会因为工作流切换而重新计算。
        /// 每次创建节点时递增，确保全局唯一性。
        /// </remarks>
        public int GlobalIndex { get; set; } = -1;

        /// <summary>
        /// 局部序号 - 同类型节点的局部序号,直接存储
        /// </summary>
        /// <remarks>
        /// 局部序号不再从节点ID解析,而是作为独立属性存储。
        /// 支持hole pool机制进行索引复用。
        /// </remarks>
        public int LocalIndex { get; set; } = -1;

        /// <summary>
        /// 节点名称 - 格式：{GlobalIndex} {DispName}
        /// </summary>
        /// <remarks>
        /// 节点名称由全局序号和显示名称组成,用于序列化。
        /// 示例：1 图像采集1, 2 高斯模糊2
        /// </remarks>
        public string Name { get; set; }

        /// <summary>
        /// 节点显示名称 - 格式：{DisplayName}{LocalIndex}
        /// </summary>
        /// <remarks>
        /// 节点显示名称由工具显示名称和局部序号组成,用于UI显示。
        /// 示例：图像采集1, 高斯模糊2
        /// 支持用户重命名。
        /// </remarks>
        public string DispName { get; set; }

        /// <summary>
        /// 工具类型（算法类型的简化表示，符合视觉软件行业标准）
        /// </summary>
        /// <remarks>
        /// 用于标识节点使用的工具类型。
        /// NodeType 从此类型动态推断。
        /// </remarks>
        public string ToolType { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        private double _positionX = 0;
        /// <summary>
        /// 节点位置X坐标
        /// </summary>
        public double PositionX
        {
            get => _positionX;
            set
            {
                if (SetProperty(ref _positionX, value))
                {
                    OnPropertyChanged(nameof(Position));
                }
            }
        }

        private double _positionY = 0;
        /// <summary>
        /// 节点位置Y坐标
        /// </summary>
        public double PositionY
        {
            get => _positionY;
            set
            {
                if (SetProperty(ref _positionY, value))
                {
                    OnPropertyChanged(nameof(Position));
                }
            }
        }

        /// <summary>
        /// 节点宽度
        /// </summary>
        public double Width { get; set; } = 140;

        /// <summary>
        /// 节点高度
        /// </summary>
        public double Height { get; set; } = 90;

        /// <summary>
        /// 节点参数
        /// </summary>
        public ToolParameters Parameters { get; set; }

        /// <summary>
        /// 参数绑定配置
        /// </summary>
        /// <remarks>
        /// 支持参数与父节点输出的动态绑定。
        /// 用于在执行时自动从父节点获取参数值。
        /// </remarks>
        public ParameterBindingContainer ParameterBindings { get; set; }

        #endregion

        #region UI 层专有属性（不序列化）

        private bool _isSelected;
        private bool _isVisible = true;
        private string _status = "待运行";
        private object? _outputCache;
        private object? _inputSource;
        private object? _lastResult;
        private object? _styleConfig;

        /// <summary>
        /// 节点位置（UI 层专用，不序列化，基于 PositionX/PositionY 计算）
        /// </summary>
        [JsonIgnore]
        public Point Position
        {
            get => new Point(PositionX, PositionY);
            set
            {
                if (PositionX != value.X || PositionY != value.Y)
                {
                    PositionX = value.X;
                    PositionY = value.Y;
                    OnPropertyChanged(nameof(Position));
                }
            }
        }

        /// <summary>
        /// 是否被选中（UI 层专用，不序列化）
        /// </summary>
        [JsonIgnore]
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>
        /// 节点是否可见（UI 层专用，用于虚拟化渲染，不序列化）
        /// </summary>
        [JsonIgnore]
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        /// <summary>
        /// 节点状态（UI 层专用，不序列化）
        /// </summary>
        [JsonIgnore]
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        /// <summary>
        /// 节点输出缓存（UI 层专用，不序列化）
        /// </summary>
        [JsonIgnore]
        public object? OutputCache
        {
            get => _outputCache;
            set => SetProperty(ref _outputCache, value);
        }

        /// <summary>
        /// 节点输入源（UI 层专用，不序列化）
        /// </summary>
        [JsonIgnore]
        public object? InputSource
        {
            get => _inputSource;
            set => SetProperty(ref _inputSource, value);
        }

        /// <summary>
        /// 最近一次执行结果（UI 层专用，不序列化）
        /// </summary>
        [JsonIgnore]
        public object? LastResult
        {
            get => _lastResult;
            set => SetProperty(ref _lastResult, value);
        }

        /// <summary>
        /// 节点样式配置（UI 层专用，不序列化）
        /// </summary>
        [JsonIgnore]
        public object? StyleConfig
        {
            get => _styleConfig;
            set => SetProperty(ref _styleConfig, value);
        }

        #endregion

        #region 计算属性（从元数据推断，不序列化）

        /// <summary>
        /// 节点类型（从 ToolType 动态推断，不序列化）
        /// </summary>
        [JsonIgnore]
        public NodeType NodeType => NodeTypeHelper.InferNodeTypeFromToolType(ToolType);

        /// <summary>
        /// 显示名称（从工具元数据获取，不序列化）
        /// </summary>
        [JsonIgnore]
        public string DisplayName
        {
            get
            {
                var metadata = ToolRegistry.GetToolMetadata(ToolType);
                return metadata?.DisplayName ?? Name;
            }
        }

        /// <summary>
        /// 节点图标（从工具元数据获取，不序列化）
        /// </summary>
        [JsonIgnore]
        public string Icon
        {
            get
            {
                var metadata = ToolRegistry.GetToolMetadata(ToolType);
                return metadata?.Icon ?? "?";
            }
        }

        #endregion

        /// <summary>
        /// 执行前事件
        /// </summary>
        public event Action<WorkflowNodeBase> BeforeExecute;

        /// <summary>
        /// 执行后事件
        /// </summary>
        public event Action<WorkflowNodeBase, AlgorithmResult> AfterExecute;

        /// <summary>
        /// 供 System.Text.Json 反序列化使用的无参构造函数
        /// </summary>
        [JsonConstructor]
        public WorkflowNodeBase()
        {
            Id = Guid.NewGuid().ToString();
            Name = string.Empty;
            DispName = string.Empty;
            ToolType = string.Empty;
            Parameters = new GenericToolParameters();
            ParameterBindings = new ParameterBindingContainer();
        }

        /// <summary>
        /// 创建节点的参数化构造函数
        /// </summary>
        public WorkflowNodeBase(string id, string name, string dispName, string toolType)
        {
            Id = id;
            Name = name;
            DispName = dispName;
            ToolType = toolType;
            Parameters = new GenericToolParameters();
            ParameterBindings = new ParameterBindingContainer();
        }

        /// <summary>
        /// 触发执行前事件
        /// </summary>
        protected virtual void OnBeforeExecute()
        {
            BeforeExecute?.Invoke(this);
        }

        /// <summary>
        /// 触发执行后事件
        /// </summary>
        protected virtual void OnAfterExecute(AlgorithmResult result)
        {
            AfterExecute?.Invoke(this, result);
        }

        /// <summary>
        /// 创建工具实例（的初始化处理方法）
        /// </summary>
        public virtual IToolPlugin? CreateInstance()
        {
            throw new NotImplementedException($"Tool type '{ToolType}' is not implemented.");
        }

        /// <summary>
        /// 从字典创建节点（仅用于兼容旧格式 JSON 的反序列化）
        /// </summary>
        public static WorkflowNodeBase FromDictionary(Dictionary<string, object> dict)
        {
            var id = dict.TryGetValue("Id", out var idVal) ? idVal?.ToString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString();
            var name = dict.TryGetValue("Name", out var nameVal) ? nameVal?.ToString() ?? "Node" : "Node";
            var dispName = dict.TryGetValue("DispName", out var dispNameVal) ? dispNameVal?.ToString() ?? string.Empty : string.Empty;
            var toolType = dict.TryGetValue("ToolType", out var toolTypeVal) ? toolTypeVal?.ToString() ?? string.Empty : string.Empty;
            var isEnabled = dict.TryGetValue("IsEnabled", out var enabledVal) && Convert.ToBoolean(enabledVal);

            // 读取位置属性
            var positionX = dict.TryGetValue("PositionX", out var xVal) ? Convert.ToDouble(xVal) : 0.0;
            var positionY = dict.TryGetValue("PositionY", out var yVal) ? Convert.ToDouble(yVal) : 0.0;
            var width = dict.TryGetValue("Width", out var wVal) ? Convert.ToDouble(wVal) : 140.0;
            var height = dict.TryGetValue("Height", out var hVal) ? Convert.ToDouble(hVal) : 90.0;

            // 读取索引属性
            var localIndex = dict.TryGetValue("LocalIndex", out var localIdx) ? Convert.ToInt32(localIdx) : -1;
            var globalIndex = dict.TryGetValue("GlobalIndex", out var globalIdx) ? Convert.ToInt32(globalIdx) : -1;

            // 推断节点类型（优先读取 NodeType 字段，否则从 ToolType 推断）
            NodeType nodeType = NodeType.Algorithm;
            if (dict.TryGetValue("NodeType", out var typeVal))
            {
                if (Enum.IsDefined(typeof(NodeType), typeVal))
                {
                    nodeType = (NodeType)Convert.ToInt32(typeVal);
                }
            }
            else
            {
                nodeType = NodeTypeHelper.InferNodeTypeFromToolType(toolType);
            }

            var node = new WorkflowNodeBase(id, name, dispName, toolType)
            {
                LocalIndex = localIndex,
                GlobalIndex = globalIndex,
                IsEnabled = isEnabled,
                PositionX = positionX,
                PositionY = positionY,
                Width = width,
                Height = height
            };

            // 恢复参数
            if (dict.TryGetValue("Parameters", out var paramsVal) && paramsVal != null)
            {
                // 参数对象已经由 JsonSerializer 直接反序列化
                // 无需使用 Dictionary 转换层
                if (paramsVal is ToolParameters parameters)
                {
                    node.Parameters = parameters;
                    // 🔍 详细日志：节点参数恢复成功
                    var paramSummary = parameters.GetParameterSummary();
                    VisionLogger.Instance.Log(LogLevel.Success,
                        $"✅ [节点参数恢复] 节点: {name} | 类型: {parameters.GetType().Name} | 值: {paramSummary}",
                        "WorkflowNode");
                }
                else
                {
                    // 如果参数为 null 或类型不匹配，创建新的 GenericToolParameters
                    VisionLogger.Instance.Log(LogLevel.Warning,
                        $"⚠️ [节点参数恢复] 节点: {name} | 参数类型不匹配: {paramsVal?.GetType().Name ?? "null"} | 使用 GenericToolParameters",
                        "WorkflowNode");
                    node.Parameters = new GenericToolParameters();
                }
            }
            else
            {
                // 参数为空时创建默认参数
                VisionLogger.Instance.Log(LogLevel.Warning,
                    $"⚠️ [节点参数恢复] 节点: {name} | 参数为空，使用 GenericToolParameters",
                    "WorkflowNode");
                node.Parameters = new GenericToolParameters();
            }

            // 恢复参数绑定
            if (dict.TryGetValue("ParameterBindings", out var bindingsVal) && bindingsVal is Dictionary<string, object> bindingsDict)
            {
                node.ParameterBindings = ParameterBindingContainer.FromDictionary(bindingsDict);
            }

            return node;
        }

        /// <summary>
        /// 克隆节点
        /// </summary>
        public WorkflowNodeBase Clone()
        {
            var cloned = new WorkflowNodeBase(
                Id,
                $"{Name}_副本",
                DispName,
                ToolType
            )
            {
                IsEnabled = IsEnabled,
                Parameters = Parameters?.Clone(),
                ParameterBindings = ParameterBindings?.Clone(),
                PositionX = PositionX,
                PositionY = PositionY,
                Width = Width,
                Height = Height
            };

            return cloned;
        }
    }
}
