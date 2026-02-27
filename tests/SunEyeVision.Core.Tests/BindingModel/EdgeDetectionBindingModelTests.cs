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
        private readonly EdgeDetectionToolPlugin _plugin;

        public EdgeDetectionBindingModelTests()
        {
            _plugin = new EdgeDetectionToolPlugin();
            _plugin.Initialize();
        }

        #region 1. 元数据验证测试

        [Fact]
        public void Plugin_Should_SupportDataBinding()
        {
            // Arrange & Act
            var metadata = _plugin.GetToolMetadata().First();

            // Assert
            metadata.SupportsDataBinding.Should().BeTrue("插件应该支持数据绑定");
            metadata.ParameterType.Should().Be(typeof(EdgeDetectionParameters), "参数类型应该正确");
            metadata.ResultType.Should().Be(typeof(EdgeDetectionResults), "结果类型应该正确");
        }

        [Fact]
        public void InputParameters_Should_HaveCorrectMetadata()
        {
            // Arrange & Act
            var metadata = _plugin.GetToolMetadata().First();
            var inputParams = metadata.InputParameters;

            // Assert
            inputParams.Should().HaveCount(3, "应该有3个输入参数");

            var threshold1 = inputParams.First(p => p.Name == "Threshold1");
            threshold1.DisplayName.Should().Be("低阈值");
            threshold1.MinValue.Should().Be(0.0);
            threshold1.MaxValue.Should().Be(255.0);
            threshold1.SupportsBinding.Should().BeTrue("应该支持绑定");

            var threshold2 = inputParams.First(p => p.Name == "Threshold2");
            threshold2.DisplayName.Should().Be("高阈值");
            threshold2.MinValue.Should().Be(0.0);
            threshold2.MaxValue.Should().Be(255.0);
            threshold2.SupportsBinding.Should().BeTrue("应该支持绑定");

            var apertureSize = inputParams.First(p => p.Name == "ApertureSize");
            apertureSize.DisplayName.Should().Be("孔径大小");
            apertureSize.MinValue.Should().Be(3);
            apertureSize.MaxValue.Should().Be(7);
            apertureSize.SupportsBinding.Should().BeTrue("应该支持绑定");
        }

        [Fact]
        public void OutputParameters_Should_HaveCorrectMetadata()
        {
            // Arrange & Act
            var metadata = _plugin.GetToolMetadata().First();
            var outputParams = metadata.OutputParameters;

            // Assert
            outputParams.Should().HaveCount(5, "应该有5个输出参数");

            outputParams.Should().Contain(p => p.Name == "OutputImage" && p.Type == ParamDataType.Image);
            outputParams.Should().Contain(p => p.Name == "EdgeCount" && p.Type == ParamDataType.Int);
            outputParams.Should().Contain(p => p.Name == "Threshold1Used" && p.Type == ParamDataType.Double);
            outputParams.Should().Contain(p => p.Name == "Threshold2Used" && p.Type == ParamDataType.Double);
            outputParams.Should().Contain(p => p.Name == "ExecutionTimeMs" && p.Type == ParamDataType.Int);
        }

        #endregion

        #region 2. 参数特性验证测试

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
            apertureSizeProp.Should().BeDecoratedWith<ParameterRangeAttribute>(
                "ApertureSize 应该有范围特性");
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

        #region 3. 参数验证测试

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

        #region 4. 参数转换测试

        [Fact]
        public void ConvertToTypedParameters_Should_ConvertCorrectly()
        {
            // Arrange
            var algorithmParams = new AlgorithmParameters();
            algorithmParams.Set("Threshold1", 60.0);
            algorithmParams.Set("Threshold2", 180.0);
            algorithmParams.Set("ApertureSize", 5);

            // Act
            var typedParams = EdgeDetectionToolPlugin.ConvertToTypedParameters(algorithmParams);

            // Assert
            typedParams.Threshold1.Should().Be(60.0);
            typedParams.Threshold2.Should().Be(180.0);
            typedParams.ApertureSize.Should().Be(5);
        }

        [Fact]
        public void ConvertToAlgorithmParameters_Should_ConvertCorrectly()
        {
            // Arrange
            var typedParams = new EdgeDetectionParameters
            {
                Threshold1 = 70.0,
                Threshold2 = 200.0,
                ApertureSize = 7
            };

            // Act
            var algorithmParams = EdgeDetectionToolPlugin.ConvertToAlgorithmParameters(typedParams);

            // Assert
            algorithmParams.Get<double>("Threshold1").Should().Be(70.0);
            algorithmParams.Get<double>("Threshold2").Should().Be(200.0);
            algorithmParams.Get<int>("ApertureSize").Should().Be(7);
        }

        [Fact]
        public void ParameterConversion_Should_BeRoundTrip()
        {
            // Arrange
            var originalParams = new EdgeDetectionParameters
            {
                Threshold1 = 80.0,
                Threshold2 = 220.0,
                ApertureSize = 5
            };

            // Act
            var algorithmParams = EdgeDetectionToolPlugin.ConvertToAlgorithmParameters(originalParams);
            var convertedParams = EdgeDetectionToolPlugin.ConvertToTypedParameters(algorithmParams);

            // Assert
            convertedParams.Threshold1.Should().Be(originalParams.Threshold1);
            convertedParams.Threshold2.Should().Be(originalParams.Threshold2);
            convertedParams.ApertureSize.Should().Be(originalParams.ApertureSize);
        }

        #endregion

        #region 5. 结果类测试

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

        #region 6. 运行时元数据测试

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

        #region 7. 默认参数测试

        [Fact]
        public void GetDefaultParameters_Should_ReturnCorrectDefaults()
        {
            // Act
            var defaultParams = _plugin.GetDefaultTypedParameters();

            // Assert
            defaultParams.Threshold1.Should().Be(50.0);
            defaultParams.Threshold2.Should().Be(150.0);
            defaultParams.ApertureSize.Should().Be(3);
        }

        [Fact]
        public void GetDefaultParameters_ThroughAlgorithmParameters_Should_Work()
        {
            // Act
            var algorithmParams = _plugin.GetDefaultParameters("edge_detection");
            var typedParams = EdgeDetectionToolPlugin.ConvertToTypedParameters(algorithmParams);

            // Assert
            typedParams.Threshold1.Should().Be(50.0);
            typedParams.Threshold2.Should().Be(150.0);
            typedParams.ApertureSize.Should().Be(3);
        }

        #endregion
    }
}
