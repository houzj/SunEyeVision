using System;
using System.Collections.Generic;

namespace SunEyeVision.Plugin.SDK.UI.Controls.ROI
{
    /// <summary>
    /// 编辑动作接口
    /// </summary>
    public interface IEditAction
    {
        /// <summary>
        /// 撤销
        /// </summary>
        void Undo(ROIImageEditor editor);

        /// <summary>
        /// 重做
        /// </summary>
        void Redo(ROIImageEditor editor);

        /// <summary>
        /// 动作描述
        /// </summary>
        string Description { get; }
    }

    /// <summary>
    /// 编辑历史管理器
    /// </summary>
    public class EditHistory
    {
        private readonly Stack<IEditAction> _undoStack = new Stack<IEditAction>();
        private readonly Stack<IEditAction> _redoStack = new Stack<IEditAction>();
        private readonly int _maxHistorySize;

        /// <summary>
        /// 最大历史记录数量
        /// </summary>
        public int MaxHistorySize => _maxHistorySize;

        /// <summary>
        /// 可撤销数量
        /// </summary>
        public int UndoCount => _undoStack.Count;

        /// <summary>
        /// 可重做数量
        /// </summary>
        public int RedoCount => _redoStack.Count;

        /// <summary>
        /// 是否可撤销
        /// </summary>
        public bool CanUndo => _undoStack.Count > 0;

        /// <summary>
        /// 是否可重做
        /// </summary>
        public bool CanRedo => _redoStack.Count > 0;

        /// <summary>
        /// 历史变更事件
        /// </summary>
        public event EventHandler? HistoryChanged;

        /// <summary>
        /// 构造函数
        /// </summary>
        public EditHistory(int maxHistorySize = 50)
        {
            _maxHistorySize = maxHistorySize;
        }

        /// <summary>
        /// 添加编辑动作
        /// </summary>
        public void AddAction(IEditAction action)
        {
            _undoStack.Push(action);
            _redoStack.Clear(); // 新动作清除重做栈

            // 限制历史记录大小
            while (_undoStack.Count > _maxHistorySize)
            {
                var temp = new Stack<IEditAction>();
                while (_undoStack.Count > _maxHistorySize / 2)
                {
                    temp.Push(_undoStack.Pop());
                }
                _undoStack.Clear();
                while (temp.Count > 0)
                {
                    _undoStack.Push(temp.Pop());
                }
            }

            OnHistoryChanged();
        }

        /// <summary>
        /// 撤销
        /// </summary>
        public void Undo(ROIImageEditor editor)
        {
            if (!CanUndo) return;

            var action = _undoStack.Pop();
            action.Undo(editor);
            _redoStack.Push(action);

            OnHistoryChanged();
        }

        /// <summary>
        /// 重做
        /// </summary>
        public void Redo(ROIImageEditor editor)
        {
            if (!CanRedo) return;

            var action = _redoStack.Pop();
            action.Redo(editor);
            _undoStack.Push(action);

            OnHistoryChanged();
        }

        /// <summary>
        /// 清除所有历史
        /// </summary>
        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            OnHistoryChanged();
        }

        /// <summary>
        /// 获取撤销描述
        /// </summary>
        public string? GetUndoDescription()
        {
            return CanUndo ? _undoStack.Peek().Description : null;
        }

        /// <summary>
        /// 获取重做描述
        /// </summary>
        public string? GetRedoDescription()
        {
            return CanRedo ? _redoStack.Peek().Description : null;
        }

