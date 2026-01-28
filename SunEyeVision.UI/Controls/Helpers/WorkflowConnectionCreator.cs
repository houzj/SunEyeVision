using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.ViewModels;

namespace SunEyeVision.UI.Controls.Helpers
{
    /// <summary>
    /// 工作流连接创建器 - 负责节点连接的创建逻辑
    /// </summary>
    public class WorkflowConnectionCreator
    {
        private readonly MainWindowViewModel? _viewModel;

        public WorkflowConnectionCreator(MainWindowViewModel? viewModel)
        {
            _viewModel = viewModel;
        }

        /// <summary>
        /// 创建节点连接（使用指定的目标端口）
        /// </summary>
        public WorkflowConnection? CreateConnectionWithSpecificPort(
            WorkflowNode sourceNode, 
            WorkflowNode targetNode, 
            string sourcePortName,
            string targetPortName,
            WorkflowTabViewModel? currentTab)
        {
            System.Diagnostics.Debug.WriteLine("[CreateConnectionWithSpecificPort] ========== 开始创建连接（指定端口） ==========");

            if (currentTab == null || currentTab.WorkflowConnections == null)
            {
                System.Diagnostics.Debug.WriteLine("[CreateConnectionWithSpecificPort] ❌ CurrentTab或WorkflowConnections为null");
                return null;
            }

            var connectionId = $"conn_{Guid.NewGuid().ToString("N")[..8]}";
            var newConnection = new WorkflowConnection(connectionId, sourceNode.Id, targetNode.Id);
            System.Diagnostics.Debug.WriteLine($"[CreateConnectionWithSpecificPort] 新连接ID: {connectionId}");

            // 设置端口名称
            newConnection.SourcePort = sourcePortName;
            newConnection.TargetPort = targetPortName;

            // 获取源端口位置
            Point sourcePos = GetPortPosition(sourceNode, sourcePortName);
            
            // 获取目标端口位置
            Point targetPos = GetPortPosition(targetNode, targetPortName);

            newConnection.SourcePosition = sourcePos;
            newConnection.TargetPosition = targetPos;

            // 计算箭头位置（从目标端口向目标节点方向偏移一定距离）
            Point arrowPos = CalculateArrowPosition(targetPos, targetNode);
            newConnection.ArrowPosition = arrowPos;

            // 计算箭头角度
            double arrowAngle = CalculateArrowAngle(targetPos, targetNode, targetPortName);
            newConnection.ArrowAngle = arrowAngle;

            System.Diagnostics.Debug.WriteLine($"[CreateConnectionWithSpecificPort] 源端口:{sourcePortName} 位置:{sourcePos}");
            System.Diagnostics.Debug.WriteLine($"[CreateConnectionWithSpecificPort] 目标端口:{targetPortName} 位置:{targetPos}");
            System.Diagnostics.Debug.WriteLine($"[CreateConnectionWithSpecificPort] 箭头位置:{arrowPos}");
            System.Diagnostics.Debug.WriteLine($"[CreateConnectionWithSpecificPort] 箭头角度:{arrowAngle:F1}°");

            currentTab.WorkflowConnections.Add(newConnection);
            System.Diagnostics.Debug.WriteLine("[CreateConnectionWithSpecificPort] ✓ 连接创建完成");

            return newConnection;
        }

