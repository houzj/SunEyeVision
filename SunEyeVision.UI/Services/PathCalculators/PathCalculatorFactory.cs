using System;

namespace SunEyeVision.UI.Services.PathCalculators
{
    /// <summary>
    /// 路径计算器类型枚举
    /// </summary>
    public enum PathCalculatorType
    {
        /// <summary>
        /// Libavoid 路径计算器（C++/CLI 封装，LGPL-2.1）
        /// </summary>
        Libavoid,

        /// <summary>
        /// AIStudio.Wpf.DiagramDesigner 路径计算器（MIT）
        /// </summary>
        AIStudio,

        /// <summary>
        /// 简单正交路径计算器（内置）
        /// </summary>
        Orthogonal,

        /// <summary>
        /// 贝塞尔曲线路径计算器（内置）
        /// </summary>
        Bezier
    }

    /// <summary>
    /// 路径计算器工厂 - 负责创建和管理路径计算器实例
    /// </summary>
    public static class PathCalculatorFactory
    {
        /// <summary>
        /// 当前使用的路径计算器类型（可在运行时修改）
        /// </summary>
        public static PathCalculatorType CurrentCalculatorType { get; set; } = PathCalculatorType.Bezier;

        /// <summary>
        /// 创建路径计算器实例
        /// </summary>
        /// <param name="calculatorType">路径计算器类型</param>
        /// <returns>路径计算器实例</returns>
        public static IPathCalculator CreateCalculator(PathCalculatorType? calculatorType = null)
        {
            var type = calculatorType ?? CurrentCalculatorType;

            System.Diagnostics.Debug.WriteLine($"[PathCalculatorFactory] 创建路径计算器: {type}");

            try
            {
                switch (type)
                {
                    case PathCalculatorType.Libavoid:
                        System.Diagnostics.Debug.WriteLine("[PathCalculatorFactory] 尝试创建 LibavoidPathCalculator...");
                        return new LibavoidPathCalculator();

                    case PathCalculatorType.AIStudio:
                        System.Diagnostics.Debug.WriteLine("[PathCalculatorFactory] 尝试创建 AIStudioPathCalculator...");
                        return new AIStudioPathCalculator();

                    case PathCalculatorType.Orthogonal:
                        System.Diagnostics.Debug.WriteLine("[PathCalculatorFactory] 尝试创建 OrthogonalPathCalculator...");
                        return new OrthogonalPathCalculator();

                    case PathCalculatorType.Bezier:
                        System.Diagnostics.Debug.WriteLine("[PathCalculatorFactory] 尝试创建 BezierPathCalculator...");
                        return new BezierPathCalculator();

                    default:
                        throw new ArgumentException($"未知的路径计算器类型: {type}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PathCalculatorFactory] ❌ 创建 {type} 路径计算器失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[PathCalculatorFactory] 尝试回退到 OrthogonalPathCalculator...");

                // 回退到内置的正交路径计算器
                try
                {
                    return new OrthogonalPathCalculator();
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[PathCalculatorFactory] ❌ 回退失败: {fallbackEx.Message}");
                    throw new InvalidOperationException("无法创建任何路径计算器实例", fallbackEx);
                }
            }
        }

        /// <summary>
        /// 创建路径计算器实例（带自动回退）
        /// </summary>
        /// <param name="preferredTypes">优先使用的路径计算器类型列表（按优先级排序）</param>
        /// <returns>路径计算器实例</returns>
        public static IPathCalculator CreateCalculatorWithFallback(params PathCalculatorType[] preferredTypes)
        {
            if (preferredTypes == null || preferredTypes.Length == 0)
            {
                return CreateCalculator();
            }

            foreach (var type in preferredTypes)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[PathCalculatorFactory] 尝试创建 {type} 路径计算器...");
                    return CreateCalculator(type);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PathCalculatorFactory] ⚠️ {type} 路径计算器创建失败: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[PathCalculatorFactory] 尝试下一个选项...");
                }
            }

            // 所有选项都失败，抛出异常
            throw new InvalidOperationException($"无法创建任何指定的路径计算器: {string.Join(", ", preferredTypes)}");
        }

        /// <summary>
        /// 获取路径计算器的显示名称
        /// </summary>
        /// <param name="type">路径计算器类型</param>
        /// <returns>显示名称</returns>
        public static string GetDisplayName(PathCalculatorType type)
        {
            return type switch
            {
                PathCalculatorType.Libavoid => "Libavoid (LGPL-2.1)",
                PathCalculatorType.AIStudio => "AIStudio.Wpf.DiagramDesigner (MIT)",
                PathCalculatorType.Orthogonal => "简单正交路径 (内置)",
                PathCalculatorType.Bezier => "贝塞尔曲线 (内置)",
                _ => "未知"
            };
        }

        /// <summary>
        /// 获取路径计算器的描述
        /// </summary>
        /// <param name="type">路径计算器类型</param>
        /// <returns>描述信息</returns>
        public static string GetDescription(PathCalculatorType type)
        {
            return type switch
            {
                PathCalculatorType.Libavoid => "基于 C++ Libavoid 库的正交路径路由器，支持节点和连线避让，性能优异",
                PathCalculatorType.AIStudio => "基于 AIStudio.Wpf.DiagramDesigner 的路径计算器，MIT 协议，适合商业项目",
                PathCalculatorType.Orthogonal => "内置的简单正交路径计算器，无需外部依赖，适合简单场景",
                PathCalculatorType.Bezier => "内置的贝塞尔曲线路径计算器，提供平滑的曲线连接，适合需要美观曲线的场景",
                _ => "未知类型"
            };
        }
    }
}
