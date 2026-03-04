using System;
using System.Collections.Generic;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Models;
using SunEyeVision.Plugin.SDK.Validation;

namespace SunEyeVision.Tool.RegionEditor
{
    /// <summary>
    /// 区域编辑器工具插件
    /// </summary>
    [ToolPlugin("region_editor", "RegionEditor")]
    public class RegionEditorToolPlugin : IToolPlugin
    {
        public string Name => "区域编辑器";
        public string Version => "1.0.0";
        public string Author => "SunEyeVision";
        public string Description => "创建和编辑检测区域，支持多种形状类型";
        public string PluginId => "suneye.region_editor";
        public string Icon => "🔲";
        public List<string> Dependencies => new List<string>();
        public bool IsLoaded { get; private set; }

        public void Initialize() => IsLoaded = true;
        public void Unload() => IsLoaded = false;

        public List<ToolMetadata> GetToolMetadata() => new List<ToolMetadata>
        {
            new ToolMetadata
            {
                Id = "region_editor",
                Name = "RegionEditor",
                DisplayName = "区域编辑器",
                Icon = "🔲",
                Category = "图像处理",
                Description = "创建和编辑检测区域，支持多种形状类型",
                Version = Version,
                Author = Author,
                InputParameters = new List<ParameterMetadata>
                {
                    new ParameterMetadata
                    {
                        Name = "EnableRealtimePreview",
                        DisplayName = "实时预览",
                        Description = "是否启用实时预览功能",
                        Type = ParamDataType.Bool,
                        DefaultValue = true
                    },
                    new ParameterMetadata
                    {
                        Name = "DefaultDisplayColor",
                        DisplayName = "默认颜色",
                        Description = "默认显示颜色（ARGB格式）",
                        Type = ParamDataType.Int,
                        DefaultValue = unchecked((int)0xFFFF0000)
                    },
                    new ParameterMetadata
                    {
                        Name = "DefaultOpacity",
                        DisplayName = "默认透明度",
                        Description = "默认显示透明度（0-1）",
                        Type = ParamDataType.Double,
                        DefaultValue = 0.3
                    }
                },
                OutputParameters = new List<ParameterMetadata>
                {
                    new ParameterMetadata
                    {
                        Name = "Regions",
                        DisplayName = "区域列表",
                        Description = "编辑后的区域列表",
                        Type = ParamDataType.String
                    },
                    new ParameterMetadata
                    {
                        Name = "RegionCount",
                        DisplayName = "区域数量",
                        Description = "区域的数量",
                        Type = ParamDataType.Int
                    }
                }
            }
        };

        public ITool? CreateToolInstance(string toolId) => toolId == "region_editor" ? new RegionEditorTool() : null;

        public AlgorithmParameters GetDefaultParameters(string toolId)
        {
            var p = new AlgorithmParameters();
            p.Set("EnableRealtimePreview", true);
            p.Set("DefaultDisplayColor", 0xFFFF0000);
            p.Set("DefaultOpacity", 0.3);
            return p;
        }

        public ValidationResult ValidateParameters(string toolId, AlgorithmParameters parameters)
        {
            return new ValidationResult();
        }
    }
}
