using System;
using System.Collections.Generic;
using SunEyeVision.Plugin.SDK.Logging;
using SunEyeVision.Core.Models;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.Infrastructure.Managers.Tool;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 工作流节点基类
    /// </summary>
    /// <remarks>
    /// 优化说明：
    /// - 移除冗余字段：Type, ParametersTypeName, Index, NodeTypeIcon
    /// - 添加计算属性：NodeType, DisplayName, Icon, ToolType
    /// - 核心字段：8个（Id, LocalIndex, Name, AlgorithmType, Parameters, ParameterBindings, IsEnabled, PositionX, PositionY, Width, Height）
    ///
    /// ID优化说明：
    /// - 节点ID使用Guid，确保全局唯一性和稳定性
    /// - LocalIndex作为独立属性存储，支持hole pool机制
    /// - 节点显示名称格式：{DisplayName}{LocalIndex}（无分隔符）
    ///
    /// 设计原则：
    /// - 节点类型从 AlgorithmType 动态推断
    /// - 显示名称从工具元数据获取
    /// - 图标从工具元数据获取
    ///
    /// 重构说明：
    /// - 2026-03-21: 重命名为 WorkflowNodeBase，消除命名冲突
    /// - 2026-03-22: 优化节点ID系统，使用Guid替代组合格式，LocalIndex独立存储
    /// - UI 层派生类为 WorkflowNode（SunEyeVision.UI.Models）
    /// </remarks>
    public class WorkflowNodeBase
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

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 节点位置X坐标
        /// </summary>
        public double PositionX { get; set; } = 0;

        /// <summary>
        /// 节点位置Y坐标
        /// </summary>
        public double PositionY { get; set; } = 0;

        /// <summary>
        /// 节点宽度
        /// </summary>
        public double Width { get; set; } = 140;

        /// <summary>
        /// 节点高度
        /// </summary>
        public double Height { get; set; } = 90;

        #endregion

        #region 计算属性（从元数据推断）

        /// <summary>
        /// 节点类型（从 ToolType 动态推断）
        /// </summary>
        public NodeType NodeType => NodeTypeHelper.InferNodeTypeFromToolType(ToolType);

        /// <summary>
        /// 显示名称（从工具元数据获取）
        /// </summary>
        public string DisplayName
        {
            get
            {
                var metadata = ToolRegistry.GetToolMetadata(ToolType);
                return metadata?.DisplayName ?? Name;
            }
        }

        /// <summary>
        /// 节点图标（从工具元数据获取）
        /// </summary>
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
        /// 获取节点的可序列化数据
        /// </summary>
        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>
            {
                ["Id"] = Id,
                ["Name"] = Name,
                ["DispName"] = DispName,
                ["LocalIndex"] = LocalIndex,
                ["GlobalIndex"] = GlobalIndex,
                ["NodeType"] = (int)NodeType,
                ["ToolType"] = ToolType,
                ["IsEnabled"] = IsEnabled,
                ["PositionX"] = PositionX,
                ["PositionY"] = PositionY,
                ["Width"] = Width,
                ["Height"] = Height,
                ["Parameters"] = Parameters.ToSerializableDictionary()
            };

            // 始终序列化参数绑定，即使为空
            if (ParameterBindings != null)
            {
                dict["ParameterBindings"] = ParameterBindings.ToDictionary();
            }
            else
            {
                var defaultBindings = new ParameterBindingContainer { NodeId = Id };
                dict["ParameterBindings"] = defaultBindings.ToDictionary();
            }

            return dict;
        }

        /// <summary>
        /// 从字典创建节点
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
            if (dict.TryGetValue("Parameters", out var paramsVal) && paramsVal is Dictionary<string, object?> paramsDict)
            {
                var restored = ToolParameters.CreateFromDictionary(paramsDict);
                if (restored != null)
                    node.Parameters = restored;
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