        /// <summary>
        /// 创建节点连接（智能选择端口）
        /// </summary>
        public WorkflowConnection? CreateConnection(
            WorkflowNode sourceNode, 
            WorkflowNode targetNode,
            string initialSourcePort,
            WorkflowTabViewModel? currentTab)
        {
            System.Diagnostics.Debug.WriteLine("[CreateConnection] ========== 开始创建连接 ==========");

            if (currentTab == null)
            {
                System.Diagnostics.Debug.WriteLine("[CreateConnection] ❌ CurrentTab为null");
                return null;
            }

            if (currentTab.WorkflowConnections == null)
            {
                System.Diagnostics.Debug.WriteLine("[CreateConnection] ❌ WorkflowConnections为null");
                return null;
            }

            System.Diagnostics.Debug.WriteLine($"[CreateConnection] 源节点: {sourceNode.Name} (ID={sourceNode.Id}), 位置: {sourceNode.Position}");
            System.Diagnostics.Debug.WriteLine($"[CreateConnection] 目标节点: {targetNode.Name} (ID={targetNode.Id}), 位置: {targetNode.Position}");

            var connectionId = $"conn_{Guid.NewGuid().ToString("N")[..8]}";
            var newConnection = new WorkflowConnection(connectionId, sourceNode.Id, targetNode.Id);
            System.Diagnostics.Debug.WriteLine($"[CreateConnection] 新连接ID: {connectionId}");

            // 智能选择连接点位置
            var (sourcePos, targetPos, finalSourcePort, finalTargetPort) = 
                CalculateSmartPortPositions(sourceNode, targetNode, initialSourcePort);

            System.Diagnostics.Debug.WriteLine("[CreateConnection] ========== 最终端口配置 ==========");
            System.Diagnostics.Debug.WriteLine($"[CreateConnection] 最终源端口: {finalSourcePort}");
            System.Diagnostics.Debug.WriteLine($"[CreateConnection] 最终目标端口: {finalTargetPort}");
            System.Diagnostics.Debug.WriteLine($"[CreateConnection] 最终源位置: ({sourcePos.X:F1}, {sourcePos.Y:F1})");
            System.Diagnostics.Debug.WriteLine($"[CreateConnection] 最终目标位置: ({targetPos.X:F1}, {targetPos.Y:F1})");
            System.Diagnostics.Debug.WriteLine("[CreateConnection] =======================================");

            newConnection.SourcePort = finalSourcePort;
            newConnection.TargetPort = finalTargetPort;
            newConnection.SourcePosition = sourcePos;
            newConnection.TargetPosition = targetPos;

            // 计算箭头位置
            Point arrowPos = CalculateArrowPosition(targetPos, targetNode);
            newConnection.ArrowPosition = arrowPos;

            // 计算箭头角度
            double arrowAngle = CalculateArrowAngle(targetPos, targetNode, finalTargetPort);
            newConnection.ArrowAngle = arrowAngle;

            System.Diagnostics.Debug.WriteLine("[CreateConnection] ========== 连接属性设置 ==========");
            System.Diagnostics.Debug.WriteLine($"[CreateConnection] newConnection.SourcePort = {finalSourcePort}");
            System.Diagnostics.Debug.WriteLine($"[CreateConnection] newConnection.TargetPort = {finalTargetPort}");
            System.Diagnostics.Debug.WriteLine($"[CreateConnection] newConnection.SourcePosition = ({sourcePos.X:F1}, {sourcePos.Y:F1})");
            System.Diagnostics.Debug.WriteLine($"[CreateConnection] newConnection.TargetPosition = ({targetPos.X:F1}, {targetPos.Y:F1})");
            System.Diagnostics.Debug.WriteLine($"[CreateConnection] newConnection.ArrowPosition = ({arrowPos.X:F1}, {arrowPos.Y:F1})");
            System.Diagnostics.Debug.WriteLine($"[CreateConnection] newConnection.ArrowAngle = {arrowAngle:F1}°");

            // 验证端口位置是否正确
            ValidatePortPositions(sourceNode, targetNode, finalSourcePort, finalTargetPort, sourcePos, targetPos);

            // 关键信息：添加前后的连接数
            int beforeCount = currentTab.WorkflowConnections.Count;
            System.Diagnostics.Debug.WriteLine($"[CreateConnection] 添加前连接数: {beforeCount}");

            currentTab.WorkflowConnections.Add(newConnection);

            int afterCount = currentTab.WorkflowConnections.Count;
            System.Diagnostics.Debug.WriteLine($"[CreateConnection] 添加后连接数: {afterCount}");

            // 关键信息：验证连接是否真的在集合中
            var addedConnection = currentTab.WorkflowConnections.FirstOrDefault(c => c.Id == connectionId);
            if (addedConnection != null)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateConnection] ✓ 连接验证成功，ID: {addedConnection.Id}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[CreateConnection] ❌ 连接验证失败，ID: {connectionId}");
            }

            _viewModel!.StatusText = $"成功连接: {sourceNode.Name} -> {targetNode.Name}";
            System.Diagnostics.Debug.WriteLine("[CreateConnection] ========== 连接创建完成 ==========");

