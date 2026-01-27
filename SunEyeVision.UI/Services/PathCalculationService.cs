using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Services
{
    public class PathCalculationService
    {
        private readonly PathConfiguration _config;
        private readonly Dictionary<string, List<Point>> _pathCache;
        private readonly Dictionary<string, double> _connectionAngles;
        private readonly Dictionary<string, DateTime> _cacheTimestamps;
        private readonly QuadTree _obstacleIndex;
        
        public PathCalculationService(PathConfiguration config = null)
        {
            _config = config ?? new PathConfiguration();
            _pathCache = new Dictionary<string, List<Point>>();
            _connectionAngles = new Dictionary<string, double>();
            _cacheTimestamps = new Dictionary<string, DateTime>();
            _obstacleIndex = new QuadTree(
                new Rect(0, 0, 2000, 2000),
                _config.QuadTreeMaxObjects,
                _config.QuadTreeMaxLevels
            );
        }
        
        public void RebuildObstacleIndex(IEnumerable<WorkflowNode> nodes)
        {
            _obstacleIndex.Clear();
            foreach (var node in nodes)
            {
                var nodeBounds = new Rect(
                    node.Position.X - _config.NodeMargin,
                    node.Position.Y - _config.NodeMargin,
                    _config.NodeWidth + _config.NodeMargin * 2,
                    _config.NodeHeight + _config.NodeMargin * 2
                );
                _obstacleIndex.Insert(node, nodeBounds);
            }
        }
        
        public List<Point> CalculatePath(Point startPoint, Point endPoint, Point arrowTailPoint, 
            WorkflowNode sourceNode, WorkflowNode targetNode, string sourcePort, string targetPort)
        {
            return CalculatePathAsync(startPoint, endPoint, arrowTailPoint, sourceNode, targetNode, sourcePort, targetPort).GetAwaiter().GetResult();
        }
        
        public async Task<List<Point>> CalculatePathAsync(Point startPoint, Point endPoint, Point arrowTailPoint, 
            WorkflowNode sourceNode, WorkflowNode targetNode, string sourcePort, string targetPort)
        {
            var cacheKey = $"{startPoint.X:F2}_{startPoint.Y:F2}_{endPoint.X:F2}_{endPoint.Y:F2}_{sourcePort}_{targetPort}";
            
            Console.WriteLine($"[PathCalculation] 开始计算路径: 从 ({startPoint.X:F2}, {startPoint.Y:F2}) 到 ({endPoint.X:F2}, {endPoint.Y:F2})，源端口: {sourcePort}，目标端口: {targetPort}");
            
            if (_pathCache.TryGetValue(cacheKey, out var cachedPath))
            {
                _cacheTimestamps[cacheKey] = DateTime.Now;
                Console.WriteLine($"[PathCalculation] 使用缓存路径，缓存项数量: {_pathCache.Count}");
                return cachedPath;
            }
            
            if (_pathCache.Count > 1000)
            {
                var oldestKey = _cacheTimestamps.OrderBy(kv => kv.Value).First().Key;
                _pathCache.Remove(oldestKey);
                _cacheTimestamps.Remove(oldestKey);
                Console.WriteLine($"[PathCalculation] 移除最旧缓存项，当前缓存项数量: {_pathCache.Count}");
            }
            
            var obstacles = FindObstacleNodes(startPoint, endPoint, sourceNode, targetNode);
            List<Point> pathPoints;
            
            if (obstacles.Count == 0)
            {
                Console.WriteLine($"[PathCalculation] 未发现障碍物，创建简单路径");
                pathPoints = CreateSimplePath(startPoint, endPoint, sourceNode, targetNode, targetPort);
            }
            else
            {
                var obstacleMinY = obstacles.Min(n => n.Position.Y) - _config.NodeMargin;
                var obstacleMaxY = obstacles.Max(n => n.Position.Y + _config.NodeHeight) + _config.NodeMargin;
                var obstacleMinX = obstacles.Min(n => n.Position.X) - _config.NodeMargin;
                var obstacleMaxX = obstacles.Max(n => n.Position.X + _config.NodeWidth) + _config.NodeMargin;
                
                Console.WriteLine($"[PathCalculation] 发现 {obstacles.Count} 个障碍物，创建绕行路径");
                foreach (var obstacle in obstacles)
                {
                    Console.WriteLine($"[PathCalculation] 障碍物: {obstacle.Name} 在 ({obstacle.Position.X:F2}, {obstacle.Position.Y:F2})");
                }
                
                pathPoints = CreateDetourPath(startPoint, endPoint, obstacles, obstacleMinY, obstacleMaxY, 
                    obstacleMinX, obstacleMaxX, targetPort, targetNode);
            }
            
            Console.WriteLine($"[PathCalculation] 路径计算完成，转折点数量: {pathPoints.Count}");
            foreach (var point in pathPoints)
            {
                Console.WriteLine($"[PathCalculation] 转折点: ({point.X:F2}, {point.Y:F2})");
            }
            
            _pathCache[cacheKey] = pathPoints;
            _cacheTimestamps[cacheKey] = DateTime.Now;
            return pathPoints;
        }
        
        public double CalculateArrowAngle(string targetPort)
        {
            return targetPort switch
            {
                "TopPort" => 90,
                "BottomPort" => 270,
                "LeftPort" => 0,
                "RightPort" => 180,
                _ => 0
            };
        }
        
        public Point CalculateAdjustedEndPoint(Point startPoint, Point targetPortPos, WorkflowNode targetNode, string targetPort)
        {
            double arrowOffset = _config.ArrowSize + 2;
            
            return targetPort switch
            {
                "TopPort" => new Point(targetPortPos.X, targetPortPos.Y - arrowOffset),
                "BottomPort" => new Point(targetPortPos.X, targetPortPos.Y + arrowOffset),
                "LeftPort" => new Point(targetPortPos.X - arrowOffset, targetPortPos.Y),
                "RightPort" => new Point(targetPortPos.X + arrowOffset, targetPortPos.Y),
                _ => targetPortPos
            };
        }
        
        private List<WorkflowNode> FindObstacleNodes(Point start, Point end, WorkflowNode sourceNode, WorkflowNode targetNode)
        {
            var obstacles = new List<WorkflowNode>();
            
            double minX = Math.Min(start.X, end.X) - _config.NodeMargin;
            double maxX = Math.Max(start.X, end.X) + _config.NodeMargin;
            double minY = Math.Min(start.Y, end.Y) - _config.NodeMargin;
            double maxY = Math.Max(start.Y, end.Y) + _config.NodeMargin;
            var pathBounds = new Rect(minX, minY, maxX - minX, maxY - minY);
            
            Console.WriteLine($"[ObstacleDetection] 开始检测障碍物，路径边界: X[{minX:F2}, {maxX:F2}], Y[{minY:F2}, {maxY:F2}]");
            
            var candidateNodes = _obstacleIndex.Query(pathBounds);
            Console.WriteLine($"[ObstacleDetection] 找到 {candidateNodes.Count} 个候选节点");
            
            foreach (var node in candidateNodes)
            {
                if (node.Id == sourceNode.Id || node.Id == targetNode.Id)
                {
                    Console.WriteLine($"[ObstacleDetection] 跳过源/目标节点: {node.Name}");
                    continue;
                }
                
                var nodeBounds = new Rect(
                    node.Position.X - _config.NodeMargin,
                    node.Position.Y - _config.NodeMargin,
                    _config.NodeWidth + _config.NodeMargin * 2,
                    _config.NodeHeight + _config.NodeMargin * 2
                );
                
                // 快速边界检查
                if (!pathBounds.IntersectsWith(nodeBounds))
                {
                    Console.WriteLine($"[ObstacleDetection] 跳过边界外节点: {node.Name}");
                    continue;
                }
                
                // 检查直接路径
                if (LineSegmentIntersectsRect(start, end, nodeBounds))
                {
                    Console.WriteLine($"[ObstacleDetection] 发现直接路径障碍物: {node.Name}");
                    obstacles.Add(node);
                    continue;
                }
                
                // 检查可能的绕行路径
                double midX = (start.X + end.X) / 2;
                double midY = (start.Y + end.Y) / 2;
                
                if (LineSegmentIntersectsRect(start, new Point(midX, start.Y), nodeBounds) ||
                    LineSegmentIntersectsRect(new Point(midX, start.Y), new Point(midX, end.Y), nodeBounds) ||
                    LineSegmentIntersectsRect(new Point(midX, end.Y), end, nodeBounds) ||
                    LineSegmentIntersectsRect(start, new Point(start.X, midY), nodeBounds) ||
                    LineSegmentIntersectsRect(new Point(start.X, midY), new Point(end.X, midY), nodeBounds) ||
                    LineSegmentIntersectsRect(new Point(end.X, midY), end, nodeBounds))
                {
                    Console.WriteLine($"[ObstacleDetection] 发现绕行路径障碍物: {node.Name}");
                    obstacles.Add(node);
                }
            }
            
            Console.WriteLine($"[ObstacleDetection] 障碍物检测完成，发现 {obstacles.Count} 个障碍物");
            return obstacles;
        }
        
        private List<Point> CreateSimplePath(Point startPoint, Point endPoint, WorkflowNode sourceNode, WorkflowNode targetNode, string targetPort)
        {
            var segments = new List<Point>();
            
            double nodeLeft = targetNode.Position.X;
            double nodeRight = targetNode.Position.X + _config.NodeWidth;
            double nodeTop = targetNode.Position.Y;
            double nodeBottom = targetNode.Position.Y + _config.NodeHeight;
            
            Console.WriteLine($"[PathCreation] 开始创建简单路径，目标端口: {targetPort}");
            Console.WriteLine($"[PathCreation] 目标节点边界: 左={nodeLeft:F2}, 右={nodeRight:F2}, 上={nodeTop:F2}, 下={nodeBottom:F2}");
            
            if (targetPort == "LeftPort" || targetPort == "RightPort")
            {
                double yDiff = Math.Abs(startPoint.Y - endPoint.Y);
                Console.WriteLine($"[PathCreation] Y方向差异: {yDiff:F2}");
                if (yDiff >= 5)
                {
                    if (targetPort == "RightPort")
                    {
                        if (startPoint.X < endPoint.X)
                        {
                            double detourY = startPoint.Y < nodeTop ? nodeTop - _config.NodeMargin : nodeBottom + _config.NodeMargin;
                            double lastTurnX = nodeRight + _config.NodeMargin;
                            segments.Add(new Point(startPoint.X, detourY));
                            segments.Add(new Point(lastTurnX, detourY));
                            segments.Add(new Point(lastTurnX, endPoint.Y));
                            Console.WriteLine($"[PathCreation] 右端口 - 右向绕行，转折点: ({startPoint.X:F2}, {detourY:F2}) → ({lastTurnX:F2}, {detourY:F2}) → ({lastTurnX:F2}, {endPoint.Y:F2})");
                        }
                        else
                        {
                            double midY = (startPoint.Y + endPoint.Y) / 2;
                            segments.Add(new Point(startPoint.X, midY));
                            segments.Add(new Point(endPoint.X, midY));
                            Console.WriteLine($"[PathCreation] 右端口 - 直接水平路径，转折点: ({startPoint.X:F2}, {midY:F2}) → ({endPoint.X:F2}, {midY:F2})");
                        }
                    }
                    else
                    {
                        if (startPoint.X > endPoint.X)
                        {
                            double detourY = startPoint.Y < nodeTop ? nodeTop - _config.NodeMargin : nodeBottom + _config.NodeMargin;
                            double lastTurnX = nodeLeft - _config.NodeMargin;
                            segments.Add(new Point(startPoint.X, detourY));
                            segments.Add(new Point(lastTurnX, detourY));
                            segments.Add(new Point(lastTurnX, endPoint.Y));
                            Console.WriteLine($"[PathCreation] 左端口 - 左向绕行，转折点: ({startPoint.X:F2}, {detourY:F2}) → ({lastTurnX:F2}, {detourY:F2}) → ({lastTurnX:F2}, {endPoint.Y:F2})");
                        }
                        else
                        {
                            double midY = (startPoint.Y + endPoint.Y) / 2;
                            segments.Add(new Point(startPoint.X, midY));
                            segments.Add(new Point(endPoint.X, midY));
                            Console.WriteLine($"[PathCreation] 左端口 - 直接水平路径，转折点: ({startPoint.X:F2}, {midY:F2}) → ({endPoint.X:F2}, {midY:F2})");
                        }
                    }
                }
            }
            else
            {
                double xDiff = Math.Abs(startPoint.X - endPoint.X);
                Console.WriteLine($"[PathCreation] X方向差异: {xDiff:F2}");
                if (xDiff >= 5)
                {
                    if (targetPort == "BottomPort")
                    {
                        if (startPoint.Y < endPoint.Y)
                        {
                            double detourX = startPoint.X < nodeLeft ? nodeLeft - _config.NodeMargin : nodeRight + _config.NodeMargin;
                            double lastTurnY = nodeBottom + _config.NodeMargin;
                            segments.Add(new Point(detourX, startPoint.Y));
                            segments.Add(new Point(detourX, lastTurnY));
                            segments.Add(new Point(endPoint.X, lastTurnY));
                            Console.WriteLine($"[PathCreation] 下端口 - 下向绕行，转折点: ({detourX:F2}, {startPoint.Y:F2}) → ({detourX:F2}, {lastTurnY:F2}) → ({endPoint.X:F2}, {lastTurnY:F2})");
                        }
                        else
                        {
                            double midX = (startPoint.X + endPoint.X) / 2;
                            segments.Add(new Point(midX, startPoint.Y));
                            segments.Add(new Point(midX, endPoint.Y));
                            Console.WriteLine($"[PathCreation] 下端口 - 直接垂直路径，转折点: ({midX:F2}, {startPoint.Y:F2}) → ({midX:F2}, {endPoint.Y:F2})");
                        }
                    }
                    else
                    {
                        if (startPoint.Y > endPoint.Y)
                        {
                            double detourX = startPoint.X < nodeLeft ? nodeLeft - _config.NodeMargin : nodeRight + _config.NodeMargin;
                            double lastTurnY = nodeTop - _config.NodeMargin;
                            segments.Add(new Point(detourX, startPoint.Y));
                            segments.Add(new Point(detourX, lastTurnY));
                            segments.Add(new Point(endPoint.X, lastTurnY));
                            Console.WriteLine($"[PathCreation] 上端口 - 上向绕行，转折点: ({detourX:F2}, {startPoint.Y:F2}) → ({detourX:F2}, {lastTurnY:F2}) → ({endPoint.X:F2}, {lastTurnY:F2})");
                        }
                        else
                        {
                            double midX = (startPoint.X + endPoint.X) / 2;
                            segments.Add(new Point(midX, startPoint.Y));
                            segments.Add(new Point(midX, endPoint.Y));
                            Console.WriteLine($"[PathCreation] 上端口 - 直接垂直路径，转折点: ({midX:F2}, {startPoint.Y:F2}) → ({midX:F2}, {endPoint.Y:F2})");
                        }
                    }
                }
            }
            
            Console.WriteLine($"[PathCreation] 简单路径创建完成，转折点数量: {segments.Count}");
            return segments;
        }
        
        private List<Point> CreateDetourPath(Point startPoint, Point endPoint, List<WorkflowNode> obstacles, 
            double obstacleMinY, double obstacleMaxY, double obstacleMinX, double obstacleMaxX, string targetPort, WorkflowNode targetNode)
        {
            var segments = new List<Point>();

            double targetNodeMinX = targetNode.Position.X;
            double targetNodeMaxX = targetNode.Position.X + _config.NodeWidth;
            double targetNodeMinY = targetNode.Position.Y;
            double targetNodeMaxY = targetNode.Position.Y + _config.NodeHeight;

            Console.WriteLine($"[PathCreation] 开始创建绕行路径，目标端口: {targetPort}");
            Console.WriteLine($"[PathCreation] 障碍物边界: X[{obstacleMinX:F2}, {obstacleMaxX:F2}], Y[{obstacleMinY:F2}, {obstacleMaxY:F2}]");
            Console.WriteLine($"[PathCreation] 目标节点边界: X[{targetNodeMinX:F2}, {targetNodeMaxX:F2}], Y[{targetNodeMinY:F2}, {targetNodeMaxY:F2}]");

            if (targetPort == "LeftPort" || targetPort == "RightPort")
            {
                Console.WriteLine($"[PathCreation] 创建水平绕行路径");
                segments = CreateHorizontalDetourPath(startPoint, endPoint, obstacles, obstacleMinY, obstacleMaxY, obstacleMinX, obstacleMaxX, 
                    targetPort, targetNode, targetNodeMinX, targetNodeMaxX, targetNodeMinY, targetNodeMaxY);
            }
            else
            {
                Console.WriteLine($"[PathCreation] 创建垂直绕行路径");
                segments = CreateVerticalDetourPath(startPoint, endPoint, obstacles, obstacleMinY, obstacleMaxY, obstacleMinX, obstacleMaxX, 
                    targetPort, targetNode, targetNodeMinX, targetNodeMaxX, targetNodeMinY, targetNodeMaxY);
            }

            Console.WriteLine($"[PathCreation] 绕行路径创建完成，转折点数量: {segments.Count}");
            return segments;
        }
        
        private List<Point> CreateHorizontalDetourPath(Point startPoint, Point endPoint, List<WorkflowNode> obstacles, 
            double obstacleMinY, double obstacleMaxY, double obstacleMinX, double obstacleMaxX, string targetPort, WorkflowNode targetNode, 
            double targetNodeMinX, double targetNodeMaxX, double targetNodeMinY, double targetNodeMaxY)
        {
            var segments = new List<Point>();
            double detourOffset = _config.PathOffset + 20;
            
            double turnPointX = CalculateHorizontalTurnPointX(startPoint, endPoint, targetPort, 
                targetNodeMinX, targetNodeMaxX, obstacleMinX, obstacleMaxX, detourOffset);
            
            double detourY = CalculateDetourY(startPoint, endPoint, obstacleMinY, obstacleMaxY, 
                targetNodeMinY, targetNodeMaxY, detourOffset);
            
            segments.Add(new Point(startPoint.X, detourY));
            segments.Add(new Point(turnPointX, detourY));
            segments.Add(new Point(turnPointX, endPoint.Y));
            
            Console.WriteLine($"[PathCreation] 水平绕行路径创建完成，转折点: ({startPoint.X:F2}, {detourY:F2}) → ({turnPointX:F2}, {detourY:F2}) → ({turnPointX:F2}, {endPoint.Y:F2})");
            
            return segments;
        }
        
        private List<Point> CreateVerticalDetourPath(Point startPoint, Point endPoint, List<WorkflowNode> obstacles, 
            double obstacleMinY, double obstacleMaxY, double obstacleMinX, double obstacleMaxX, string targetPort, WorkflowNode targetNode, 
            double targetNodeMinX, double targetNodeMaxX, double targetNodeMinY, double targetNodeMaxY)
        {
            var segments = new List<Point>();
            double detourOffset = _config.PathOffset + 20;
            
            double turnPointY = CalculateVerticalTurnPointY(startPoint, endPoint, targetPort, 
                targetNodeMinY, targetNodeMaxY, obstacleMinY, obstacleMaxY, detourOffset);
            
            double detourX = CalculateDetourX(startPoint, endPoint, obstacleMinX, obstacleMaxX, detourOffset);
            
            segments.Add(new Point(detourX, startPoint.Y));
            segments.Add(new Point(detourX, turnPointY));
            segments.Add(new Point(endPoint.X, turnPointY));
            
            Console.WriteLine($"[PathCreation] 垂直绕行路径创建完成，转折点: ({detourX:F2}, {startPoint.Y:F2}) → ({detourX:F2}, {turnPointY:F2}) → ({endPoint.X:F2}, {turnPointY:F2})");
            
            return segments;
        }
        
        private double CalculateHorizontalTurnPointX(Point startPoint, Point endPoint, string targetPort, 
            double targetNodeMinX, double targetNodeMaxX, double obstacleMinX, double obstacleMaxX, double detourOffset)
        {
            double turnPointX;
            
            if (targetPort == "LeftPort")
            {
                if (startPoint.X > endPoint.X)
                {
                    turnPointX = targetNodeMaxX + detourOffset;
                    
                    double distanceToObstacleRight = Math.Abs(startPoint.X - obstacleMaxX);
                    double distanceToObstacleLeft = Math.Abs(startPoint.X - obstacleMinX);
                    
                    if (distanceToObstacleLeft < distanceToObstacleRight)
                    {
                        turnPointX = obstacleMinX - detourOffset;
                        if (turnPointX > targetNodeMinX)
                            turnPointX = targetNodeMaxX + detourOffset;
                    }
                    else
                    {
                        turnPointX = obstacleMaxX + detourOffset;
                        if (turnPointX < targetNodeMinX)
                            turnPointX = targetNodeMaxX + detourOffset;
                    }
                }
                else
                {
                    turnPointX = targetNodeMinX - detourOffset;
                    
                    double distanceToObstacleLeft = Math.Abs(startPoint.X - obstacleMinX);
                    double distanceToObstacleRight = Math.Abs(startPoint.X - obstacleMaxX);
                    
                    if (distanceToObstacleLeft <= distanceToObstacleRight)
                    {
                        turnPointX = obstacleMinX - detourOffset;
                        if (turnPointX > targetNodeMaxX)
                            turnPointX = targetNodeMinX - detourOffset;
                    }
                    else
                    {
                        turnPointX = obstacleMaxX + detourOffset;
                        if (turnPointX < targetNodeMaxX)
                            turnPointX = targetNodeMinX - detourOffset;
                    }
                }
            }
            else
            {
                if (startPoint.X < endPoint.X)
                {
                    turnPointX = targetNodeMinX - detourOffset;
                    
                    double distanceToObstacleLeft = Math.Abs(startPoint.X - obstacleMinX);
                    double distanceToObstacleRight = Math.Abs(startPoint.X - obstacleMaxX);
                    
                    if (distanceToObstacleLeft <= distanceToObstacleRight)
                    {
                        turnPointX = obstacleMinX - detourOffset;
                        if (turnPointX > targetNodeMaxX)
                            turnPointX = targetNodeMinX - detourOffset;
                    }
                    else
                    {
                        turnPointX = obstacleMaxX + detourOffset;
                        if (turnPointX < targetNodeMaxX)
                            turnPointX = targetNodeMinX - detourOffset;
                    }
                }
                else
                {
                    turnPointX = targetNodeMaxX + detourOffset;
                    
                    double distanceToObstacleRight = Math.Abs(startPoint.X - obstacleMaxX);
                    double distanceToObstacleLeft = Math.Abs(startPoint.X - obstacleMinX);
                    
                    if (distanceToObstacleRight <= distanceToObstacleLeft)
                    {
                        turnPointX = obstacleMaxX + detourOffset;
                        if (turnPointX < targetNodeMinX)
                            turnPointX = targetNodeMaxX + detourOffset;
                    }
                    else
                    {
                        turnPointX = obstacleMinX - detourOffset;
                        if (turnPointX > targetNodeMinX)
                            turnPointX = targetNodeMaxX + detourOffset;
                    }
                }
            }
            
            return turnPointX;
        }
        
        private double CalculateVerticalTurnPointY(Point startPoint, Point endPoint, string targetPort, 
            double targetNodeMinY, double targetNodeMaxY, double obstacleMinY, double obstacleMaxY, double detourOffset)
        {
            double turnPointY;
            
            if (targetPort == "TopPort")
            {
                if (startPoint.Y < endPoint.Y)
                {
                    if (endPoint.Y > obstacleMaxY)
                    {
                        if (targetNodeMinY > obstacleMaxY)
                            turnPointY = obstacleMaxY + detourOffset;
                        else
                            turnPointY = obstacleMinY - detourOffset;
                    }
                    else if (endPoint.Y < obstacleMinY)
                    {
                        turnPointY = obstacleMinY - detourOffset;
                    }
                    else
                    {
                        double distanceToTop = Math.Abs(startPoint.Y - obstacleMinY);
                        double distanceToBottom = Math.Abs(startPoint.Y - obstacleMaxY);
                        turnPointY = distanceToTop < distanceToBottom ? obstacleMinY - detourOffset : obstacleMaxY + detourOffset;
                    }
                }
                else
                {
                    turnPointY = obstacleMinY - detourOffset;
                    if (turnPointY > targetNodeMinY)
                        turnPointY = targetNodeMinY - detourOffset;
                }
            }
            else
            {
                if (startPoint.Y > endPoint.Y)
                {
                    if (endPoint.Y < obstacleMinY)
                    {
                        if (targetNodeMinY > obstacleMaxY)
                            turnPointY = obstacleMaxY + detourOffset;
                        else
                            turnPointY = obstacleMinY - detourOffset;
                    }
                    else if (endPoint.Y > obstacleMaxY)
                    {
                        turnPointY = obstacleMaxY + detourOffset;
                    }
                    else
                    {
                        double distanceToTop = Math.Abs(startPoint.Y - obstacleMinY);
                        double distanceToBottom = Math.Abs(startPoint.Y - obstacleMaxY);
                        turnPointY = distanceToTop < distanceToBottom ? obstacleMinY - detourOffset : obstacleMaxY + detourOffset;
                    }
                }
                else
                {
                    turnPointY = obstacleMaxY + detourOffset;
                    if (turnPointY < targetNodeMaxY)
                        turnPointY = targetNodeMaxY + detourOffset;
                }
            }
            
            if (turnPointY >= targetNodeMinY && turnPointY <= targetNodeMaxY)
            {
                double distanceToTop = Math.Abs(turnPointY - targetNodeMinY);
                double distanceToBottom = Math.Abs(turnPointY - targetNodeMaxY);
                turnPointY = distanceToTop <= distanceToBottom ? targetNodeMinY - detourOffset : targetNodeMaxY + detourOffset;
            }
            else
            {
                if (targetPort == "TopPort" && turnPointY > targetNodeMinY)
                    turnPointY = targetNodeMinY - detourOffset;
                else if (targetPort == "BottomPort" && turnPointY < targetNodeMaxY)
                    turnPointY = targetNodeMaxY + detourOffset;
            }
            
            return turnPointY;
        }
        
        private double CalculateDetourY(Point startPoint, Point endPoint, double obstacleMinY, double obstacleMaxY, 
            double targetNodeMinY, double targetNodeMaxY, double detourOffset)
        {
            double pathY = (startPoint.Y + endPoint.Y) / 2;
            double detourY;
            
            if (pathY < obstacleMinY)
                detourY = obstacleMinY - detourOffset;
            else if (pathY > obstacleMaxY)
                detourY = obstacleMaxY + detourOffset;
            else
            {
                double distanceToTop = pathY - obstacleMinY;
                double distanceToBottom = obstacleMaxY - pathY;
                detourY = distanceToTop < distanceToBottom ? obstacleMinY - detourOffset : obstacleMaxY + detourOffset;
            }
            
            if (detourY >= targetNodeMinY && detourY <= targetNodeMaxY)
            {
                double distanceToTop = Math.Abs(detourY - targetNodeMinY);
                double distanceToBottom = Math.Abs(detourY - targetNodeMaxY);
                detourY = distanceToTop <= distanceToBottom ? targetNodeMinY - detourOffset : targetNodeMaxY + detourOffset;
            }
            
            return detourY;
        }
        
        private double CalculateDetourX(Point startPoint, Point endPoint, double obstacleMinX, double obstacleMaxX, double detourOffset)
        {
            double pathX = (startPoint.X + endPoint.X) / 2;
            double detourX;
            
            if (pathX < obstacleMinX)
                detourX = obstacleMinX - detourOffset;
            else if (pathX > obstacleMaxX)
                detourX = obstacleMaxX + detourOffset;
            else
            {
                double distanceToLeft = pathX - obstacleMinX;
                double distanceToRight = obstacleMaxX - pathX;
                detourX = distanceToLeft < distanceToRight ? obstacleMinX - detourOffset : obstacleMaxX + detourOffset;
            }
            
            return detourX;
        }
        
        private bool LineSegmentIntersectsRect(Point p1, Point p2, Rect rect)
        {
            if (rect.Contains(p1) || rect.Contains(p2))
                return true;
            
            var topLeft = new Point(rect.X, rect.Y);
            var topRight = new Point(rect.X + rect.Width, rect.Y);
            var bottomLeft = new Point(rect.X, rect.Y + rect.Height);
            var bottomRight = new Point(rect.X + rect.Width, rect.Y + rect.Height);
            
            return LineSegmentsIntersect(p1, p2, topLeft, topRight) ||
                   LineSegmentsIntersect(p1, p2, topRight, bottomRight) ||
                   LineSegmentsIntersect(p1, p2, bottomRight, bottomLeft) ||
                   LineSegmentsIntersect(p1, p2, bottomLeft, topLeft);
        }
        
        private bool LineSegmentsIntersect(Point p1, Point p2, Point p3, Point p4)
        {
            double d = (p2.X - p1.X) * (p4.Y - p3.Y) - (p2.Y - p1.Y) * (p4.X - p3.X);
            if (d == 0)
                return false;
            
            double t = ((p3.X - p1.X) * (p4.Y - p3.Y) - (p3.Y - p1.Y) * (p4.X - p3.X)) / d;
            double u = ((p3.X - p1.X) * (p2.Y - p1.Y) - (p3.Y - p1.Y) * (p2.X - p1.X)) / d;
            
            return t >= 0 && t <= 1 && u >= 0 && u <= 1;
        }
        
        public void ClearCache()
        {
            _pathCache.Clear();
            _connectionAngles.Clear();
            _cacheTimestamps.Clear();
        }
        
        public void InvalidateNode(string nodeId)
        {
            // 智能缓存失效：只清除与该节点相关的缓存
            var keysToRemove = new List<string>();
            
            foreach (var key in _pathCache.Keys)
            {
                // 简单的缓存键分析，实际项目中可能需要更复杂的缓存键设计
                // 这里假设缓存键中包含节点ID信息
                if (key.Contains(nodeId))
                {
                    keysToRemove.Add(key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _pathCache.Remove(key);
                _cacheTimestamps.Remove(key);
            }
            
            // 如果没有找到相关缓存，或者移除的缓存较少，保留其他缓存
            // 否则清除所有缓存
            if (keysToRemove.Count == 0 || keysToRemove.Count < _pathCache.Count * 0.3)
            {
                // 只清除相关缓存
            }
            else
            {
                ClearCache();
            }
        }
        
        public string GetCacheStatistics()
        {
            int recentCacheItems = _cacheTimestamps.Count(kv => kv.Value > DateTime.Now.AddMinutes(-5));
            return $"Path cache size: {_pathCache.Count}, Recent items (5min): {recentCacheItems}, Connection angles: {_connectionAngles.Count}";
        }
    }
    
    public class PathConfiguration
    {
        public double ControlOffset { get; set; } = 60;
        public double GridSize { get; set; } = 20;
        public double NodeMargin { get; set; } = 35;
        public double ArrowSize { get; set; } = 10;
        public double PathOffset { get; set; } = 25;
        public double NodeWidth { get; set; } = 140;
        public double NodeHeight { get; set; } = 90;
        public bool EnableDebugLog { get; set; } = false;
        public int QuadTreeMaxObjects { get; set; } = 10;
        public int QuadTreeMaxLevels { get; set; } = 5;
    }
    
    public class QuadTree
    {
        private readonly int _maxObjects;
        private readonly int _maxLevels;
        private readonly int _level;
        private readonly List<WorkflowNode> _objects;
        private readonly Rect _bounds;
        private readonly QuadTree[] _nodes;
        
        public QuadTree(Rect bounds, int maxObjects = 10, int maxLevels = 5, int level = 0)
        {
            _maxObjects = maxObjects;
            _maxLevels = maxLevels;
            _level = level;
            _objects = new List<WorkflowNode>();
            _bounds = bounds;
            _nodes = new QuadTree[4];
        }
        
        public void Clear()
        {
            _objects.Clear();
            
            for (int i = 0; i < _nodes.Length; i++)
            {
                if (_nodes[i] != null)
                {
                    _nodes[i].Clear();
                    _nodes[i] = null;
                }
            }
        }
        
        private void Split()
        {
            double subWidth = _bounds.Width / 2;
            double subHeight = _bounds.Height / 2;
            double x = _bounds.X;
            double y = _bounds.Y;
            
            _nodes[0] = new QuadTree(new Rect(x + subWidth, y, subWidth, subHeight), _level + 1);
            _nodes[1] = new QuadTree(new Rect(x, y, subWidth, subHeight), _level + 1);
            _nodes[2] = new QuadTree(new Rect(x, y + subHeight, subWidth, subHeight), _level + 1);
            _nodes[3] = new QuadTree(new Rect(x + subWidth, y + subHeight, subWidth, subHeight), _level + 1);
        }
        
        private int GetIndex(Rect rect)
        {
            double verticalMidpoint = _bounds.X + (_bounds.Width / 2);
            double horizontalMidpoint = _bounds.Y + (_bounds.Height / 2);
            
            bool topQuadrant = (rect.Y < horizontalMidpoint) && (rect.Y + rect.Height < horizontalMidpoint);
            bool bottomQuadrant = (rect.Y > horizontalMidpoint);
            
            if (rect.X < verticalMidpoint && rect.X + rect.Width < verticalMidpoint)
            {
                if (topQuadrant)
                    return 1;
                if (bottomQuadrant)
                    return 2;
            }
            else if (rect.X > verticalMidpoint)
            {
                if (topQuadrant)
                    return 0;
                if (bottomQuadrant)
                    return 3;
            }
            
            return -1;
        }
        
        public void Insert(WorkflowNode node, Rect nodeBounds)
        {
            if (_nodes[0] != null)
            {
                int index = GetIndex(nodeBounds);
                if (index != -1)
                {
                    _nodes[index].Insert(node, nodeBounds);
                    return;
                }
            }
            
            _objects.Add(node);
            
            if (_objects.Count > _maxObjects && _level < _maxLevels)
            {
                if (_nodes[0] == null)
                    Split();
                
                int i = 0;
                while (i < _objects.Count)
                {
                    var objBounds = new Rect(
                        _objects[i].Position.X - 35,
                        _objects[i].Position.Y - 35,
                        140 + 70,
                        90 + 70
                    );
                    int index = GetIndex(objBounds);
                    if (index != -1)
                    {
                        _nodes[index].Insert(_objects[i], objBounds);
                        _objects.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
        }
        
        public List<WorkflowNode> Query(Rect range)
        {
            var returnObjects = new List<WorkflowNode>();
            Query(range, returnObjects);
            return returnObjects;
        }
        
        private void Query(Rect range, List<WorkflowNode> returnObjects)
        {
            int index = GetIndex(range);
            if (index != -1 && _nodes[0] != null)
            {
                _nodes[index].Query(range, returnObjects);
            }
            
            foreach (var obj in _objects)
            {
                var objBounds = new Rect(
                    obj.Position.X - 35,
                    obj.Position.Y - 35,
                    140 + 70,
                    90 + 70
                );
                if (objBounds.IntersectsWith(range))
                {
                    returnObjects.Add(obj);
                }
            }
        }
    }
}
