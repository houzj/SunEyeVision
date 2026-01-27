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
            _viewModel?.AddLog($"[CreateConnectionWithSpecificPort] ========== 开始创建连接（指定端口） ==========");

            if (currentTab == null || currentTab.WorkflowConnections == null)
            {
                _viewModel?.AddLog("[CreateConnectionWithSpecificPort] ❌ CurrentTab或WorkflowConnections为null");
                return null;
            }

            var connectionId = $"conn_{Guid.NewGuid().ToString("N")[..8]}";
            var newConnection = new WorkflowConnection(connectionId, sourceNode.Id, targetNode.Id);
            _viewModel?.AddLog($"[CreateConnectionWithSpecificPort] 新连接ID: {connectionId}");

            // 设置端口名称
            newConnection.SourcePort = sourcePortName;
            newConnection.TargetPort = targetPortName;

            // 获取源端口位置
            Point sourcePos = GetPortPosition(sourceNode, sourcePortName);
            
            // 获取目标端口位置
            Point targetPos = GetPortPosition(targetNode, targetPortName);

            newConnection.SourcePosition = sourcePos;
            newConnection.TargetPosition = targetPos;

            _viewModel?.AddLog($"[CreateConnectionWithSpecificPort] 源端口:{sourcePortName} 位置:{sourcePos}");
            _viewModel?.AddLog($"[CreateConnectionWithSpecificPort] 目标端口:{targetPortName} 位置:{targetPos}");

            currentTab.WorkflowConnections.Add(newConnection);
            _viewModel?.AddLog($"[CreateConnectionWithSpecificPort] ✓ 连接创建完成");

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
            _viewModel?.AddLog($"[CreateConnection] ========== 开始创建连接 ==========");

            if (currentTab == null)
            {
                _viewModel?.AddLog("[CreateConnection] ❌ CurrentTab为null");
                return null;
            }

            if (currentTab.WorkflowConnections == null)
            {
                _viewModel?.AddLog("[CreateConnection] ❌ WorkflowConnections为null");
                return null;
            }

            _viewModel?.AddLog($"[CreateConnection] 源节点: {sourceNode.Name} (ID={sourceNode.Id}), 位置: {sourceNode.Position}");
            _viewModel?.AddLog($"[CreateConnection] 目标节点: {targetNode.Name} (ID={targetNode.Id}), 位置: {targetNode.Position}");

            var connectionId = $"conn_{Guid.NewGuid().ToString("N")[..8]}";
            var newConnection = new WorkflowConnection(connectionId, sourceNode.Id, targetNode.Id);
            _viewModel?.AddLog($"[CreateConnection] 新连接ID: {connectionId}");

            // 智能选择连接点位置
            var (sourcePos, targetPos, finalSourcePort, finalTargetPort) = 
                CalculateSmartPortPositions(sourceNode, targetNode, initialSourcePort);

            _viewModel?.AddLog($"[CreateConnection] ========== 最终端口配置 ==========");
            _viewModel?.AddLog($"[CreateConnection] 最终源端口: {finalSourcePort}");
            _viewModel?.AddLog($"[CreateConnection] 最终目标端口: {finalTargetPort}");
            _viewModel?.AddLog($"[CreateConnection] 最终源位置: ({sourcePos.X:F1}, {sourcePos.Y:F1})");
            _viewModel?.AddLog($"[CreateConnection] 最终目标位置: ({targetPos.X:F1}, {targetPos.Y:F1})");
            _viewModel?.AddLog($"[CreateConnection] =======================================");

            newConnection.SourcePort = finalSourcePort;
            newConnection.TargetPort = finalTargetPort;
            newConnection.SourcePosition = sourcePos;
            newConnection.TargetPosition = targetPos;

            _viewModel?.AddLog($"[CreateConnection] ========== 连接属性设置 ==========");
            _viewModel?.AddLog($"[CreateConnection] newConnection.SourcePort = {finalSourcePort}");
            _viewModel?.AddLog($"[CreateConnection] newConnection.TargetPort = {finalTargetPort}");
            _viewModel?.AddLog($"[CreateConnection] newConnection.SourcePosition = ({sourcePos.X:F1}, {sourcePos.Y:F1})");
            _viewModel?.AddLog($"[CreateConnection] newConnection.TargetPosition = ({targetPos.X:F1}, {targetPos.Y:F1})");

            // 验证端口位置是否正确
            ValidatePortPositions(sourceNode, targetNode, finalSourcePort, finalTargetPort, sourcePos, targetPos);

            // 关键信息：添加前后的连接数
            int beforeCount = currentTab.WorkflowConnections.Count;
            _viewModel?.AddLog($"[CreateConnection] 添加前连接数: {beforeCount}");

            currentTab.WorkflowConnections.Add(newConnection);

            int afterCount = currentTab.WorkflowConnections.Count;
            _viewModel?.AddLog($"[CreateConnection] 添加后连接数: {afterCount}");

            // 关键信息：验证连接是否真的在集合中
            var addedConnection = currentTab.WorkflowConnections.FirstOrDefault(c => c.Id == connectionId);
            if (addedConnection != null)
            {
                _viewModel?.AddLog($"[CreateConnection] ✓ 连接验证成功，ID: {addedConnection.Id}");
            }
            else
            {
                _viewModel?.AddLog($"[CreateConnection] ❌ 连接验证失败，ID: {connectionId}");
            }

            _viewModel!.StatusText = $"成功连接: {sourceNode.Name} -> {targetNode.Name}";
            _viewModel.AddLog($"[CreateConnection] ========== 连接创建完成 ==========");

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

            // ========== 关键调试信息：端口选择和调整 ==========
            _viewModel?.AddLog($"[CreateConnection] ========== 端口选择和调整 ==========");

            // 选择目标端口（根据源端口方向和目标节点位置选择最近的端口）
            var deltaX = targetNode.Position.X - sourcePos.X;
            var deltaY = targetNode.Position.Y - sourcePos.Y;

            _viewModel?.AddLog($"[CreateConnection] 初始源端口: {initialSourcePort}");
            _viewModel?.AddLog($"[CreateConnection] 初始源位置: ({sourcePos.X:F1}, {sourcePos.Y:F1})");
            _viewModel?.AddLog($"[CreateConnection] 源节点位置: ({sourceNode.Position.X:F1}, {sourceNode.Position.Y:F1})");
            _viewModel?.AddLog($"[CreateConnection] 目标节点位置: ({targetNode.Position.X:F1}, {targetNode.Position.Y:F1})");
            _viewModel?.AddLog($"[CreateConnection] 节点偏移: delta X={deltaX:F1}, delta Y={deltaY:F1}");
            _viewModel?.AddLog($"[CreateConnection] 偏移比率: |deltaX|/|deltaY| = {(Math.Abs(deltaX) / Math.Abs(deltaY)):F2}");

            string direction = "";
            bool isVerticalDominant = initialSourcePort == "TopPort" || initialSourcePort == "BottomPort";

            _viewModel?.AddLog($"[CreateConnection] 源端口类型: {(isVerticalDominant ? "垂直方向(Top/Bottom)" : "水平方向(Left/Right)")}");

            if (isVerticalDominant)
            {
                // 源端口是垂直方向（Top/Bottom），优先选择垂直方向的目标端口
                bool horizontalDominant = Math.Abs(deltaX) > 2 * Math.Abs(deltaY);
                _viewModel?.AddLog($"[CreateConnection] 判断: |deltaX|({Math.Abs(deltaX):F1}) > 2*|deltaY|({2 * Math.Abs(deltaY):F1}) = {horizontalDominant}");

                if (horizontalDominant)
                {
                    direction = "水平（源垂直但水平偏移过大）";
                    _viewModel?.AddLog($"[CreateConnection] ⚠️ 端口调整: 从{initialSourcePort}调整为水平端口");
                    if (deltaX > 0)
                    {
                        _viewModel?.AddLog($"[CreateConnection] {direction}:源在左，目标在右 -> 源右->目标左");
                        finalSourcePort = "RightPort";
                        finalTargetPort = "LeftPort";
                        sourcePos = sourceNode.RightPortPosition;
                        targetPos = targetNode.LeftPortPosition;
                    }
                    else
                    {
                        _viewModel?.AddLog($"[CreateConnection] {direction}:源在右，目标在左 -> 源左->目标右");
                        finalSourcePort = "LeftPort";
                        finalTargetPort = "RightPort";
                        sourcePos = sourceNode.LeftPortPosition;
                        targetPos = targetNode.RightPortPosition;
                    }
                }
                else
                {
                    direction = "垂直（源端口主导）";
                    _viewModel?.AddLog($"[CreateConnection] 保持垂直端口");
                    if (deltaY > 0)
                    {
                        _viewModel?.AddLog($"[CreateConnection] {direction}:源在上，目标在下 -> 源底->目标顶");
                        finalSourcePort = "BottomPort";
                        finalTargetPort = "TopPort";
                        sourcePos = sourceNode.BottomPortPosition;
                        targetPos = targetNode.TopPortPosition;
                    }
                    else
                    {
                        _viewModel?.AddLog($"[CreateConnection] {direction}:源在下，目标在上 -> 源顶->目标底");
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
                _viewModel?.AddLog($"[CreateConnection] 判断: |deltaY|({Math.Abs(deltaY):F1}) > 2*|deltaX|({2 * Math.Abs(deltaX):F1}) = {verticalDominant}");

                if (verticalDominant)
                {
                    direction = "垂直（源水平但垂直偏移过大）";
                    _viewModel?.AddLog($"[CreateConnection] ⚠️ 端口调整: 从{initialSourcePort}调整为垂直端口");
                    if (deltaY > 0)
                    {
                        _viewModel?.AddLog($"[CreateConnection] {direction}:源在上，目标在下 -> 源底->目标顶");
                        finalSourcePort = "BottomPort";
                        finalTargetPort = "TopPort";
                        sourcePos = sourceNode.BottomPortPosition;
                        targetPos = targetNode.TopPortPosition;
                    }
                    else
                    {
                        _viewModel?.AddLog($"[CreateConnection] {direction}:源在下，目标在上 -> 源顶->目标底");
                        finalSourcePort = "TopPort";
                        finalTargetPort = "BottomPort";
                        sourcePos = sourceNode.TopPortPosition;
                        targetPos = targetNode.BottomPortPosition;
                    }
                }
                else
                {
                    direction = "水平（源端口主导）";
                    _viewModel?.AddLog($"[CreateConnection] 保持水平端口");
                    if (deltaX > 0)
                    {
                        _viewModel?.AddLog($"[CreateConnection] {direction}:源在左，目标在右 -> 源右->目标左");
                        finalSourcePort = "RightPort";
                        finalTargetPort = "LeftPort";
                        sourcePos = sourceNode.RightPortPosition;
                        targetPos = targetNode.LeftPortPosition;
                    }
                    else
                    {
                        _viewModel?.AddLog($"[CreateConnection] {direction}:源在右，目标在左 -> 源左->目标右");
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
        /// 获取节点指定端口的位置
        /// </summary>
        private Point GetPortPosition(WorkflowNode node, string portName)
        {
            return portName switch
            {
                "TopPort" => node.TopPortPosition,
                "BottomPort" => node.BottomPortPosition,
                "LeftPort" => node.LeftPortPosition,
                "RightPort" => node.RightPortPosition,
                _ => node.RightPortPosition
            };
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
            _viewModel?.AddLog($"[CreateConnection] ========== 端口位置验证 ==========");
            
            Point expectedSourcePos = finalSourcePort switch
            {
                "RightPort" => new Point(sourceNode.Position.X + 140, sourceNode.Position.Y + 45),
                "LeftPort" => new Point(sourceNode.Position.X, sourceNode.Position.Y + 45),
                "TopPort" => new Point(sourceNode.Position.X + 70, sourceNode.Position.Y),
                "BottomPort" => new Point(sourceNode.Position.X + 70, sourceNode.Position.Y + 90),
                _ => new Point(0, 0)
            };
            
            Point expectedTargetPos = finalTargetPort switch
            {
                "RightPort" => new Point(targetNode.Position.X + 140, targetNode.Position.Y + 45),
                "LeftPort" => new Point(targetNode.Position.X, targetNode.Position.Y + 45),
                "TopPort" => new Point(targetNode.Position.X + 70, targetNode.Position.Y),
                "BottomPort" => new Point(targetNode.Position.X + 70, targetNode.Position.Y + 90),
                _ => new Point(0, 0)
            };

            bool sourcePosCorrect = Math.Abs(sourcePos.X - expectedSourcePos.X) < 0.1 && Math.Abs(sourcePos.Y - expectedSourcePos.Y) < 0.1;
            bool targetPosCorrect = Math.Abs(targetPos.X - expectedTargetPos.X) < 0.1 && Math.Abs(targetPos.Y - expectedTargetPos.Y) < 0.1;

            _viewModel?.AddLog($"[CreateConnection] 源端口{finalSourcePort}期望位置: ({expectedSourcePos.X:F1}, {expectedSourcePos.Y:F1})");
            _viewModel?.AddLog($"[CreateConnection] 源端口实际位置: ({sourcePos.X:F1}, {sourcePos.Y:F1})");
            _viewModel?.AddLog($"[CreateConnection] 源端口位置正确: {sourcePosCorrect}");
            _viewModel?.AddLog($"[CreateConnection] 目标端口{finalTargetPort}期望位置: ({expectedTargetPos.X:F1}, {expectedTargetPos.Y:F1})");
            _viewModel?.AddLog($"[CreateConnection] 目标端口实际位置: ({targetPos.X:F1}, {targetPos.Y:F1})");
            _viewModel?.AddLog($"[CreateConnection] 目标位置正确: {targetPosCorrect}");
            _viewModel?.AddLog($"[CreateConnection] =======================================");
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
    }
}
