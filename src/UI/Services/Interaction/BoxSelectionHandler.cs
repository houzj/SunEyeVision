using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SunEyeVision.UI.Views.Controls.Common;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.ViewModels;
using SunEyeVision.UI.Services.Canvas;
using SunEyeVision.UI.Services.Interaction;
using SunEyeVision.UI.Views.Controls.Canvas;

namespace SunEyeVision.UI.Services.Interaction
{
    /// <summary>
    /// 框选处理器 - 负责处理矩形框选功�?
    /// </summary>
    public class BoxSelectionHandler
    {
        #region 私有字段

        private readonly System.Windows.Controls.Canvas _canvas;
        private readonly MainWindowViewModel? _viewModel;
        private readonly SelectionBox _selectionBox;

        private bool _isBoxSelecting;
        private Point _startPosition;
        private Point[]? _selectedNodesInitialPositions;
        private bool _isMultiSelectMode;

        #endregion

        #region 事件

        public event EventHandler<BoxSelectionEventArgs>? SelectionStarted;
        public event EventHandler<BoxSelectionEventArgs>? SelectionUpdating;
        public event EventHandler<BoxSelectionEventArgs>? SelectionEnded;

        #endregion

        #region 属�?

        /// <summary>
        /// 是否正在框�?
        /// </summary>
        public bool IsBoxSelecting => _isBoxSelecting;

        /// <summary>
        /// 框选区�?
        /// </summary>
        public Rect SelectionRect { get; private set; }

        /// <summary>
        /// 选中的节点数�?
        /// </summary>
        public int SelectedCount { get; private set; }

        #endregion

        #region 构造函�?

        public BoxSelectionHandler(
            System.Windows.Controls.Canvas canvas,
            MainWindowViewModel? viewModel,
            SelectionBox selectionBox)
        {
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            _viewModel = viewModel;
            _selectionBox = selectionBox ?? throw new ArgumentNullException(nameof(selectionBox));
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 开始框�?
        /// </summary>
        /// <param name="startPosition">起始位置</param>
        /// <param name="isMultiSelectMode">是否多选模�?/param>
        public void StartSelection(Point startPosition, bool isMultiSelectMode = false)
        {
            _isBoxSelecting = true;
            _startPosition = startPosition;
            _isMultiSelectMode = isMultiSelectMode;

            RecordSelectedNodesPositions();

            if (!_isMultiSelectMode)
            {
                ClearAllSelections();
            }

            _selectionBox.StartSelection(startPosition);

            _canvas.CaptureMouse();

            OnSelectionStarted(new BoxSelectionEventArgs(startPosition, Rect.Empty));
        }

        /// <summary>
        /// 更新框选区�?
        /// </summary>
        /// <param name="currentPosition">当前位置</param>
        public void UpdateSelection(Point currentPosition)
        {
            if (!_isBoxSelecting)
            {
                return;
            }

            _selectionBox.UpdateSelection(currentPosition);

            var selectionRect = _selectionBox.GetSelectionRect();
            SelectionRect = selectionRect;

            UpdateNodeSelections(selectionRect);

            OnSelectionUpdating(new BoxSelectionEventArgs(currentPosition, selectionRect));
        }

        /// <summary>
        /// 结束框�?
        /// </summary>
        public void EndSelection()
        {
            if (!_isBoxSelecting)
            {
                return;
            }

            _isBoxSelecting = false;

            _selectionBox.EndSelection();

            OnSelectionEnded(new BoxSelectionEventArgs(_startPosition, SelectionRect));

            _selectedNodesInitialPositions = null;
        }

        /// <summary>
        /// 取消框�?
        /// </summary>
        public void CancelSelection()
        {
            if (!_isBoxSelecting)
            {
                return;
            }

            if (_selectedNodesInitialPositions != null &&
                _viewModel?.WorkflowTabViewModel.SelectedTab != null)
            {
                var selectedNodes = _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes
                    .Where(n => n.IsSelected)
                    .ToList();

                for (int i = 0; i < selectedNodes.Count && i < _selectedNodesInitialPositions.Length; i++)
                {
                    selectedNodes[i].Position = _selectedNodesInitialPositions[i];
                }
            }

            _isBoxSelecting = false;
            _selectionBox.EndSelection();

            _selectedNodesInitialPositions = null;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 记录所有选中节点的初始位�?
        /// </summary>
        private void RecordSelectedNodesPositions()
        {
            if (_viewModel?.WorkflowTabViewModel.SelectedTab == null)
            {
                return;
            }

            var selectedNodes = _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes
                .Where(n => n.IsSelected)
                .ToList();

            if (selectedNodes.Count > 0)
            {
                _selectedNodesInitialPositions = new Point[selectedNodes.Count];
                for (int i = 0; i < selectedNodes.Count; i++)
                {
                    _selectedNodesInitialPositions[i] = selectedNodes[i].Position;
                }
            }
        }

        /// <summary>
        /// 清除所有选择
        /// </summary>
        private void ClearAllSelections()
        {
            if (_viewModel?.WorkflowTabViewModel.SelectedTab == null)
            {
                return;
            }

            CanvasHelper.ClearSelection(_viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes);
        }

        /// <summary>
        /// 更新节点选择状�?
        /// </summary>
        private void UpdateNodeSelections(Rect selectionRect)
        {
            if (_viewModel?.WorkflowTabViewModel.SelectedTab == null)
            {
                return;
            }

            int selectedCount = 0;

            foreach (var node in _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes)
            {
                var nodeBounds = CanvasHelper.GetNodeBounds(node);
                bool isSelected = selectionRect.IntersectsWith(nodeBounds);
                node.IsSelected = isSelected;

                if (isSelected)
                {
                    selectedCount++;
                }
            }

            SelectedCount = selectedCount;
            _selectionBox.SetItemCount(selectedCount);
        }

        /// <summary>
        /// 触发框选开始事�?
        /// </summary>
        private void OnSelectionStarted(BoxSelectionEventArgs e)
        {
            SelectionStarted?.Invoke(this, e);
        }

        /// <summary>
        /// 触发框选更新事�?
        /// </summary>
        private void OnSelectionUpdating(BoxSelectionEventArgs e)
        {
            SelectionUpdating?.Invoke(this, e);
        }

        /// <summary>
        /// 触发框选结束事�?
        /// </summary>
        private void OnSelectionEnded(BoxSelectionEventArgs e)
        {
            SelectionEnded?.Invoke(this, e);
        }

        #endregion
    }

    #region 框选事件参�?

    /// <summary>
    /// 框选事件参�?
    /// </summary>
    public class BoxSelectionEventArgs : EventArgs
    {
        /// <summary>
        /// 起始位置
        /// </summary>
        public Point StartPosition { get; }

        /// <summary>
        /// 框选区�?
        /// </summary>
        public Rect SelectionRect { get; }

        public BoxSelectionEventArgs(Point startPosition, Rect selectionRect)
        {
            StartPosition = startPosition;
            SelectionRect = selectionRect;
        }
    }

    #endregion
}
