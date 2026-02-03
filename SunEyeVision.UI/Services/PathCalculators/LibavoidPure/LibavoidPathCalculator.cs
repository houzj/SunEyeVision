using System;
using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;
using System.Linq;

#if LIBAVOID_AVAILABLE
using SunEyeVision.LibavoidWrapper;
using PortDirectionWrapper = SunEyeVision.LibavoidWrapper.PortDirection;
#endif

using PortDirectionUI = SunEyeVision.UI.Services.PathCalculators.PortDirection;

namespace SunEyeVision.UI.Services.PathCalculators
{
    /// <summary>
    /// Libavoid包装的路径计算器 - 使用C++/CLI封装的Libavoid库进行正交路径计算
    /// 注意: 需要编译SunEyeVision.LibavoidWrapper C++项目才能使用此功能
    /// </summary>
    public class LibavoidPathCalculator : IPathCalculator
    {
#if LIBAVOID_AVAILABLE
        private LibavoidRouter? router;
        private RouterConfiguration? config;
#endif
        private readonly object _lockObj = new object();
        private bool _initialized = false;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public LibavoidPathCalculator()
        {
#if LIBAVOID_AVAILABLE
            System.Diagnostics.Debug.WriteLine("[LibavoidPathCalculator] ╔═════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("[LibavoidPathCalculator] ║      LibavoidPathCalculator 构造函数开始           ║");
            System.Diagnostics.Debug.WriteLine("[LibavoidPathCalculator] ╚═════════════════════════════════════════════════════╝");

            try
            {
                // 检查 LibavoidWrapper 类型是否可加载
                System.Diagnostics.Debug.WriteLine("[LibavoidPathCalculator] 步骤0: 检查 LibavoidWrapper 类型加载");
                Type routerType = typeof(LibavoidRouter);
                System.Diagnostics.Debug.WriteLine($"[LibavoidPathCalculator] ✅ LibavoidRouter 类型加载成功: {routerType.Assembly.FullName}");

                System.Diagnostics.Debug.WriteLine("[LibavoidPathCalculator] 步骤1: 创建 RouterConfiguration");
                config = new RouterConfiguration();

                System.Diagnostics.Debug.WriteLine("[LibavoidPathCalculator] 步骤2: 设置配置属性");
                config.IdealSegmentLength = 50.0;
                config.UseOrthogonalRouting = true;
                config.RoutingTimeLimit = 5000;

                System.Diagnostics.Debug.WriteLine("[LibavoidPathCalculator] 步骤3: 配置设置完成");
                System.Diagnostics.Debug.WriteLine($"[LibavoidPathCalculator]   - IdealSegmentLength: {config.IdealSegmentLength}");
                System.Diagnostics.Debug.WriteLine($"[LibavoidPathCalculator]   - UseOrthogonalRouting: {config.UseOrthogonalRouting}");

                System.Diagnostics.Debug.WriteLine("[LibavoidPathCalculator] ✅ === 构造函数成功完成 ===");
                System.Diagnostics.Debug.WriteLine("[LibavoidPathCalculator] ℹ️  注意: LibavoidRouter 将在首次使用时延迟初始化");
            }
            catch (TypeLoadException ex)
            {
                System.Diagnostics.Debug.WriteLine("[LibavoidPathCalculator] ❌ 类型加载异常！");
                System.Diagnostics.Debug.WriteLine($"[LibavoidPathCalculator] 消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[LibavoidPathCalculator] 类型名: {ex.TypeName}");
                System.Diagnostics.Debug.WriteLine($"[LibavoidPathCalculator] 堆栈: {ex.StackTrace}");
                throw new InvalidOperationException("无法加载 LibavoidWrapper 程序集。请确保 SunEyeVision.LibavoidWrapper.dll 在输出目录中。", ex);
            }
            catch (BadImageFormatException ex)
            {
                System.Diagnostics.Debug.WriteLine("[LibavoidPathCalculator] ❌ DLL 格式异常！");
                System.Diagnostics.Debug.WriteLine($"[LibavoidPathCalculator] 消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[LibavoidPathCalculator] 文件名: {ex.FileName}");
                System.Diagnostics.Debug.WriteLine($"[LibavoidPathCalculator] 堆栈: {ex.StackTrace}");
                throw new InvalidOperationException("LibavoidWrapper DLL 格式不正确（可能是架构不匹配，x86 vs x64）。", ex);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LibavoidPathCalculator] ❌ 构造函数异常: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[LibavoidPathCalculator] 消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[LibavoidPathCalculator] 堆栈: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[LibavoidPathCalculator] 内部异常: {ex.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"[LibavoidPathCalculator] 内部堆栈: {ex.InnerException.StackTrace}");
                }
                throw;
            }
            // 延迟初始化 LibavoidRouter - 不在构造函数中创建
#else
            System.Diagnostics.Debug.WriteLine("[LibavoidPathCalculator] LibavoidWrapper 不可用 - 使用OrthogonalPathCalculator");
#endif
        }

