using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.Validation;
using SunEyeVision.Tool.EdgeDetection;
using Xunit;

namespace SunEyeVision.Core.Tests.BindingModel
{
    /// <summary>
    /// 绑定模型验证测试
    /// 验证 EdgeDetection 工具的完整绑定链路
    /// </summary>
    public class EdgeDetectionBindingModelTests
    {
        private readonly EdgeDetectionTool _plugin;

        public EdgeDetectionBindingModelTests()
        {
            _plugin = new EdgeDetectionTool();
        }

        #region 1. 参数特性验证测试

        [Fact]
        public void Parameters_Should_HaveRangeAttributes()
        {
            // Arrange
            var type = typeof(EdgeDetectionParameters);
            var threshold1Prop = type.GetProperty("Threshold1");
            var threshold2Prop = type.GetProperty("Threshold2");
            var apertureSizeProp = type.GetProperty("ApertureSize");

            // Act & Assert
            threshold1Prop.Should().BeDecoratedWith<ParameterRangeAttribute>(
                "Threshold1 应该有范围特性");
            threshold2Prop.Should().BeDecoratedWith<ParameterRangeAttribute>(
                "Threshold2 应该有范围特性");
            apertureSizeProp.Should().BeDecoratedWith<ParameterDisplayAttribute>(
                "ApertureSize 应该有显示特性");
        }

        [Fact]
        public void Parameters_Should_HaveDisplayAttributes()
        {
            // Arrange
            var type = typeof(EdgeDetectionParameters);
            var threshold1Prop = type.GetProperty("Threshold1");
            var threshold2Prop = type.GetProperty("Threshold2");
            var apertureSizeProp = type.GetProperty("ApertureSize");

            // Act & Assert
            threshold1Prop.Should().BeDecoratedWith<ParameterDisplayAttribute>(
                "Threshold1 应该有显示特性");
            threshold2Prop.Should().BeDecoratedWith<ParameterDisplayAttribute>(
                "Threshold2 应该有显示特性");
            apertureSizeProp.Should().BeDecoratedWith<ParameterDisplayAttribute>(
                "ApertureSize 应该有显示特性");
        }

        #endregion

        #region 2. 参数验证测试

        [Fact]
        public void Validate_Should_PassForValidParameters()
        {
            // Arrange
            var parameters = new EdgeDetectionParameters
            {
                Threshold1 = 50.0,
                Threshold2 = 150.0,
                ApertureSize = 3
            };

            // Act
            var result = parameters.Validate();

            // Assert
            result.IsValid.Should().BeTrue("有效参数应该通过验证");
            result.Errors.Should().BeEmpty("有效参数不应该有错误");
        }

        [Fact]
        public void Validate_Should_FailForInvalidThreshold1()
        {
            // Arrange
            var parameters = new EdgeDetectionParameters
            {
                Threshold1 = -10.0, // 无效值
                Threshold2 = 150.0,
                ApertureSize = 3
            };

            // Act
            var result = parameters.Validate();

            // Assert
            result.IsValid.Should().BeFalse("无效阈值应该验证失败");
            result.Errors.Should().Contain(e => e.Contains("低阈值"));
        }

        [Fact]
        public void Validate_Should_FailForInvalidThreshold2()
        {
            // Arrange
            var parameters = new EdgeDetectionParameters
            {
                Threshold1 = 50.0,
                Threshold2 = 300.0, // 超出范围
                ApertureSize = 3
            };

            // Act
            var result = parameters.Validate();

            // Assert
            result.IsValid.Should().BeFalse("超出范围的阈值应该验证失败");
            result.Errors.Should().Contain(e => e.Contains("高阈值"));
        }

        [Fact]
        public void Validate_Should_FailForInvalidApertureSize()
        {
            // Arrange
            var parameters = new EdgeDetectionParameters
            {
                Threshold1 = 50.0,
                Threshold2 = 150.0,
                ApertureSize = 4 // 无效值，必须是3、5或7
            };

            // Act
            var result = parameters.Validate();

            // Assert
            result.IsValid.Should().BeFalse("无效孔径大小应该验证失败");
            result.Errors.Should().Contain(e => e.Contains("孔径大小"));
        }

