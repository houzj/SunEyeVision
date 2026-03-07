using System;
using System.Linq;
using Xunit;
using FluentAssertions;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.ViewModels;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Models;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Logic;

namespace SunEyeVision.Core.Tests.Region
{
    /// <summary>
    /// Region 编辑器集成测试
    /// </summary>
    public class RegionEditorIntegrationTests
    {
        #region EditHistory 测试

        [Fact]
        public void EditHistory_CreateRegion_ShouldSupportUndoRedo()
        {
            // Arrange
            using var viewModel = new RegionEditorViewModel();

            // Act - 添加区域
            viewModel.AddRegion(ShapeType.Rectangle);
            viewModel.Regions.Count.Should().Be(1);
            viewModel.CanUndo.Should().BeTrue();
            viewModel.CanRedo.Should().BeFalse();

            // Act - 撤销
            viewModel.Undo();
            viewModel.Regions.Count.Should().Be(0);
            viewModel.CanUndo.Should().BeFalse();
            viewModel.CanRedo.Should().BeTrue();

            // Act - 重做
            viewModel.Redo();
            viewModel.Regions.Count.Should().Be(1);
            viewModel.CanUndo.Should().BeTrue();
            viewModel.CanRedo.Should().BeFalse();
        }

        [Fact]
        public void EditHistory_DeleteRegion_ShouldSupportUndoRedo()
        {
            // Arrange
            using var viewModel = new RegionEditorViewModel();
            viewModel.AddRegion(ShapeType.Circle);
            var region = viewModel.Regions.First();

            // Act - 删除
            viewModel.SelectedRegion = region;
            viewModel.RemoveSelectedRegion();
            viewModel.Regions.Count.Should().Be(0);
            viewModel.CanUndo.Should().BeTrue();

            // Act - 撤销
            viewModel.Undo();
            viewModel.Regions.Count.Should().Be(1);
            viewModel.Regions.First().GetShapeType().Should().Be(ShapeType.Circle);
        }

        [Fact]
        public void EditHistory_ClearAll_ShouldSupportUndoRedo()
        {
            // Arrange
            using var viewModel = new RegionEditorViewModel();
            viewModel.AddRegion(ShapeType.Rectangle);
            viewModel.AddRegion(ShapeType.Circle);
            viewModel.AddRegion(ShapeType.Line);
            viewModel.Regions.Count.Should().Be(3);

            // Act - 清除所有
            viewModel.ClearAllRegions();
            viewModel.Regions.Count.Should().Be(0);

            // Act - 撤销
            viewModel.Undo();
            viewModel.Regions.Count.Should().Be(3);

            // Act - 重做
            viewModel.Redo();
            viewModel.Regions.Count.Should().Be(0);
        }

        [Fact]
        public void EditHistory_MultipleActions_ShouldSupportSequentialUndoRedo()
        {
            // Arrange
            using var viewModel = new RegionEditorViewModel();

            // Act - 多个操作
            viewModel.AddRegion(ShapeType.Rectangle);
            viewModel.AddRegion(ShapeType.Circle);
            viewModel.AddRegion(ShapeType.Line);
            viewModel.Regions.Count.Should().Be(3);

            // Act - 撤销3次
            viewModel.Undo();
            viewModel.Regions.Count.Should().Be(2);

            viewModel.Undo();
            viewModel.Regions.Count.Should().Be(1);

            viewModel.Undo();
            viewModel.Regions.Count.Should().Be(0);

            // Act - 重做3次
            viewModel.Redo();
            viewModel.Regions.Count.Should().Be(1);

            viewModel.Redo();
            viewModel.Regions.Count.Should().Be(2);

            viewModel.Redo();
            viewModel.Regions.Count.Should().Be(3);
        }

        [Fact]
        public void EditHistory_NewActionAfterUndo_ShouldClearRedoStack()
        {
            // Arrange
            using var viewModel = new RegionEditorViewModel();
            viewModel.AddRegion(ShapeType.Rectangle);
            viewModel.AddRegion(ShapeType.Circle);
            viewModel.Undo(); // 撤销到只有1个区域

            viewModel.CanRedo.Should().BeTrue();

            // Act - 执行新操作
            viewModel.AddRegion(ShapeType.Line);

            // Assert - 重做栈应被清空
            viewModel.CanRedo.Should().BeFalse();
            viewModel.Regions.Count.Should().Be(2);
        }

