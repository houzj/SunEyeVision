using System;
using System.Collections.Generic;
using System.Linq;
using SunEyeVision.PluginSystem;
using SunEyeVision.PluginSystem.Base.Interfaces;
using SunEyeVision.PluginSystem.Base.Models;
using SunEyeVision.PluginSystem.Base.Base;

namespace SunEyeVision.Tools.ImageSaveTool.DTOs
{
    /// <summary>
    /// ImageSaveTool 序列化DTO - 用于JSON持久化
    /// </summary>
    public class ImageSaveToolDTO
    {
        /// <summary>
        /// 工具ID
        /// </summary>
        public string ToolId { get; set; } = string.Empty;

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 图像格式
        /// </summary>
        public string ImageFormat { get; set; } = "PNG";

        /// <summary>
        /// 图像质量 (0-100)
        /// </summary>
        public int ImageQuality { get; set; } = 95;

        /// <summary>
        /// 是否覆盖已有文件
        /// </summary>
        public bool OverwriteExisting { get; set; } = false;

        /// <summary>
        /// 从ToolMetadata创建DTO
        /// </summary>
        public static ImageSaveToolDTO FromToolMetadata(ToolMetadata metadata)
        {
            return new ImageSaveToolDTO
            {
                ToolId = metadata.Id,
                FilePath = metadata.InputParameters
                    ?.FirstOrDefault(p => p.Name == "FilePath")?.DefaultValue?.ToString() ?? string.Empty,
                ImageFormat = metadata.InputParameters
                    ?.FirstOrDefault(p => p.Name == "ImageFormat")?.DefaultValue?.ToString() ?? "PNG",
                ImageQuality = Convert.ToInt32(metadata.InputParameters
                    ?.FirstOrDefault(p => p.Name == "ImageQuality")?.DefaultValue ?? 95),
                OverwriteExisting = Convert.ToBoolean(metadata.InputParameters
                    ?.FirstOrDefault(p => p.Name == "OverwriteExisting")?.DefaultValue ?? false)
            };
        }

        /// <summary>
        /// 转换为ToolMetadata
        /// </summary>
        public ToolMetadata ToToolMetadata()
        {
            return new ToolMetadata
            {
                Id = ToolId,
                Name = "ImageSaveTool",
                DisplayName = "图像保存",
                Description = "将处理后的图像保存到指定路径",
                Icon = "💾",
                Category = "输出",
                NodeType = SunEyeVision.Models.NodeType.Algorithm,
                InputParameters = new List<ParameterMetadata>
                {
                    new ParameterMetadata
                    {
                        Name = "FilePath",
                        DisplayName = "文件路径",
                        Type = ParameterType.String,
                        DefaultValue = FilePath,
                        Description = "图像保存的完整路径",
                        Required = true,
                        Category = "基本参数"
                    },
                    new ParameterMetadata
                    {
                        Name = "ImageFormat",
                        DisplayName = "图像格式",
                        Type = ParameterType.Enum,
                        DefaultValue = ImageFormat,
                        Options = new object[] { "PNG", "JPEG", "BMP", "TIFF" },
                        Description = "保存的图像格式",
                        Required = true,
                        Category = "基本参数"
                    },
                    new ParameterMetadata
                    {
                        Name = "ImageQuality",
                        DisplayName = "图像质量",
                        Type = ParameterType.Int,
                        DefaultValue = ImageQuality,
                        MinValue = 1,
                        MaxValue = 100,
                        Description = "JPEG格式的压缩质量(1-100), 越高质量越好",
                        Required = false,
                        Category = "高级参数"
                    },
                    new ParameterMetadata
                    {
                        Name = "OverwriteExisting",
                        DisplayName = "覆盖已有文件",
                        Type = ParameterType.Bool,
                        DefaultValue = OverwriteExisting,
                        Description = "如果目标文件已存在，是否覆盖",
                        Required = false,
                        Category = "基本参数"
                    }
                },
                HasDebugInterface = true,
                Version = "1.0.0",
                Author = "SunEyeVision"
            };
        }
    }
}
