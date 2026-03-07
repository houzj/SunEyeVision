using System;
using System.Collections.Generic;
using FluentAssertions;
using SunEyeVision.Plugin.Infrastructure.Managers.Tool;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.Validation;
using Xunit;

namespace SunEyeVision.Core.Tests.Integration
{
    /// <summary>
    /// 插件系统集成测试
    /// 验证插件注册、工具管理等完整链路
    /// </summary>
    public class PluginIntegrationTests
    {
        public PluginIntegrationTests()
        {
            // 清理工具注册表
            ToolRegistry.ClearAll();
        }

        #region 工具注册测试

        [Fact]
        public void ToolRegistry_RegisterTool_ShouldStoreMetadata()
        {
            // Arrange
            var metadata = new ToolMetadata
            {
                Id = "test_tool",
                Name = "测试工具",
                Category = "测试分类",
                Description = "测试描述"
            };

            // Act
            ToolRegistry.Register(typeof(TestToolPlugin), metadata);

            // Assert
            ToolRegistry.GetToolMetadata("test_tool").Should().NotBeNull();
            ToolRegistry.GetToolMetadata("test_tool")!.Name.Should().Be("测试工具");
        }

        [Fact]
        public void ToolRegistry_GetAllTools_ShouldReturnAllRegistered()
        {
            // Arrange
            var metadata1 = new ToolMetadata { Id = "tool1", Name = "工具1", Category = "分类1" };
            var metadata2 = new ToolMetadata { Id = "tool2", Name = "工具2", Category = "分类2" };

            // Act
            ToolRegistry.Register(typeof(TestToolPlugin), metadata1);
            ToolRegistry.Register(typeof(TestToolPlugin), metadata2);
            var allTools = ToolRegistry.GetAllToolMetadata();

            // Assert
            allTools.Should().HaveCount(2);
        }

        [Fact]
        public void ToolRegistry_GetToolsByCategory_ShouldFilterCorrectly()
        {
            // Arrange
            ToolRegistry.ClearAll();
            var metadata1 = new ToolMetadata { Id = "tool1", Name = "工具1", Category = "图像处理" };
            var metadata2 = new ToolMetadata { Id = "tool2", Name = "工具2", Category = "图像处理" };
            var metadata3 = new ToolMetadata { Id = "tool3", Name = "工具3", Category = "测量" };

            // Act
            ToolRegistry.Register(typeof(TestToolPlugin), metadata1);
            ToolRegistry.Register(typeof(TestToolPlugin), metadata2);
            ToolRegistry.Register(typeof(TestToolPlugin), metadata3);
            var imageTools = ToolRegistry.GetToolsByCategory("图像处理");

            // Assert
            imageTools.Should().HaveCount(2);
            imageTools.All(t => t.Category == "图像处理").Should().BeTrue();
        }

        #endregion
    }

    #region 测试辅助类

    /// <summary>
    /// 测试工具插件
    /// </summary>
    [Tool("test_tool", "测试工具")]
    public class TestToolPlugin : IToolPlugin
    {
        public Type ParamsType => typeof(GenericToolParameters);
        
        public Type ResultType => typeof(TestToolResults);
        
        public bool HasDebugWindow => false;

        public ToolParameters CreateParameters()
        {
            return new GenericToolParameters();
        }

        public IReadOnlyList<RuntimeParameterMetadata> GetParameterMetadata()
        {
            return new List<RuntimeParameterMetadata>();
        }

        public ValidationResult ValidateParameters(ToolParameters parameters)
        {
            return new ValidationResult();
        }

        public ToolResults Run(OpenCvSharp.Mat image, ToolParameters parameters)
        {
            return new TestToolResults();
        }

        public System.Windows.Window? CreateDebugWindow()
        {
            return null;
        }
    }

    /// <summary>
    /// 测试工具结果
    /// </summary>
    public class TestToolResults : ToolResults
    {
        public override List<ResultItem> GetResultItems()
        {
            return new List<ResultItem>();
        }

        public override Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>();
        }
    }

    #endregion
}
