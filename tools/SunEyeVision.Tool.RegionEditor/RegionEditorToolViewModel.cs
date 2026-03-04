using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Logic;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Models;
using SunEyeVision.Plugin.SDK.ViewModels;

namespace SunEyeVision.Tool.RegionEditor
{
    /// <summary>
    /// 区域编辑器ViewModel
    /// </summary>
    public class RegionEditorToolViewModel : ToolViewModelBase
    {
        #region 私有字段

        private bool _enableRealtimePreview = true;
        private uint _defaultDisplayColor = 0xFFFF0000;
        private double _defaultOpacity = 0.3;

        #endregion

        #region 属性

        /// <summary>
        /// 是否启用实时预览
        /// </summary>
        public bool EnableRealtimePreview
        {
            get => _enableRealtimePreview;
            set
            {
                SetProperty(ref _enableRealtimePreview, value);
                SetParamValue("EnableRealtimePreview", value);
            }
        }

        /// <summary>
        /// 默认显示颜色
        /// </summary>
        public uint DefaultDisplayColor
        {
            get => _defaultDisplayColor;
            set
            {
                SetProperty(ref _defaultDisplayColor, value);
                SetParamValue("DefaultDisplayColor", (int)value);
            }
        }

        /// <summary>
        /// 默认透明度
        /// </summary>
        public double DefaultOpacity
        {
            get => _defaultOpacity;
            set
            {
                SetProperty(ref _defaultOpacity, value);
                SetParamValue("DefaultOpacity", value);
            }
        }

        /// <summary>
        /// 区域集合
        /// </summary>
        public ObservableCollection<RegionData> Regions { get; } = new ObservableCollection<RegionData>();

        /// <summary>
        /// 选中的区域
        /// </summary>
        public RegionData? SelectedRegion { get; set; }

        #endregion

        #region 初始化

        public override void Initialize(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
        {
            base.Initialize(toolId, toolPlugin, toolMetadata);
            ToolName = toolMetadata?.DisplayName ?? "区域编辑器";
        }

        #endregion

        #region 参数构建

        /// <summary>
        /// 获取当前运行参数
        /// </summary>
        protected override ToolParameters GetRunParameters()
        {
            return new RegionEditorParameters
            {
                EnableRealtimePreview = this.EnableRealtimePreview,
                DefaultDisplayColor = this.DefaultDisplayColor,
                DefaultOpacity = this.DefaultOpacity,
                Regions = Regions.ToList()
            };
        }

        #endregion

        #region 区域管理

        /// <summary>
        /// 添加绘制模式区域
        /// </summary>
        public void AddDrawingRegion(string name, ShapeType shapeType, double centerX = 100, double centerY = 100,
            double width = 100, double height = 100, double angle = 0, double radius = 50)
        {
            var region = RegionData.CreateDrawingRegion(name, shapeType);
            if (region.Definition is ShapeDefinition shapeDef)
            {
                shapeDef.CenterX = centerX;
                shapeDef.CenterY = centerY;
                shapeDef.Width = width;
                shapeDef.Height = height;
                shapeDef.Angle = angle;
                shapeDef.Radius = radius;
            }
            region.DisplayColor = DefaultDisplayColor;
            region.DisplayOpacity = DefaultOpacity;

            Regions.Add(region);
        }

        /// <summary>
        /// 添加订阅模式区域（按区域订阅）
        /// </summary>
        public void AddSubscribedRegion(string name, string nodeId, string outputName, int? index = null)
        {
            var region = RegionData.CreateSubscribedRegion(name, nodeId, outputName, index);
            region.DisplayColor = DefaultDisplayColor;
            region.DisplayOpacity = DefaultOpacity;

            Regions.Add(region);
        }

        /// <summary>
        /// 添加计算模式区域（按参数订阅）
        /// </summary>
        public void AddComputedRegion(string name, ShapeType targetShapeType)
        {
            var region = RegionData.CreateComputedRegion(name, targetShapeType);
            region.DisplayColor = DefaultDisplayColor;
            region.DisplayOpacity = DefaultOpacity;

            Regions.Add(region);
        }

        /// <summary>
        /// 移除区域
        /// </summary>
        public void RemoveRegion(Guid regionId)
        {
            var region = Regions.FirstOrDefault(r => r.Id == regionId);
            if (region != null)
            {
                Regions.Remove(region);
            }
        }

        /// <summary>
        /// 清除所有区域
        /// </summary>
        public void ClearRegions()
        {
            Regions.Clear();
        }

        /// <summary>
        /// 从数据列表加载区域
        /// </summary>
        public void LoadRegionsFromData(IEnumerable<RegionData> regionDataList)
        {
            Regions.Clear();
            foreach (var data in regionDataList)
            {
                Regions.Add(data);
            }
        }

        /// <summary>
        /// 导出区域数据
        /// </summary>
        public List<RegionData> ExportRegions()
        {
            return Regions.ToList();
        }

        /// <summary>
        /// 解析所有区域
        /// </summary>
        public List<ResolvedRegion> ResolveAllRegions()
        {
            var resolver = new RegionResolver();
            return resolver.ResolveAll(Regions);
        }

        #endregion

        #region 结果处理

        protected override void OnExecutionCompleted(RunResult result)
        {
            base.OnExecutionCompleted(result);

            if (result.IsSuccess && result.ToolResult is RegionEditorResults regionResult)
            {
                StatusMessage = $"执行成功，共 {regionResult.RegionCount} 个区域";
            }
        }

        #endregion
    }
}