            return newConnection;
        }

        /// <summary>
        /// 计算智能端口位置
        /// </summary>
        private (Point sourcePos, Point targetPos, string sourcePort, string targetPort) 
            CalculateSmartPortPositions(WorkflowNode sourceNode, WorkflowNode targetNode, string initialSourcePort)
        {
            Point sourcePos, targetPos;
            string finalSourcePort, finalTargetPort;

            // 获取初始源端口位置
            sourcePos = GetPortPosition(sourceNode, initialSourcePort);

            // 选择目标端口（根据源端口方向和目标节点位置选择最近的端口）
            var deltaX = targetNode.Position.X - sourcePos.X;
            var deltaY = targetNode.Position.Y - sourcePos.Y;

            string direction = "";
            bool isVerticalDominant = initialSourcePort == "TopPort" || initialSourcePort == "BottomPort";

            if (isVerticalDominant)
            {
                // 源端口是垂直方向（Top/Bottom），优先选择垂直方向的目标端口
                bool horizontalDominant = Math.Abs(deltaX) > 2 * Math.Abs(deltaY);

                if (horizontalDominant)
                {
                    direction = "水平（源垂直但水平偏移过大）";
                    if (deltaX > 0)
                    {
                        finalSourcePort = "RightPort";
                        finalTargetPort = "LeftPort";
                        sourcePos = sourceNode.RightPortPosition;
                        targetPos = targetNode.LeftPortPosition;
                    }
                    else
                    {
                        finalSourcePort = "LeftPort";
                        finalTargetPort = "RightPort";
                        sourcePos = sourceNode.LeftPortPosition;
                        targetPos = targetNode.RightPortPosition;
                    }
                }
                else
                {
                    direction = "垂直（源端口主导）";
                    if (deltaY > 0)
                    {
                        finalSourcePort = "BottomPort";
                        finalTargetPort = "TopPort";
                        sourcePos = sourceNode.BottomPortPosition;
                        targetPos = targetNode.TopPortPosition;
                    }
                    else
                    {
                        finalSourcePort = "TopPort";
                        finalTargetPort = "BottomPort";
                        sourcePos = sourceNode.TopPortPosition;
                        targetPos = targetNode.BottomPortPosition;
                    }
                }
            }
            else
            {
                // 源端口是水平方向（Left/Right），优先选择水平方向的目标端口
                bool verticalDominant = Math.Abs(deltaY) > 2 * Math.Abs(deltaX);

                if (verticalDominant)
                {
                    direction = "垂直（源水平但垂直偏移过大）";
                    if (deltaY > 0)
                    {
                        finalSourcePort = "BottomPort";
                        finalTargetPort = "TopPort";
                        sourcePos = sourceNode.BottomPortPosition;
                        targetPos = targetNode.TopPortPosition;
                    }
                    else
                    {
                        finalSourcePort = "TopPort";
                        finalTargetPort = "BottomPort";
                        sourcePos = sourceNode.TopPortPosition;
                        targetPos = targetNode.BottomPortPosition;
                    }
                }
                else
                {
                    direction = "水平（源端口主导）";
                    if (deltaX > 0)
                    {
                        finalSourcePort = "RightPort";
                        finalTargetPort = "LeftPort";
                        sourcePos = sourceNode.RightPortPosition;
                        targetPos = targetNode.LeftPortPosition;
                    }
                    else
                    {
                        finalSourcePort = "LeftPort";
                        finalTargetPort = "RightPort";
                        sourcePos = sourceNode.LeftPortPosition;
                        targetPos = targetNode.RightPortPosition;
                    }
                }
            }

            return (sourcePos, targetPos, finalSourcePort, finalTargetPort);
        }

        /// <summary>
        /// 验证端口位置是否正确
        /// </summary>
        private void ValidatePortPositions(
            WorkflowNode sourceNode, 
            WorkflowNode targetNode, 
            string finalSourcePort, 
            string finalTargetPort,
            Point sourcePos, 
            Point targetPos)
        {
            // 已删除详细验证日志，避免信息干扰
        }

        /// <summary>
        /// 检查连接是否已存在
        /// </summary>
        public bool ConnectionExists(WorkflowTabViewModel? currentTab, string sourceNodeId, string targetNodeId)
        {
            if (currentTab?.WorkflowConnections == null) return false;

            return currentTab.WorkflowConnections.Any(c => 
                c.SourceNodeId == sourceNodeId && 
                c.TargetNodeId == targetNodeId);
        }

        /// <summary>
        /// 检查是否为自连接
        /// </summary>
        public bool IsSelfConnection(string sourceNodeId, string targetNodeId)
        {
            return sourceNodeId == targetNodeId;
        }

        /// <summary>
        /// 获取指定端口的位置
        /// </summary>
        private static Point GetPortPosition(WorkflowNode node, string portName)
        {
            return portName switch
            {
                "TopPort" => node.TopPortPosition,
                "BottomPort" => node.BottomPortPosition,
                "LeftPort" => node.LeftPortPosition,
                "RightPort" => node.RightPortPosition,
                _ => node.Position // 默认返回节点中心
            };
        }

        /// <summary>
        /// 计算箭头位置
        /// </summary>
        private static Point CalculateArrowPosition(Point targetPortPos, WorkflowNode targetNode)
        {
            // 箭头偏移量（从目标端口向目标节点方向偏移）
            const double ArrowOffset = 10;

            // 判断目标端口在节点的哪个方向
            var deltaX = targetPortPos.X - targetNode.Position.X;
            var deltaY = targetPortPos.Y - targetNode.Position.Y;

            // 箭头位置 = 目标端口位置 - 箭头偏移（向节点方向）
            Point arrowPos;
            if (Math.Abs(deltaX) > Math.Abs(deltaY))
            {
                // 水平方向
                if (deltaX < 0) // LeftPort
                    arrowPos = new Point(targetPortPos.X + ArrowOffset, targetPortPos.Y);
                else // RightPort
                    arrowPos = new Point(targetPortPos.X - ArrowOffset, targetPortPos.Y);
            }
            else
            {
                // 垂直方向
                if (deltaY < 0) // TopPort
                    arrowPos = new Point(targetPortPos.X, targetPortPos.Y + ArrowOffset);
                else // BottomPort
                    arrowPos = new Point(targetPortPos.X, targetPortPos.Y - ArrowOffset);
            }

            return arrowPos;
        }

        /// <summary>
        /// 计算箭头角度（度）
        /// </summary>
        private static double CalculateArrowAngle(Point targetPortPos, WorkflowNode targetNode, string targetPortName)
        {
            // 箭头默认指向右方（0度），根据端口方向旋转
            return targetPortName switch
            {
                "TopPort" => 270,    // 指向上方
                "BottomPort" => 90,   // 指向下方
                "LeftPort" => 180,    // 指向左方
                "RightPort" => 0,     // 指向右方
                _ => 0
            };
        }
    }
}