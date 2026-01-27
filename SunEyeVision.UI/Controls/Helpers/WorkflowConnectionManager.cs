using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.ViewModels;

namespace SunEyeVision.UI.Controls.Helpers
{
    /// <summary>
    /// 工作流连接管理器
    /// 负责节点之间的连接创建、管理和路径计算
    /// </summary>
    public class WorkflowConnectionManager
    {
        private readonly WorkflowCanvasControl _canvasControl;
        private readonly MainWindowViewModel? _viewModel;
        private Services.ConnectionPathCache? _connectionPathCache;

        public WorkflowConnectionManager(WorkflowCanvasControl canvasControl, MainWindowViewModel? viewModel)
        {
            _canvasControl = canvasControl;
            _viewModel = viewModel;
        }

        /// <summary>
        /// 设置连接路径缓存
        /// </summary>
        public void SetConnectionPathCache(Services.ConnectionPathCache cache)
        {
            _connectionPathCache = cache;
        }

        /// <summary>
        /// 刷新所有连接的路径（触发重新计算）
        /// </summary>
        public void RefreshAllConnectionPaths()
        {
            var currentWorkflowTab = GetCurrentWorkflowTab();
            if (currentWorkflowTab == null) return;

            // 标记所有缓存为脏数据
            if (_connectionPathCache != null)
            {
                _connectionPathCache.MarkAllDirty();
            }

            // 触发所有连接的属性变化，强制刷新UI
            foreach (var connection in currentWorkflowTab.WorkflowConnections)
            {
                // 触发 SourcePosition 变化，导致转换器重新计算
                var oldPos = connection.SourcePosition;
                connection.SourcePosition = new Point(oldPos.X + 0.001, oldPos.Y);
                connection.SourcePosition = oldPos;
            }
        }

        /// <summary>
        /// 创建节点连接（使用指定的目标端口）
        /// </summary>
        public void CreateConnectionWithSpecificPort(WorkflowNode sourceNode, WorkflowNode targetNode, string targetPortName, string? sourcePortName)
        {
            _viewModel?.AddLog($"[CreateConnectionWithSpecificPort] ========== 开始创建连接（指定端口） ==========");

            var selectedTab = GetCurrentWorkflowTab();
            if (selectedTab == null || selectedTab.WorkflowConnections == null)
            {
                _viewModel?.AddLog("[CreateConnectionWithSpecificPort] ❌ SelectedTab或WorkflowConnections为null");
                return;
            }

            var connectionId = $"conn_{Guid.NewGuid().ToString("N")[..8]}";
            var newConnection = new WorkflowConnection(connectionId, sourceNode.Id, targetNode.Id);
            _viewModel?.AddLog($"[CreateConnectionWithSpecificPort] 新连接ID: {connectionId}");

            // 设置源端口名称
            newConnection.SourcePort = sourcePortName ?? "RightPort";
            newConnection.TargetPort = targetPortName;

            // 获取源端口位置
            Point sourcePos;
            switch (sourcePortName)
            {
                case "TopPort":
                    sourcePos = sourceNode.TopPortPosition;
                    break;
                case "BottomPort":
                    sourcePos = sourceNode.BottomPortPosition;
                    break;
                case "LeftPort":
                    sourcePos = sourceNode.LeftPortPosition;
                    break;
                case "RightPort":
                    sourcePos = sourceNode.RightPortPosition;
                    break;
                default:
                    sourcePos = sourceNode.RightPortPosition;
                    break;
            }

            // 获取目标端口位置（使用用户指定的端口）
            Point targetPos;
            switch (targetPortName)
            {
                case "TopPort":
                    targetPos = targetNode.TopPortPosition;
                    break;
                case "BottomPort":
                    targetPos = targetNode.BottomPortPosition;
                    break;
                case "LeftPort":
                    targetPos = targetNode.LeftPortPosition;
                    break;
                case "RightPort":
                    targetPos = targetNode.RightPortPosition;
                    break;
                default:
                    targetPos = targetNode.LeftPortPosition;
                    break;
            }

            newConnection.SourcePosition = sourcePos;
            newConnection.TargetPosition = targetPos;

            _viewModel?.AddLog($"[CreateConnectionWithSpecificPort] 源端口:{sourcePortName} 位置:{sourcePos}");
            _viewModel?.AddLog($"[CreateConnectionWithSpecificPort] 目标端口:{targetPortName} 位置:{targetPos}");

            selectedTab.WorkflowConnections.Add(newConnection);
            _viewModel?.AddLog($"[CreateConnectionWithSpecificPort] ✓ 连接创建完成");
        }

