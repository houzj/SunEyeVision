using System;
using System.Collections.Generic;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.Validation;

namespace SunEyeVision.Tool.ROIEditor
{
    /// <summary>
    /// ROI编辑器工具插件
    /// </summary>
    [ToolPlugin("roi_editor", "ROIEditor")]
    public class ROIEditorToolPlugin : IToolPlugin
    {
        public string Name => "ROI编辑器";
        public string Version => "1.0.0";
        public string Author => "SunEyeVision";
        public string Description => "创建和编辑感兴趣区域（ROI）";
        public string PluginId => "suneye.roi_editor";
        public string Icon => "📐";
        public List<string> Dependencies => new List<string>();
        public bool IsLoaded { get; private set; }

        public void Initialize() => IsLoaded = true;
        public void Unload() => IsLoaded = false;

        public List<ToolMetadata> GetToolMetadata() => new List<ToolMetadata>
        {
            new ToolMetadata
            {
                Id = "roi_editor",
                Name = "ROIEditor",
                DisplayName = "ROI编辑器",
                Icon = "📐",
                Category = "图像处理",
                Description = "创建和编辑感兴趣区域（ROI）",
                Version = Version,
                Author = Author,
                InputParameters = new List<ParameterMetadata>
                {
                    new ParameterMetadata
                    {
                        Name = "Mode",
                        DisplayName = "编辑模式",
                        Description = "编辑模式：Edit 或 Inherit",
                        Type = ParamDataType.String,
                        DefaultValue = "Edit"
                    },
                    new ParameterMetadata
                    {
                        Name = "ShowGrid",
                        DisplayName = "显示网格",
                        Description = "是否显示网格辅助线",
                        Type = ParamDataType.Bool,
                        DefaultValue = false
                    },
                    new ParameterMetadata
                    {
                        Name = "EnableSnap",
                        DisplayName = "启用吸附",
                        Description = "是否启用网格吸附功能",
                        Type = ParamDataType.Bool,
                        DefaultValue = true
                    },
                    new ParameterMetadata
                    {
                        Name = "GridSize",
                        DisplayName = "网格大小",
                        Description = "网格的尺寸",
                        Type = ParamDataType.Int,
                        DefaultValue = 10
                    }
                },
                OutputParameters = new List<ParameterMetadata>
                {
                    new ParameterMetadata
                    {
                        Name = "ROIs",
                        DisplayName = "ROI列表",
                        Description = "编辑后的ROI列表",
                        Type = ParamDataType.String
                    },
                    new ParameterMetadata
                    {
                        Name = "ROICount",
                        DisplayName = "ROI数量",
                        Description = "ROI的数量",
                        Type = ParamDataType.Int
                    }
                }
            }
        };

        public ITool? CreateToolInstance(string toolId) => toolId == "roi_editor" ? new ROIEditorTool() : null;

        public AlgorithmParameters GetDefaultParameters(string toolId)
        {
            var p = new AlgorithmParameters();
            p.Set("Mode", "Edit");
            p.Set("ShowGrid", false);
            p.Set("EnableSnap", true);
            p.Set("GridSize", 10);
            return p;
        }

        public ValidationResult ValidateParameters(string toolId, AlgorithmParameters parameters) => new ValidationResult();
    }
}