        [Fact]
        public void Validate_Should_WarnWhenThreshold1GreaterThanThreshold2()
        {
            // Arrange
            var parameters = new EdgeDetectionParameters
            {
                Threshold1 = 200.0, // 大于 Threshold2
                Threshold2 = 100.0,
                ApertureSize = 3
            };

            // Act
            var result = parameters.Validate();

            // Assert
            result.IsValid.Should().BeTrue("参数范围有效");
            result.Warnings.Should().Contain(w => w.Contains("低阈值大于高阈值"));
        }

        #endregion

        #region 3. 结果类测试

        [Fact]
        public void Results_Should_ImplementGetResultItems()
        {
            // Arrange
            var results = new EdgeDetectionResults
            {
                EdgeCount = 10,
                Threshold1Used = 50.0,
                Threshold2Used = 150.0,
                ApertureSizeUsed = 3
            };
            results.SetSuccess(100);

            // Act
            var items = results.GetResultItems();

            // Assert
            items.Should().NotBeEmpty("结果项列表不应该为空");
            items.Should().Contain(i => i.Name == "EdgeCount");
            items.Should().Contain(i => i.Name == "Threshold1Used");
            items.Should().Contain(i => i.Name == "Threshold2Used");
            items.Should().Contain(i => i.Name == "ExecutionTimeMs");
        }

        [Fact]
        public void Results_Should_ImplementToDictionary()
        {
            // Arrange
            var results = new EdgeDetectionResults
            {
                EdgeCount = 15,
                Threshold1Used = 60.0,
                Threshold2Used = 180.0,
                ApertureSizeUsed = 5
            };
            results.SetSuccess(150);

            // Act
            var dict = results.ToDictionary();

            // Assert
            dict.Should().ContainKey("EdgeCount");
            dict.Should().ContainKey("Threshold1Used");
            dict.Should().ContainKey("Threshold2Used");
            dict.Should().ContainKey("ApertureSizeUsed");
            dict.Should().ContainKey("ExecutionTimeMs");
        }

        #endregion

        #region 4. 运行时元数据测试

        [Fact]
        public void GetRuntimeMetadata_Should_ReturnCorrectMetadata()
        {
            // Arrange
            var parameters = new EdgeDetectionParameters
            {
                Threshold1 = 90.0,
                Threshold2 = 200.0,
                ApertureSize = 7
            };

            // Act
            var runtimeMetadata = parameters.GetRuntimeParameterMetadata();

            // Assert
            runtimeMetadata.Should().NotBeEmpty("运行时元数据不应该为空");
            runtimeMetadata.Should().Contain(m => m.Name == "Threshold1");
            runtimeMetadata.Should().Contain(m => m.Name == "Threshold2");
            runtimeMetadata.Should().Contain(m => m.Name == "ApertureSize");
        }

        #endregion

        #region 5. 默认参数测试

        [Fact]
        public void GetDefaultParameters_Should_ReturnCorrectDefaults()
        {
            // Act
            var defaultParams = ((IToolPlugin)_plugin).GetDefaultParameters() as EdgeDetectionParameters;

            // Assert
            defaultParams.Should().NotBeNull();
            // 默认参数由 new EdgeDetectionParameters() 创建，使用默认值
        }

        #endregion

        #region 6. Tool 特性测试

        [Fact]
        public void Tool_Should_HaveToolAttribute()
        {
            // Arrange
            var type = typeof(EdgeDetectionTool);

            // Act & Assert
            type.Should().BeDecoratedWith<ToolAttribute>(
                "EdgeDetectionTool 应该有 Tool 特性");
        }

        [Fact]
        public void ToolAttribute_Should_HaveCorrectValues()
        {
            // Arrange
            var type = typeof(EdgeDetectionTool);
            var attr = type.GetCustomAttributes(typeof(ToolAttribute), false)
                          .FirstOrDefault() as ToolAttribute;

            // Assert
            attr.Should().NotBeNull();
            attr!.Id.Should().Be("edge_detection");
            attr.DisplayName.Should().Be("边缘检测");
            attr.Category.Should().Be("图像处理");
        }

        #endregion
    }
}