        /// <summary>
        /// 创建节点连接
        /// </summary>
        public void CreateConnection(WorkflowNode sourceNode, WorkflowNode targetNode, string? sourcePortName)
        {
            _viewModel?.AddLog($"[CreateConnection] ========== 开始创建连接 ==========");

            var selectedTab = GetCurrentWorkflowTab();
            if (selectedTab == null)
            {
                _viewModel?.AddLog("[CreateConnection] ❌ SelectedTab为null");
                return;
            }

            if (selectedTab.WorkflowConnections == null)
            {
                _viewModel?.AddLog("[CreateConnection] ❌ WorkflowConnections为null");
                return;
            }

            _viewModel?.AddLog($"[CreateConnection] 源节点: {sourceNode.Name} (ID={sourceNode.Id}), 位置: {sourceNode.Position}");
            _viewModel?.AddLog($"[CreateConnection] 目标节点: {targetNode.Name} (ID={targetNode.Id}), 位置: {targetNode.Position}");

            var connectionId = $"conn_{Guid.NewGuid().ToString("N")[..8]}";
            var newConnection = new WorkflowConnection(connectionId, sourceNode.Id, targetNode.Id);
            _viewModel?.AddLog($"[CreateConnection] 新连接ID: {connectionId}");

            // 智能选择连接点位置
            Point sourcePos, targetPos;
            string finalSourcePort, finalTargetPort;

            // 使用记录的源端口
            string initialSourcePort = sourcePortName ?? "RightPort";
            switch (initialSourcePort)
            {
                case "TopPort":
                    sourcePos = sourceNode.TopPortPosition;
                    break;
                case "BottomPort":
                    sourcePos = sourceNode.BottomPortPosition;
                    break;
                case "LeftPort":
                    sourcePos = sourceNode.LeftPortPosition;
                    break;
                case "RightPort":
                    sourcePos = sourceNode.RightPortPosition;
                    break;
                default:
                    sourcePos = sourceNode.RightPortPosition;
                    break;
            }

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

            _viewModel?.AddLog($"[CreateConnection] 最终源端口: {finalSourcePort}, 最终目标端口: {finalTargetPort}");
            _viewModel?.AddLog($"[CreateConnection] 最终源位置: ({sourcePos.X:F1}, {sourcePos.Y:F1})");
            _viewModel?.AddLog($"[CreateConnection] 最终目标位置: ({targetPos.X:F1}, {targetPos.Y:F1})");

            // 设置连接属性
            newConnection.SourcePort = finalSourcePort;
            newConnection.TargetPort = finalTargetPort;
            newConnection.SourcePosition = sourcePos;
            newConnection.TargetPosition = targetPos;

            // 关键信息：添加前后的连接数
            int beforeCount = selectedTab.WorkflowConnections.Count;
            _viewModel?.AddLog($"[CreateConnection] 添加前连接数: {beforeCount}");

            selectedTab.WorkflowConnections.Add(newConnection);

            int afterCount = selectedTab.WorkflowConnections.Count;
            _viewModel?.AddLog($"[CreateConnection] 添加后连接数: {afterCount}");

            // 关键信息：验证连接是否真的在集合中
            var addedConnection = selectedTab.WorkflowConnections.FirstOrDefault(c => c.Id == connectionId);
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
        }

        /// <summary>
        /// 获取当前工作流Tab
        /// </summary>
        private ViewModels.WorkflowTabViewModel? GetCurrentWorkflowTab()
        {
            if (_viewModel != null && _viewModel.WorkflowTabViewModel != null)
            {
                return _viewModel.WorkflowTabViewModel.SelectedTab;
            }
            return null;
        }
    }
}
