using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.UI.Services.ParameterBinding;

namespace SunEyeVision.UI.Tests.Services
{
    /// <summary>
    /// 图像数据源服务单元测试
    /// </summary>
    [TestClass]
    public class ImageDataSourceServiceTests
    {
        #region 测试数据

        private class MockDataSourceQueryService : IDataSourceQueryService
        {
            private readonly List<ParentNodeInfo> _parentNodes = new();
            private readonly Dictionary<string, ToolResults> _results = new();

            public void AddParentNode(ParentNodeInfo node)
            {
                _parentNodes.Add(node);
            }

            public void SetNodeResult(string nodeId, ToolResults result)
            {
                _results[nodeId] = result;
            }

            public List<ParentNodeInfo> GetParentNodes(string nodeId)
            {
                return _parentNodes;
            }

            public List<AvailableDataSource> GetAvailableDataSources(string nodeId, Type? targetType = null)
            {
                var sources = new List<AvailableDataSource>();
                foreach (var parent in _parentNodes)
                {
                    sources.AddRange(parent.OutputProperties);
                }
                return sources;
            }

            public List<AvailableDataSource> GetNodeOutputProperties(string parentNodeId)
            {
                var parent = _parentNodes.FirstOrDefault(p => p.NodeId == parentNodeId);
                return parent?.OutputProperties ?? new List<AvailableDataSource>();
            }

            public ToolResults? GetNodeResult(string nodeId)
            {
                return _results.TryGetValue(nodeId, out var result) ? result : null;
            }

            public object? GetPropertyValue(string nodeId, string propertyName) => null;

            public bool HasNodeExecuted(string nodeId) => _results.ContainsKey(nodeId);

            public void RefreshNodeData(string nodeId) { }

            public void RefreshAll() { }

            public void SetNodeResult(string nodeId, ToolResults result) { }

            public void ClearNodeResult(string nodeId) { }

            public void ClearAllResults() { }
        }

        #endregion

        #region IsImageType 测试

        [TestMethod]
        public void IsImageType_WithMatType_ReturnsTrue()
        {
            // 使用动态创建的模拟类型
            var mockMatType = CreateMockType("Mat", "OpenCvSharp.Mat");
            
            var result = ImageDataSourceService.IsImageType(mockMatType);
            
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsImageType_WithBitmapType_ReturnsTrue()
        {
            var bitmapType = typeof(System.Drawing.Bitmap);
            
            var result = ImageDataSourceService.IsImageType(bitmapType);
            
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsImageType_WithIntType_ReturnsFalse()
        {
            var intType = typeof(int);
            
            var result = ImageDataSourceService.IsImageType(intType);
            
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsImageType_WithStringType_ReturnsFalse()
        {
            var stringType = typeof(string);
            
            var result = ImageDataSourceService.IsImageType(stringType);
            
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsImageType_WithNullType_ReturnsFalse()
        {
            Type? nullType = null;
            
            var result = ImageDataSourceService.IsImageType(nullType!);
            
            Assert.IsFalse(result);
        }

        #endregion

        #region GetImageDataSources 测试

        [TestMethod]
        public void GetImageDataSources_ReturnsOnlyImageTypes()
        {
            // Arrange
            var mockService = new MockDataSourceQueryService();
            var parentNode = new ParentNodeInfo
            {
                NodeId = "node1",
                NodeName = "ImageLoad",
                NodeType = "ImageLoadTool"
            };

            // 添加图像类型数据源
            parentNode.OutputProperties.Add(new AvailableDataSource
            {
                SourceNodeId = "node1",
                SourceNodeName = "ImageLoad",
                PropertyName = "OutputImage",
                DisplayName = "输出图像",
                PropertyType = CreateMockType("Mat", "OpenCvSharp.Mat")
            });

            // 添加非图像类型数据源
            parentNode.OutputProperties.Add(new AvailableDataSource
            {
                SourceNodeId = "node1",
                SourceNodeName = "ImageLoad",
                PropertyName = "Width",
                DisplayName = "图像宽度",
                PropertyType = typeof(int)
            });

            mockService.AddParentNode(parentNode);

            var service = new ImageDataSourceService(mockService);

            // Act
            var result = service.GetImageDataSources("currentNode");

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("OutputImage", result[0].PropertyName);
        }

        [TestMethod]
        public void GetImageDataSources_WithNoImageTypes_ReturnsEmptyList()
        {
            // Arrange
            var mockService = new MockDataSourceQueryService();
            var parentNode = new ParentNodeInfo
            {
                NodeId = "node1",
                NodeName = "MathTool",
                NodeType = "MathTool"
            };

            parentNode.OutputProperties.Add(new AvailableDataSource
            {
                SourceNodeId = "node1",
                PropertyName = "Result",
                PropertyType = typeof(double)
            });

            mockService.AddParentNode(parentNode);

            var service = new ImageDataSourceService(mockService);

            // Act
            var result = service.GetImageDataSources("currentNode");

            // Assert
            Assert.AreEqual(0, result.Count);
        }

        #endregion

        #region GetImageDataSourcesGrouped 测试

        [TestMethod]
        public void GetImageDataSourcesGrouped_GroupsByParentNode()
        {
            // Arrange
            var mockService = new MockDataSourceQueryService();

            var parent1 = new ParentNodeInfo
            {
                NodeId = "node1",
                NodeName = "ImageLoad1",
                NodeType = "ImageLoadTool"
            };
            parent1.OutputProperties.Add(new AvailableDataSource
            {
                SourceNodeId = "node1",
                PropertyName = "Image",
                PropertyType = CreateMockType("Mat", "OpenCvSharp.Mat")
            });

            var parent2 = new ParentNodeInfo
            {
                NodeId = "node2",
                NodeName = "ImageLoad2",
                NodeType = "ImageLoadTool"
            };
            parent2.OutputProperties.Add(new AvailableDataSource
            {
                SourceNodeId = "node2",
                PropertyName = "Image",
                PropertyType = CreateMockType("Mat", "OpenCvSharp.Mat")
            });

            mockService.AddParentNode(parent1);
            mockService.AddParentNode(parent2);

            var service = new ImageDataSourceService(mockService);

            // Act
            var result = service.GetImageDataSourcesGrouped("currentNode");

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("node1", result[0].NodeId);
            Assert.AreEqual("node2", result[1].NodeId);
        }

        #endregion

        #region 辅助方法

        private static Type CreateMockType(string name, string fullName)
        {
            // 使用真实类型进行模拟测试
            // 在实际项目中，OpenCvSharp.Mat应该被正确引用
            try
            {
                var matType = Type.GetType(fullName);
                if (matType != null)
                    return matType;
            }
            catch { }

            // 如果无法加载OpenCvSharp类型，使用Bitmap作为替代
            return typeof(System.Drawing.Bitmap);
        }

        #endregion
    }
}