        /// <summary>
        /// 确保路由器已初始化
        /// </summary>
        private void EnsureRouterInitialized()
        {
#if LIBAVOID_AVAILABLE
            if (_initialized)
            {
                System.Diagnostics.Debug.WriteLine("[LibavoidPathCalculator] 路由器已初始化，跳过");
                return;
            }

            lock (_lockObj)
            {
                if (_initialized)
                {
                    System.Diagnostics.Debug.WriteLine("[LibavoidPathCalculator] 路由器已初始化（锁内），跳过");
                    return;
                }

                try
                {
                    System.Diagnostics.Debug.WriteLine("[LibavoidPathCalculator] === 开始初始化 LibavoidRouter ===");
                    System.Diagnostics.Debug.WriteLine("[LibavoidPathCalculator] 检查 config 是否为 null...");
                    if (config == null)
                    {
                        throw new InvalidOperationException("RouterConfiguration 未初始化");
                    }
                    System.Diagnostics.Debug.WriteLine("[LibavoidPathCalculator] config 不为 null，准备创建 LibavoidRouter");
                    System.Diagnostics.Debug.WriteLine("[LibavoidPathCalculator] 调用 new LibavoidRouter(config)...");
                    router = new LibavoidRouter(config);
                    System.Diagnostics.Debug.WriteLine("[LibavoidPathCalculator] LibavoidRouter 创建成功！");
                    _initialized = true;
                    System.Diagnostics.Debug.WriteLine("[LibavoidPathCalculator] === LibavoidRouter 初始化成功 ===");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[LibavoidPathCalculator] ❌ LibavoidRouter 初始化失败: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[LibavoidPathCalculator] 异常类型: {ex.GetType().Name}");
                    System.Diagnostics.Debug.WriteLine($"[LibavoidPathCalculator] 堆栈跟踪: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[LibavoidPathCalculator] 内部异常: {ex.InnerException.Message}");
                    }
                    throw;
                }
            }
#else
            throw new NotSupportedException("LibavoidWrapper不可用，请编译SunEyeVision.LibavoidWrapper项目");
#endif
        }

        /// <summary>
        /// 带配置的构造函数
        /// </summary>
        /// <param name="configuration">路由配置</param>
        public LibavoidPathCalculator(object configuration)
        {
#if LIBAVOID_AVAILABLE
            if (configuration is RouterConfiguration routerConfig)
            {
                config = routerConfig ?? throw new ArgumentNullException(nameof(configuration));
                router = new LibavoidRouter(config);
            }
            else
            {
                throw new ArgumentException("configuration参数必须是RouterConfiguration类型");
            }
#else
            throw new NotSupportedException("LibavoidWrapper不可用，请编译SunEyeVision.LibavoidWrapper项目");
#endif
        }

        /// <summary>
        /// 转换PortDirection枚举
        /// </summary>
#if LIBAVOID_AVAILABLE
        private PortDirectionWrapper ConvertPortDirection(PortDirectionUI direction)
        {
            return direction switch
            {
                PortDirectionUI.Top => PortDirectionWrapper.Top,
                PortDirectionUI.Bottom => PortDirectionWrapper.Bottom,
                PortDirectionUI.Left => PortDirectionWrapper.Left,
                PortDirectionUI.Right => PortDirectionWrapper.Right,
                _ => PortDirectionWrapper.Right
            };
        }