        #endregion

        #region HandleManager 测试

        [Fact]
        public void HandleManager_CreateRectangleHandles_ShouldReturn8Handles()
        {
            // Arrange
            var handleManager = new HandleManager();
            var shape = new ShapeDefinition
            {
                ShapeType = ShapeType.Rectangle,
                CenterX = 100,
                CenterY = 100,
                Width = 50,
                Height = 80
            };

            // Act
            handleManager.CreateHandles(shape);
            var handles = handleManager.Handles;

            // Assert
            handles.Count.Should().Be(8);
            handles.Any(h => h.Type == HandleType.TopLeft).Should().BeTrue();
            handles.Any(h => h.Type == HandleType.TopRight).Should().BeTrue();
            handles.Any(h => h.Type == HandleType.BottomLeft).Should().BeTrue();
            handles.Any(h => h.Type == HandleType.BottomRight).Should().BeTrue();
            handles.Any(h => h.Type == HandleType.Top).Should().BeTrue();
            handles.Any(h => h.Type == HandleType.Bottom).Should().BeTrue();
            handles.Any(h => h.Type == HandleType.Left).Should().BeTrue();
            handles.Any(h => h.Type == HandleType.Right).Should().BeTrue();
        }

        [Fact]
        public void HandleManager_CreateCircleHandles_ShouldReturn4Handles()
        {
            // Arrange
            var handleManager = new HandleManager();
            var shape = new ShapeDefinition
            {
                ShapeType = ShapeType.Circle,
                CenterX = 100,
                CenterY = 100,
                Radius = 50
            };

            // Act
            handleManager.CreateHandles(shape);
            var handles = handleManager.Handles;

            // Assert
            handles.Count.Should().Be(4);
            handles.Any(h => h.Type == HandleType.Top).Should().BeTrue();
            handles.Any(h => h.Type == HandleType.Bottom).Should().BeTrue();
            handles.Any(h => h.Type == HandleType.Left).Should().BeTrue();
            handles.Any(h => h.Type == HandleType.Right).Should().BeTrue();
        }

        [Fact]
        public void HandleManager_CreateLineHandles_ShouldReturn2Handles()
        {
            // Arrange
            var handleManager = new HandleManager();
            var shape = new ShapeDefinition
            {
                ShapeType = ShapeType.Line,
                StartX = 10,
                StartY = 20,
                EndX = 100,
                EndY = 200
            };

            // Act
            handleManager.CreateHandles(shape);
            var handles = handleManager.Handles;

            // Assert
            handles.Count.Should().Be(2);
            handles.Any(h => h.Type == HandleType.LineStart).Should().BeTrue();
            handles.Any(h => h.Type == HandleType.LineEnd).Should().BeTrue();
        }

        [Fact]
        public void HandleManager_CreateRotatedRectangleHandles_ShouldReturn10Handles()
        {
            // Arrange
            var handleManager = new HandleManager();
            var shape = new ShapeDefinition
            {
                ShapeType = ShapeType.RotatedRectangle,
                CenterX = 100,
                CenterY = 100,
                Width = 80,
                Height = 50,
                Angle = 30
            };

            // Act
            handleManager.CreateHandles(shape);
            var handles = handleManager.Handles;

            // Assert - 8个缩放手柄 + 1个中心手柄 + 1个旋转手柄 = 10
            handles.Count.Should().Be(10);
            handles.Any(h => h.Type == HandleType.Rotate).Should().BeTrue();
            handles.Any(h => h.Type == HandleType.Center).Should().BeTrue();
        }

        [Fact]
        public void HandleManager_HitTest_ShouldDetectHandle()
        {
            // Arrange
            var handleManager = new HandleManager
            {
                HandleSize = 12,
                HitTolerance = 8
            };
            var shape = new ShapeDefinition
            {
                ShapeType = ShapeType.Rectangle,
                CenterX = 100,
                CenterY = 100,
                Width = 50,
                Height = 80
            };
            handleManager.CreateHandles(shape);

            // Act - 点击左上角手柄位置
            var topLeftHandle = handleManager.Handles.First(h => h.Type == HandleType.TopLeft);
            var hitType = handleManager.HitTest(new Point2D(topLeftHandle.Position.X, topLeftHandle.Position.Y));

            // Assert
            hitType.Should().Be(HandleType.TopLeft);
        }

