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
    /// - 移除冗余字段：Type, ParametersTypeName, Index, GlobalIndex, NodeTypeIcon
    /// - 添加计算属性：NodeType, DisplayName, Icon, ToolType
    /// - 核心字段：8个（Id, Name, AlgorithmType, Parameters, ParameterBindings, IsEnabled, PositionX, PositionY, Width, Height）
    ///
    /// ID格式说明：
    /// - 节点ID格式：{GlobalIndex}_{AlgorithmType}_{LocalIndex}
    /// - 示例：1_ImageThreshold_1, 2_GaussianBlur_2
    /// - GlobalIndex：全局唯一递增序号，确保节点ID唯一性
    /// - LocalIndex：同类型节点的局部序号，支持重命名
    ///
    /// 设计原则：
    /// - 节点类型从 AlgorithmType 动态推断
    /// - 显示名称从工具元数据获取
    /// - 图标从工具元数据获取
    ///
    /// 重构说明：
    /// - 2026-03-21: 重命名为 WorkflowNodeBase，消除命名冲突
    /// - UI 层派生类为 WorkflowNode（SunEyeVision.UI.Models）
    /// </remarks>
    public class WorkflowNodeBase
    {
        #region 核心字段（8个）

        /// <summary>
        /// 节点ID - 格式：{GlobalIndex}_{AlgorithmType}_{LocalIndex}
        /// </summary>
        /// <remarks>
        /// 节点ID由全局序号、算法类型和局部序号组合而成，确保全局唯一性。
        /// 示例：1_ImageThreshold_1
        /// </remarks>
        public string Id { get; set; }

        /// <summary>
        /// 全局序号 - 从节点ID解析
        /// </summary>
        public int GlobalIndex { get; private set; } = -1;

        /// <summary>
        /// 局部序号 - 从节点ID解析
        /// </summary>
        public int LocalIndex { get; private set; } = -1;

        /// <summary>
        /// 节点名称 - 格式：{DisplayName}_{LocalIndex}
        /// </summary>
        /// <remarks>
        /// 节点名称由显示名称和局部序号组成，支持用户修改。
        /// 示例：图像阈值化_1
        /// </remarks>
        public string Name { get; set; }

        /// <summary>
        /// 算法类型名称（工具ID）
        /// </summary>
        /// <remarks>
        /// 用于标识节点使用的工具类型。
        /// NodeType 从此类型动态推断。
        /// </remarks>
        public string AlgorithmType { get; set; }

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
        /// 节点类型（从 AlgorithmType 动态推断）
        /// </summary>
        public NodeType NodeType => NodeTypeHelper.InferNodeTypeFromAlgorithmType(AlgorithmType);

        /// <summary>
        /// 显示名称（从工具元数据获取）
        /// </summary>
        public string DisplayName
        {
            get
            {
                var metadata = ToolRegistry.GetToolMetadata(AlgorithmType);
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
                var metadata = ToolRegistry.GetToolMetadata(AlgorithmType);
                return metadata?.Icon ?? "?";
            }
        }

        /// <summary>
        /// 工具类型（算法类型的简化表示）
        /// </summary>
        public string ToolType => AlgorithmType;

        #endregion

        /// <summary>
        /// 执行前事件
        /// </summary>
        public event Action<WorkflowNodeBase> BeforeExecute;

        /// <summary>
        /// 执行后事件
        /// </summary>
        public event Action<WorkflowNodeBase, AlgorithmResult> AfterExecute;

        public WorkflowNodeBase(string id, string name, string algorithmType)
        {
            Id = id;
            Name = name;
            AlgorithmType = algorithmType;
            Parameters = new GenericToolParameters();
            ParameterBindings = new ParameterBindingContainer();
            ParseIndicesFromId();
        }

        /// <summary>
        /// 从节点ID解析索引
        /// </summary>
        private void ParseIndicesFromId()
        {
            if (ValidateIdFormat())
            {
                var parts = Id.Split('_');
                GlobalIndex = int.TryParse(parts[0], out var globalIndex) ? globalIndex : -1;
                LocalIndex = int.TryParse(parts[2], out var localIndex) ? localIndex : -1;
            }
        }

        /// <summary>
        /// 验证节点ID格式
        /// </summary>
        /// <returns>如果ID格式正确返回true，否则返回false</returns>
        public bool ValidateIdFormat()
        {
            if (string.IsNullOrWhiteSpace(Id))
                return false;

            var parts = Id.Split('_');
            if (parts.Length != 3)
                return false;

            // 验证第一部分是否为有效的全局序号
            if (!int.TryParse(parts[0], out _))
                return false;

            // 验证第二部分（算法类型）不为空
            if (string.IsNullOrWhiteSpace(parts[1]))
                return false;

            // 验证第三部分是否为有效的局部序号
            if (!int.TryParse(parts[2], out _))
                return false;

            return true;
        }

        /// <summary>
        /// 从节点ID解析算法类型
        /// </summary>
        /// <returns>算法类型，如果解析失败返回空字符串</returns>
        public string ParseAlgorithmType()
        {
            if (!ValidateIdFormat())
                return string.Empty;

            var parts = Id.Split('_');
            return parts[1];
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
            throw new NotImplementedException($"Algorithm type '{AlgorithmType}' is not implemented.");
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
                ["NodeType"] = (int)NodeType,
                ["AlgorithmType"] = AlgorithmType,
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
            var algorithmType = dict.TryGetValue("AlgorithmType", out var algoVal) ? algoVal?.ToString() ?? string.Empty : string.Empty;
            var isEnabled = dict.TryGetValue("IsEnabled", out var enabledVal) && Convert.ToBoolean(enabledVal);

            // 读取位置属性
            var positionX = dict.TryGetValue("PositionX", out var xVal) ? Convert.ToDouble(xVal) : 0.0;
            var positionY = dict.TryGetValue("PositionY", out var yVal) ? Convert.ToDouble(yVal) : 0.0;
            var width = dict.TryGetValue("Width", out var wVal) ? Convert.ToDouble(wVal) : 140.0;
            var height = dict.TryGetValue("Height", out var hVal) ? Convert.ToDouble(hVal) : 90.0;

            // 推断节点类型（优先读取 NodeType 字段，否则从 AlgorithmType 推断）
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
                nodeType = NodeTypeHelper.InferNodeTypeFromAlgorithmType(algorithmType);
            }

            var node = new WorkflowNodeBase(id, name, algorithmType)
            {
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
                AlgorithmType
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