#endif

        /// <summary>
        /// 计算正交折线路径点集合（基础方法，不包含节点信息）
        /// 注意：targetPosition应该是箭头尾部位置（由ConnectionPathCache计算）
        /// </summary>
        /// <param name="sourcePosition">源端口位置（路径起点）</param>
        /// <param name="targetPosition">目标端口位置（箭头尾部位置，路径终点）</param>
        /// <param name="sourceDirection">源端口方向</param>
        /// <param name="targetDirection">目标端口方向</param>
        /// <returns>路径点集合（包括起点和终点）</returns>
        public Point[] CalculateOrthogonalPath(
            Point sourcePosition,
            Point targetPosition,
            PortDirectionUI sourceDirection,
            PortDirectionUI targetDirection)
        {
#if LIBAVOID_AVAILABLE
            System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("║     🚀 LibavoidPathCalculator.CalculateOrthogonalPath    ║");
            System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════════╝");

            if (sourcePosition == null)
                throw new ArgumentNullException(nameof(sourcePosition));
            if (targetPosition == null)
                throw new ArgumentNullException(nameof(targetPosition));

            System.Diagnostics.Debug.WriteLine($"[Libavoid] 源点: ({sourcePosition.X:F1}, {sourcePosition.Y:F1}), 方向: {sourceDirection}");
            System.Diagnostics.Debug.WriteLine($"[Libavoid] 目标点: ({targetPosition.X:F1}, {targetPosition.Y:F1}), 方向: {targetDirection}");

            // 转换为托管类型
            var managedSource = new ManagedPoint(sourcePosition.X, sourcePosition.Y);
            var managedTarget = new ManagedPoint(targetPosition.X, targetPosition.Y);

            // 创建小矩形作为源和目标节点边界
            var sourceRect = new ManagedRect(sourcePosition.X - 10, sourcePosition.Y - 10, 20, 20);
            var targetRect = new ManagedRect(targetPosition.X - 10, targetPosition.Y - 10, 20, 20);

            // 转换PortDirection
            var sourceDirWrapper = ConvertPortDirection(sourceDirection);
            var targetDirWrapper = ConvertPortDirection(targetDirection);

            // 确保路由器已初始化
            System.Diagnostics.Debug.WriteLine($"[Libavoid] 开始初始化路由器...");
            EnsureRouterInitialized();
            System.Diagnostics.Debug.WriteLine($"[Libavoid] ✅ 路由器初始化完成");

            // 路由路径
            System.Diagnostics.Debug.WriteLine($"[Libavoid] 开始调用 router.RoutePath...");
            var result = router.RoutePath(
                managedSource,
                managedTarget,
                sourceDirWrapper,
                targetDirWrapper,
                sourceRect,
                targetRect,
                null);

            // 检查结果
            if (!result.Success)
            {
                System.Diagnostics.Debug.WriteLine($"[Libavoid] ❌ 路由失败: {result.ErrorMessage}");
                // 返回备用路径
                return GenerateFallbackPath(sourcePosition, targetPosition, sourceDirection, targetDirection);
            }

            // 转换回Point数组
            var path = result.PathPoints.Select(p => new Point(p.X, p.Y)).ToArray();

            System.Diagnostics.Debug.WriteLine($"[Libavoid] ✅ 路由成功！路径点数: {path.Length}");
            System.Diagnostics.Debug.WriteLine($"[Libavoid] 路径点:");
            for (int i = 0; i < path.Length; i++)
            {
                System.Diagnostics.Debug.WriteLine($"[Libavoid]   点{i + 1}: ({path[i].X:F1}, {path[i].Y:F1})");
            }

            System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("║     LibavoidPathCalculator 计算完成                   ║");
            System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════════════════╝");

            return path;
#else
            System.Diagnostics.Debug.WriteLine("[LibavoidPathCalculator] LibavoidWrapper不可用，使用备用路径");
            return GenerateFallbackPath(sourcePosition, targetPosition, sourceDirection, targetDirection);
#endif
        }

        /// <summary>
        /// 计算正交折线路径点集合（增强方法，带节点信息和障碍物检测）
        /// </summary>
        /// <param name="sourcePosition">源端口位置
        /// <param name="targetPosition">目标端口位置（箭头尾部位置，路径终点）</param>
        /// <param name="sourceDirection">源端口方向</param>
        /// <param name="targetDirection">目标端口方向</param>
        /// <param name="sourceNodeRect">源节点边界矩形</param>
        /// <param name="targetNodeRect">目标节点边界矩形</param>
        /// <param name="allNodeRects">所有节点的边界矩形（用于碰撞检测，包括源节点和目标节点）</param>
        /// <returns>路径点集合（包括起点和终点）</returns>
        public Point[] CalculateOrthogonalPath(
            Point sourcePosition,
            Point targetPosition,
            PortDirectionUI sourceDirection,
            PortDirectionUI targetDirection,
            Rect sourceNodeRect,
            Rect targetNodeRect,
            params Rect[] allNodeRects)
        {
#if LIBAVOID_AVAILABLE
            if (sourcePosition == null)
                throw new ArgumentNullException(nameof(sourcePosition));
            if (targetPosition == null)
                throw new ArgumentNullException(nameof(targetPosition));
            if (sourceNodeRect == null)
                throw new ArgumentNullException(nameof(sourceNodeRect));
            if (targetNodeRect == null)
                throw new ArgumentNullException(nameof(targetNodeRect));

            System.Diagnostics.Debug.WriteLine($"[LibavoidPath] ========== 带障碍物路径计算 ==========");
            System.Diagnostics.Debug.WriteLine($"[LibavoidPath] 源位置:({sourcePosition.X:F1},{sourcePosition.Y:F1}), 目标位置（箭头尾部）:({targetPosition.X:F1},{targetPosition.Y:F1})");
            System.Diagnostics.Debug.WriteLine($"[LibavoidPath] 源方向:{sourceDirection}, 目标方向:{targetDirection}");
            System.Diagnostics.Debug.WriteLine($"[LibavoidPath] 源节点边界:({sourceNodeRect.X:F1},{sourceNodeRect.Y:F1},{sourceNodeRect.Width:F1}x{sourceNodeRect.Height:F1})");
            System.Diagnostics.Debug.WriteLine($"[LibavoidPath] 目标节点边界:({targetNodeRect.X:F1},{targetNodeRect.Y:F1},{targetNodeRect.Width:F1}x{targetNodeRect.Height:F1})");
            System.Diagnostics.Debug.WriteLine($"[LibavoidPath] 障碍物节点数:{(allNodeRects?.Length ?? 0)}");

            // 转换为托管类型
            var managedSource = new ManagedPoint(sourcePosition.X, sourcePosition.Y);
            var managedTarget = new ManagedPoint(targetPosition.X, targetPosition.Y);

            // 转换源和目标节点矩形
            var managedSourceRect = new ManagedRect(
                sourceNodeRect.X, sourceNodeRect.Y, sourceNodeRect.Width, sourceNodeRect.Height);
            var managedTargetRect = new ManagedRect(
                targetNodeRect.X, targetNodeRect.Y, targetNodeRect.Width, targetNodeRect.Height);

            // 转换障碍物（包含所有节点，包括源节点和目标节点）
            List<ManagedRect> managedObstacles = null;
            if (allNodeRects != null && allNodeRects.Length > 0)
            {
                managedObstacles = allNodeRects
                    .Select(r => new ManagedRect(r.X, r.Y, r.Width, r.Height))
                    .ToList();
                System.Diagnostics.Debug.WriteLine($"[LibavoidPath] 障碍物转换完成，障碍物数量:{managedObstacles.Count}");
            }

            // 转换PortDirection
            var sourceDirWrapper = ConvertPortDirection(sourceDirection);
            var targetDirWrapper = ConvertPortDirection(targetDirection);

            // 确保路由器已初始化
            EnsureRouterInitialized();

            // 路由路径
            var result = router.RoutePath(
                managedSource,
                managedTarget,
                sourceDirWrapper,
                targetDirWrapper,
                managedSourceRect,
                managedTargetRect,
                managedObstacles);

            // 检查结果
            if (!result.Success)
            {
                System.Diagnostics.Debug.WriteLine($"[LibavoidPath] ❌ 路由失败: {result.ErrorMessage}");
                return GenerateFallbackPath(sourcePosition, targetPosition, sourceDirection, targetDirection);
            }

            // 转换回Point数组
            var path = result.PathPoints.Select(p => new Point(p.X, p.Y)).ToArray();

            System.Diagnostics.Debug.WriteLine($"[LibavoidPath] ✅ 路由成功（带障碍物），路径点数: {path.Length}");
            for (int i = 0; i < path.Length; i++)
            {
                System.Diagnostics.Debug.WriteLine($"[LibavoidPath]   路径点[{i}]:({path[i].X:F1},{path[i].Y:F1})");
            }
            System.Diagnostics.Debug.WriteLine($"[LibavoidPath] ========== 路径计算完成 ==========");

            return path;
#else
            System.Diagnostics.Debug.WriteLine("[LibavoidPath] LibavoidWrapper不可用，使用备用路径");
            return GenerateFallbackPath(sourcePosition, targetPosition, sourceDirection, targetDirection);
#endif
        }

        /// <summary>
        /// 根据路径点创建路径几何
        /// </summary>
        /// <param name="pathPoints">路径点集合</param>
        /// <returns>PathGeometry对象</returns>
        public PathGeometry CreatePathGeometry(Point[] pathPoints)
        {
            if (pathPoints == null || pathPoints.Length < 2)
            {
                return new PathGeometry();
            }

            var geometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = pathPoints[0] };

            // 添加线段
            for (int i = 1; i < pathPoints.Length; i++)
            {
                figure.Segments.Add(new LineSegment(pathPoints[i], true));
            }

            geometry.Figures.Add(figure);
            return geometry;
        }

        /// <summary>
        /// 计算箭头位置和角度
        /// 箭头尖端位于目标端口位置，角度基于目标端口方向固定
        /// 路径终点已经是箭头尾部位置（由ConnectionPathCache计算）
        /// </summary>
        /// <param name="pathPoints">路径点数组（终点是箭头尾部）</param>
        /// <param name="targetPosition">目标端口位置（箭头尖端位置）</param>
        /// <param name="targetDirection">目标端口方向，决定箭头的固定角度</param>
        /// <returns>箭头位置和角度（角度为度数）</returns>
        public (Point position, double angle) CalculateArrow(Point[] pathPoints, Point targetPosition, PortDirectionUI targetDirection)
        {
            if (pathPoints == null || pathPoints.Length < 2)
            {
                return (new Point(0, 0), 0);
            }

            // 箭头尖端位于目标端口位置
            var arrowPosition = targetPosition;

            // 箭头角度基于目标端口方向固定
            var arrowAngle = GetFixedArrowAngle(targetDirection);

            // 获取路径最后一点用于调试（箭头尾部位置）
            var lastPoint = pathPoints[pathPoints.Length - 1];

            // 关键日志：记录箭头计算结果
            System.Diagnostics.Debug.WriteLine($"[ArrowCalc] ========== 箭头计算结果 ==========");
            System.Diagnostics.Debug.WriteLine($"[ArrowCalc] 箭头尖端位置（目标端口）:({arrowPosition.X:F1},{arrowPosition.Y:F1})");
            System.Diagnostics.Debug.WriteLine($"[ArrowCalc] 目标端口方向:{targetDirection}, 固定箭头角度:{arrowAngle:F1}°");
            System.Diagnostics.Debug.WriteLine($"[ArrowCalc] 箭头尾部位置（路径终点）:({lastPoint.X:F1},{lastPoint.Y:F1})");

            return (arrowPosition, arrowAngle);
        }

        /// <summary>
        /// 获取固定箭头角度（基于目标端口方向）
        /// 箭头角度不受源节点端口影响，固定为目标端口方向
        /// 角度定义：0度指向右，90度指向下，180度指向左，270度指向上
        /// </summary>
        private double GetFixedArrowAngle(PortDirectionUI targetDirection)
        {
            return targetDirection switch
            {
                PortDirectionUI.Left => 0.0,     // 左边端口：箭头向右
                PortDirectionUI.Right => 180.0,   // 右边端口：箭头向左
                PortDirectionUI.Top => 90.0,      // 上边端口：箭头向下
                PortDirectionUI.Bottom => 270.0,  // 下边端口：箭头向上
                _ => 0.0
            };
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        public void ClearCache()
        {
#if LIBAVOID_AVAILABLE
            EnsureRouterInitialized();
            router.ClearCache();
#endif
        }

        /// <summary>
        /// 生成备用路径（简单三段式路径）
        /// </summary>
        private Point[] GenerateFallbackPath(Point source, Point target, PortDirectionUI sourceDir, PortDirectionUI targetDir)
        {
            List<Point> path = new List<Point> { source };

            // 根据端口方向选择路径策略
            bool horizontalFirst = sourceDir == PortDirectionUI.Left || sourceDir == PortDirectionUI.Right;

            if (horizontalFirst)
            {
                // 水平优先
                path.Add(new Point(target.X, source.Y));
            }
            else
            {
                // 垂直优先
                path.Add(new Point(source.X, target.Y));
            }

            path.Add(target);

            return path.ToArray();
        }
    }
}
