using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI.Controls.ROI;
using SunEyeVision.Plugin.SDK.ViewModels;

namespace SunEyeVision.Tool.ROIEditor
{
    /// <summary>
    /// ROI编辑器ViewModel
    /// </summary>
    public class ROIEditorToolViewModel : ToolViewModelBase
    {
        #region 私有字段

        private string _mode = "Edit";
        private bool _showGrid = false;
        private bool _enableSnap = true;
        private int _gridSize = 10;

        #endregion

        #region 属性

        /// <summary>
        /// 编辑模式
        /// </summary>
        public string Mode
        {
            get => _mode;
            set
            {
                SetProperty(ref _mode, value);
                SetParamValue("Mode", value);
            }
        }

        /// <summary>
        /// 显示网格
        /// </summary>
        public bool ShowGrid
        {
            get => _showGrid;
            set
            {
                SetProperty(ref _showGrid, value);
                SetParamValue("ShowGrid", value);
            }
        }

        /// <summary>
        /// 启用吸附
        /// </summary>
        public bool EnableSnap
        {
            get => _enableSnap;
            set
            {
                SetProperty(ref _enableSnap, value);
                SetParamValue("EnableSnap", value);
            }
        }

        /// <summary>
        /// 网格大小
        /// </summary>
        public int GridSize
        {
            get => _gridSize;
            set
            {
                SetProperty(ref _gridSize, value);
                SetParamValue("GridSize", value);
            }
        }

        /// <summary>
        /// ROI集合
        /// </summary>
        public ObservableCollection<ROI> ROIs { get; } = new ObservableCollection<ROI>();

        #endregion

        #region 初始化

        public override void Initialize(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
        {
            base.Initialize(toolId, toolPlugin, toolMetadata);
            ToolName = toolMetadata?.DisplayName ?? "ROI编辑器";
        }

        #endregion

        #region 参数构建

        /// <summary>
        /// 获取当前运行参数
        /// </summary>
        protected override ToolParameters GetRunParameters()
        {
            var roiDataList = ROIs.Select(roi => new ROIData
            {
                Type = roi.Type.ToString(),
                X = roi.Position.X,
                Y = roi.Position.Y,
                Width = roi.Size.Width,
                Height = roi.Size.Height,
                Radius = roi.Radius,
                Rotation = roi.Rotation,
                EndX = roi.EndPoint.X,
                EndY = roi.EndPoint.Y,
                Name = roi.Name,
                Tag = roi.Tag
            }).ToList();

            return new ROIEditorParameters
            {
                Mode = this.Mode,
                ShowGrid = this.ShowGrid,
                EnableSnap = this.EnableSnap,
                GridSize = this.GridSize,
                ROIs = roiDataList
            };
        }

        #endregion

        #region ROI管理

        /// <summary>
        /// 添加ROI
        /// </summary>
        public void AddROI(ROI roi)
        {
            ROIs.Add(roi);
        }

        /// <summary>
        /// 移除ROI
        /// </summary>
        public void RemoveROI(Guid roiId)
        {
            var roi = ROIs.FirstOrDefault(r => r.ID == roiId);
            if (roi != null)
            {
                ROIs.Remove(roi);
            }
        }

        /// <summary>
        /// 清除所有ROI
        /// </summary>
        public void ClearROIs()
        {
            ROIs.Clear();
        }

        /// <summary>
        /// 从数据列表加载ROI
        /// </summary>
        public void LoadROIsFromData(IEnumerable<ROIData> roiDataList)
        {
            ROIs.Clear();
            foreach (var data in roiDataList)
            {
                var roi = new ROI
                {
                    Type = Enum.Parse<ROIType>(data.Type),
                    Position = new System.Windows.Point(data.X, data.Y),
                    Size = new System.Windows.Size(data.Width, data.Height),
                    Radius = data.Radius,
                    Rotation = data.Rotation,
                    EndPoint = new System.Windows.Point(data.EndX, data.EndY),
                    Name = data.Name,
                    Tag = data.Tag
                };
                ROIs.Add(roi);
            }
        }

        /// <summary>
        /// 导出ROI数据
        /// </summary>
        public List<ROIData> ExportROIs()
        {
            return ROIs.Select(roi => new ROIData
            {
                Type = roi.Type.ToString(),
                X = roi.Position.X,
                Y = roi.Position.Y,
                Width = roi.Size.Width,
                Height = roi.Size.Height,
                Radius = roi.Radius,
                Rotation = roi.Rotation,
                EndX = roi.EndPoint.X,
                EndY = roi.EndPoint.Y,
                Name = roi.Name,
                Tag = roi.Tag
            }).ToList();
        }

        #endregion

        #region 结果处理

        protected override void OnExecutionCompleted(RunResult result)
        {
            base.OnExecutionCompleted(result);

            if (result.IsSuccess && result.ToolResult is ROIEditorResults roiResult)
            {
                StatusMessage = $"执行成功，共 {roiResult.ROICount} 个ROI";
            }
        }

        #endregion
    }
}