        #endregion

        #region RegionEditorSettings 测试

        [Fact]
        public void RegionEditorSettings_Default_ShouldHaveValidValues()
        {
            // Arrange & Act
            var settings = RegionEditorSettings.Default;

            // Assert
            settings.LabelFontSize.Should().Be(12);
            settings.DefaultStrokeThickness.Should().Be(2);
            settings.SelectedStrokeThickness.Should().Be(3);
            settings.HandleSize.Should().Be(12);
            settings.HitTolerance.Should().Be(8);
        }

        [Fact]
        public void RegionEditorSettings_HighContrast_ShouldHaveLargerValues()
        {
            // Arrange & Act
            var settings = RegionEditorSettings.HighContrast();

            // Assert
            settings.LabelFontSize.Should().Be(14);
            settings.DefaultStrokeThickness.Should().Be(3);
            settings.SelectedStrokeThickness.Should().Be(4);
            settings.HandleSize.Should().Be(14);
        }

        [Fact]
        public void RegionEditorSettings_Compact_ShouldHaveSmallerValues()
        {
            // Arrange & Act
            var settings = RegionEditorSettings.Compact();

            // Assert
            settings.LabelFontSize.Should().Be(10);
            settings.DefaultStrokeThickness.Should().Be(1);
            settings.SelectedStrokeThickness.Should().Be(2);
            settings.HandleSize.Should().Be(8);
        }

        #endregion

        #region RegionData 测试

        [Fact]
        public void RegionData_CreateDrawingRegion_ShouldHaveValidShapeDefinition()
        {
            // Arrange & Act
            var region = RegionData.CreateDrawingRegion("测试区域", ShapeType.Rectangle);

            // Assert
            region.Name.Should().Be("测试区域");
            region.IsEditable.Should().BeTrue();
            region.IsVisible.Should().BeTrue();
            region.Definition.Should().BeOfType<ShapeDefinition>();
            region.Definition.Should().NotBeNull();
            var shapeDef = (ShapeDefinition)region.Definition!;
            shapeDef.ShapeType.Should().Be(ShapeType.Rectangle);
        }

        [Fact]
        public void RegionData_Clone_ShouldCreateIndependentCopy()
        {
            // Arrange
            var original = RegionData.CreateDrawingRegion("原始区域", ShapeType.Circle);
            original.Definition.Should().NotBeNull();
            var originalShapeDef = (ShapeDefinition)original.Definition!;
            originalShapeDef.Radius = 50;

            // Act
            var clone = (RegionData)original.Clone();
            clone.Definition.Should().NotBeNull();
            var cloneShapeDef = (ShapeDefinition)clone.Definition!;
            cloneShapeDef.Radius = 100;

            // Assert
            originalShapeDef.Radius.Should().Be(50);
            cloneShapeDef.Radius.Should().Be(100);
            original.Should().NotBeSameAs(clone);
        }

        #endregion

        #region ViewModel 命令测试

        [Fact]
        public void ViewModel_UndoCommand_CanExecuteShouldReflectHistoryState()
        {
            // Arrange
            using var viewModel = new RegionEditorViewModel();

            // Act & Assert - 初始状态
            viewModel.CanUndo.Should().BeFalse();

            // Act - 添加区域
            viewModel.AddRegion(ShapeType.Rectangle);

            // Assert - 应该可以撤销
            viewModel.CanUndo.Should().BeTrue();
        }

        [Fact]
        public void ViewModel_RedoCommand_CanExecuteShouldReflectHistoryState()
        {
            // Arrange
            using var viewModel = new RegionEditorViewModel();
            viewModel.AddRegion(ShapeType.Rectangle);

            // Act - 撤销
            viewModel.Undo();

            // Assert - 应该可以重做
            viewModel.CanRedo.Should().BeTrue();
        }

        #endregion
    }
}