        private void OnHistoryChanged()
        {
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    #region 具体编辑动作

    /// <summary>
    /// 创建ROI动作
    /// </summary>
    public class CreateROIAction : IEditAction
    {
        private readonly ROI _roi;

        public string Description => $"创建 {_roi.Type}";

        public CreateROIAction(ROI roi)
        {
            _roi = roi;
        }

        public void Undo(ROIImageEditor editor)
        {
            editor.RemoveROI(_roi.ID);
        }

        public void Redo(ROIImageEditor editor)
        {
            editor.AddROI(_roi);
        }
    }

    /// <summary>
    /// 删除ROI动作
    /// </summary>
    public class DeleteROIAction : IEditAction
    {
        private readonly ROI _roi;

        public string Description => $"删除 {_roi.Type}";

        public DeleteROIAction(ROI roi)
        {
            _roi = (ROI)roi.Clone();
        }

        public void Undo(ROIImageEditor editor)
        {
            editor.AddROI(_roi);
        }

        public void Redo(ROIImageEditor editor)
        {
            editor.RemoveROI(_roi.ID);
        }
    }

    /// <summary>
    /// 移动ROI动作
    /// </summary>
    public class MoveROIAction : IEditAction
    {
        private readonly Guid _roiId;
        private readonly System.Windows.Vector _offset;

        public string Description => "移动ROI";

        public MoveROIAction(Guid roiId, System.Windows.Vector offset)
        {
            _roiId = roiId;
            _offset = offset;
        }

        public void Undo(ROIImageEditor editor)
        {
            var roi = editor.GetROI(_roiId);
            if (roi != null)
            {
                roi.Move(-_offset);
                editor.InvalidateVisual();
            }
        }

        public void Redo(ROIImageEditor editor)
        {
            var roi = editor.GetROI(_roiId);
            if (roi != null)
            {
                roi.Move(_offset);
                editor.InvalidateVisual();
            }
        }
    }

    /// <summary>
    /// 调整ROI大小动作
    /// </summary>
    public class ResizeROIAction : IEditAction
    {
        private readonly Guid _roiId;
        private readonly System.Windows.Size _oldSize;
        private readonly System.Windows.Size _newSize;
        private readonly double _oldRadius;
        private readonly double _newRadius;

        public string Description => "调整大小";

        public ResizeROIAction(Guid roiId, System.Windows.Size oldSize, System.Windows.Size newSize, double oldRadius = 0, double newRadius = 0)
        {
            _roiId = roiId;
            _oldSize = oldSize;
            _newSize = newSize;
            _oldRadius = oldRadius;
            _newRadius = newRadius;
        }

        public void Undo(ROIImageEditor editor)
        {
            var roi = editor.GetROI(_roiId);
            if (roi != null)
            {
                roi.Size = _oldSize;
                roi.Radius = _oldRadius;
                editor.InvalidateVisual();
            }
        }

        public void Redo(ROIImageEditor editor)
        {
            var roi = editor.GetROI(_roiId);
            if (roi != null)
            {
                roi.Size = _newSize;
                roi.Radius = _newRadius;
                editor.InvalidateVisual();
            }
        }
    }

    /// <summary>
    /// 批量删除动作
    /// </summary>
    public class BatchDeleteAction : IEditAction
    {
        private readonly List<ROI> _rois;

        public string Description => $"批量删除 {_rois.Count} 个ROI";

        public BatchDeleteAction(IEnumerable<ROI> rois)
        {
            _rois = new List<ROI>();
            foreach (var roi in rois)
            {
                _rois.Add((ROI)roi.Clone());
            }
        }

        public void Undo(ROIImageEditor editor)
        {
            foreach (var roi in _rois)
            {
                editor.AddROI(roi);
            }
        }

        public void Redo(ROIImageEditor editor)
        {
            foreach (var roi in _rois)
            {
                editor.RemoveROI(roi.ID);
            }
        }
    }

    /// <summary>
    /// 清除所有动作
    /// </summary>
    public class ClearAllAction : IEditAction
    {
        private readonly List<ROI> _rois;

        public string Description => "清除所有ROI";

        public ClearAllAction(IEnumerable<ROI> rois)
        {
            _rois = new List<ROI>(rois);
        }

        public void Undo(ROIImageEditor editor)
        {
            foreach (var roi in _rois)
            {
                editor.AddROI(roi);
            }
        }

        public void Redo(ROIImageEditor editor)
        {
            editor.ClearAllROIs();
        }
    }

    /// <summary>
    /// 修改ROI动作（调整大小/旋转）
    /// </summary>
    public class ModifyROIAction : IEditAction
    {
        private readonly Guid _roiId;
        private readonly System.Windows.Rect _originalBounds;
        private readonly double _originalRotation;
        private System.Windows.Rect _newBounds;
        private double _newRotation;
        private System.Windows.Point _originalPosition;
        private System.Windows.Size _originalSize;
        private double _originalRadius;

        public string Description => "修改ROI";

        public ModifyROIAction(ROI roi, System.Windows.Rect originalBounds, double originalRotation)
        {
            _roiId = roi.ID;
            _originalBounds = originalBounds;
            _originalRotation = originalRotation;
            _originalPosition = roi.Position;
            _originalSize = roi.Size;
            _originalRadius = roi.Radius;
        }

        public void CaptureNewState(ROI roi)
        {
            _newBounds = roi.GetBounds();
            _newRotation = roi.Rotation;
        }

        public void Undo(ROIImageEditor editor)
        {
            var roi = editor.GetROI(_roiId);
            if (roi != null)
            {
                roi.Position = new System.Windows.Point(
                    _originalBounds.Left + _originalBounds.Width / 2,
                    _originalBounds.Top + _originalBounds.Height / 2);
                roi.Size = new System.Windows.Size(_originalBounds.Width, _originalBounds.Height);
                roi.Radius = Math.Max(_originalBounds.Width, _originalBounds.Height) / 2;
                roi.Rotation = _originalRotation;
                editor.InvalidateVisual();
            }
        }

        public void Redo(ROIImageEditor editor)
        {
            var roi = editor.GetROI(_roiId);
            if (roi != null)
            {
                roi.Position = new System.Windows.Point(
                    _newBounds.Left + _newBounds.Width / 2,
                    _newBounds.Top + _newBounds.Height / 2);
                roi.Size = new System.Windows.Size(_newBounds.Width, _newBounds.Height);
                roi.Radius = Math.Max(_newBounds.Width, _newBounds.Height) / 2;
                roi.Rotation = _newRotation;
                editor.InvalidateVisual();
            }
        }
    }

    #endregion
}
