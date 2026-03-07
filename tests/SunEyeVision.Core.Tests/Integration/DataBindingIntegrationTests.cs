using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FluentAssertions;
using SunEyeVision.Plugin.SDK.Models;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.SDK.Validation;
using Xunit;

namespace SunEyeVision.Core.Tests.Integration
{
    /// <summary>
    /// 数据绑定集成测试
    /// 验证参数绑定、结果更新、属性通知等完整链路
    /// </summary>
    public class DataBindingIntegrationTests
    {
        #region ObservableObject 测试

        [Fact]
        public void ObservableObject_SetProperty_ShouldRaisePropertyChanged()
        {
            // Arrange
            var obj = new TestObservableObject();
            var eventRaised = false;
            obj.PropertyChanged += (s, e) => 
            {
                if (e.PropertyName == "TestProperty")
                    eventRaised = true;
            };

            // Act
            obj.TestProperty = "NewValue";

            // Assert
            eventRaised.Should().BeTrue("属性改变事件应被触发");
            obj.TestProperty.Should().Be("NewValue");
        }

        [Fact]
        public void ObservableObject_SetSameValue_ShouldNotRaiseEvent()
        {
            // Arrange
            var obj = new TestObservableObject();
            obj.TestProperty = "InitialValue";
            var eventCount = 0;
            obj.PropertyChanged += (s, e) => eventCount++;

            // Act
            obj.TestProperty = "InitialValue"; // 设置相同的值

            // Assert
            eventCount.Should().Be(0, "相同值不应触发事件");
        }

        #endregion

        #region ToolResults 测试

        [Fact]
        public void ToolResults_SetSuccess_ShouldUpdateProperties()
        {
            // Arrange
            var results = new TestToolResults();

            // Act
            results.Status = ExecutionStatus.Success;
            results.ExecutionTimeMs = 100;

            // Assert
            results.IsSuccess.Should().BeTrue();
            results.ExecutionTimeMs.Should().Be(100);
        }

        [Fact]
        public void ToolResults_SetFailure_ShouldRecordError()
        {
            // Arrange
            var results = new TestToolResults();

            // Act
            results.Status = ExecutionStatus.Failed;
            results.ErrorMessage = "测试错误";
            results.ErrorStackTrace = new Exception("详细信息").StackTrace;

            // Assert
            results.IsSuccess.Should().BeFalse();
            results.ErrorMessage.Should().Be("测试错误");
        }

        [Fact]
        public void ToolResults_GetResultItems_ShouldReturnMetadata()
        {
            // Arrange
            var results = new TestToolResults
            {
                Count = 42,
                AverageValue = 123.45
            };
            results.SetSuccess(50);

            // Act
            var items = results.GetResultItems();

            // Assert
            items.Should().NotBeEmpty();
            items.Should().Contain(i => i.Name == "Count" && (int)i.Value == 42);
            items.Should().Contain(i => i.Name == "AverageValue");
        }

        #endregion

        #region 参数验证测试

        [Fact]
        public void AlgorithmParameters_Validate_ShouldReturnValidResult()
        {
            // Arrange
            var parameters = new TestParameters
            {
                Threshold = 50.0,
                Iterations = 10
            };

            // Act
            var result = parameters.Validate();

            // Assert
            result.IsValid.Should().BeTrue("有效参数应通过验证");
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void AlgorithmParameters_ValidateWithInvalidData_ShouldReturnErrors()
        {
            // Arrange
            var parameters = new TestParameters
            {
                Threshold = -10.0, // 无效值
                Iterations = 0 // 无效值
            };

            // Act
            var result = parameters.Validate();

            // Assert
            result.IsValid.Should().BeFalse("无效参数应验证失败");
            result.Errors.Should().NotBeEmpty();
        }

        #endregion

        #region 测试辅助类

        private class TestObservableObject : ObservableObject
        {
            private string _testProperty = string.Empty;
            
            public string TestProperty
            {
                get => _testProperty;
                set => SetProperty(ref _testProperty, value);
            }
        }

        private class TestToolResults : ToolResults
        {
            public int Count { get; set; }
            public double AverageValue { get; set; }

            public override List<ResultItem> GetResultItems()
            {
                return new List<ResultItem>
                {
                    new ResultItem { Name = "Count", Value = Count, DisplayName = "计数" },
                    new ResultItem { Name = "AverageValue", Value = AverageValue, DisplayName = "平均值" }
                };
            }

            public override Dictionary<string, object> ToDictionary()
            {
                return new Dictionary<string, object>
                {
                    ["Count"] = Count,
                    ["AverageValue"] = AverageValue
                };
            }
        }

        private class TestParameters : ToolParameters
        {
            private double _threshold;
            private int _iterations;

            [ParameterRange(0.0, 255.0)]
            [ParameterDisplay(DisplayName = "阈值", Description = "处理阈值")]
            public double Threshold
            {
                get => _threshold;
                set => SetProperty(ref _threshold, value);
            }

            [ParameterRange(1, 100)]
            [ParameterDisplay(DisplayName = "迭代次数", Description = "算法迭代次数")]
            public int Iterations
            {
                get => _iterations;
                set => SetProperty(ref _iterations, value);
            }

            public override ValidationResult Validate()
            {
                var result = new ValidationResult();
                
                if (Threshold < 0 || Threshold > 255)
                    result.AddError($"阈值必须在0-255范围内，当前值: {Threshold}");
                
                if (Iterations < 1 || Iterations > 100)
                    result.AddError($"迭代次数必须在1-100范围内，当前值: {Iterations}");
                
                return result;
            }
        }

        #endregion
    }
}
