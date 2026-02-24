using System;
using SunEyeVision.UI.Services.Path;

namespace SunEyeVision.UI.Services.PathCalculators
{
    /// <summary>
    /// 路径计算器类型枚�?
    /// </summary>
    public enum PathCalculatorType
    {
        /// <summary>
        /// AIStudio.Wpf.DiagramDesigner 路径计算器（MIT�?
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
    /// 路径计算器工�?- 负责创建和管理路径计算器实例
    /// </summary>
    public static class PathCalculatorFactory
    {
        /// <summary>
        /// 当前使用的路径计算器类型（可在运行时修改�?
        /// </summary>
        public static PathCalculatorType CurrentCalculatorType { get; set; } = PathCalculatorType.Bezier;

        /// <summary>
        /// 创建路径计算器实�?
        /// </summary>
        /// <param name="calculatorType">路径计算器类�?/param>
        /// <returns>路径计算器实�?/returns>
        public static IPathCalculator CreateCalculator(PathCalculatorType? calculatorType = null)
        {
            var type = calculatorType ?? CurrentCalculatorType;

            try
            {
                switch (type)
                {
                    case PathCalculatorType.AIStudio:
                        return new AIStudioPathCalculator();

                    case PathCalculatorType.Orthogonal:
                        return new OrthogonalPathCalculator();

                    case PathCalculatorType.Bezier:
                        return new BezierPathCalculator();

                    default:
                        throw new ArgumentException($"未知的路径计算器类型: {type}");
                }
            }
            catch (Exception ex)
            {
                // 回退到内置的正交路径计算�?
                try
                {
                    return new OrthogonalPathCalculator();
                }
                catch (Exception fallbackEx)
                {
                    throw new InvalidOperationException("无法创建任何路径计算器实�?, fallbackEx);
                }
            }
        }

        /// <summary>
        /// 创建路径计算器实例（带自动回退�?
        /// </summary>
        /// <param name="preferredTypes">优先使用的路径计算器类型列表（按优先级排序）</param>
        /// <returns>路径计算器实�?/returns>
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
                    return CreateCalculator(type);
                }
                catch (Exception ex)
                {
                    // 尝试下一个选项
                }
            }

            // 所有选项都失败，抛出异常
            throw new InvalidOperationException($"无法创建任何指定的路径计算器: {string.Join(", ", preferredTypes)}");
        }

        /// <summary>
        /// 获取路径计算器的显示名称
        /// </summary>
        /// <param name="type">路径计算器类�?/param>
        /// <returns>显示名称</returns>
        public static string GetDisplayName(PathCalculatorType type)
        {
            return type switch
            {
                PathCalculatorType.AIStudio => "AIStudio.Wpf.DiagramDesigner (MIT)",
                PathCalculatorType.Orthogonal => "简单正交路�?(内置)",
                PathCalculatorType.Bezier => "贝塞尔曲�?(内置)",
                _ => "未知"
            };
        }

        /// <summary>
        /// 获取路径计算器的描述
        /// </summary>
        /// <param name="type">路径计算器类�?/param>
        /// <returns>描述信息</returns>
        public static string GetDescription(PathCalculatorType type)
        {
            return type switch
            {
                PathCalculatorType.AIStudio => "基于 AIStudio.Wpf.DiagramDesigner 的路径计算器，MIT 协议，适合商业项目",
                PathCalculatorType.Orthogonal => "内置的简单正交路径计算器，无需外部依赖，适合简单场�?,
                PathCalculatorType.Bezier => "内置的贝塞尔曲线路径计算器，提供平滑的曲线连接，适合需要美观曲线的场景",
                _ => "未知类型"
            };
        }
    }
}
