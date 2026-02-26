using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using SunEyeVision.UI.Adapters;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Services.Rendering
{
    /// <summary>
    /// 节点转换器缓存 - 避免重复创建SolidColorBrush
    /// 预定义常用颜色并Freeze，提升渲染性能
    /// </summary>
    public static class CachedBrushes
    {
        // 预定义常用节点背景色（已Freeze，线程安全）
        private static readonly SolidColorBrush _whiteBrush;
        private static readonly SolidColorBrush _lightBlueBrush;
        private static readonly SolidColorBrush _lightGreenBrush;
        private static readonly SolidColorBrush _lightYellowBrush;
        private static readonly SolidColorBrush _lightOrangeBrush;
        private static readonly SolidColorBrush _lightPurpleBrush;

        // 预定义常用节点边框色
        private static readonly SolidColorBrush _orangeBorderBrush;
        private static readonly SolidColorBrush _blueBorderBrush;
        private static readonly SolidColorBrush _grayBorderBrush;

        // 动态缓存（用于非常用颜色）
        private static readonly ConcurrentDictionary<Color, SolidColorBrush> _brushCache = new();

        static CachedBrushes()
        {
            // 初始化并Freeze所有预定义画刷
            _whiteBrush = CreateFrozenBrush(Colors.White);
            _lightBlueBrush = CreateFrozenBrush(Color.FromRgb(230, 242, 255)); // #E6F2FF
            _lightGreenBrush = CreateFrozenBrush(Color.FromRgb(230, 255, 230)); // #E6FFE6
            _lightYellowBrush = CreateFrozenBrush(Color.FromRgb(255, 250, 230)); // #FFFAE6
            _lightOrangeBrush = CreateFrozenBrush(Color.FromRgb(255, 245, 230)); // #FFF5E6
            _lightPurpleBrush = CreateFrozenBrush(Color.FromRgb(245, 240, 255)); // #F5F0FF

            _orangeBorderBrush = CreateFrozenBrush(Color.FromRgb(255, 149, 0)); // #ff9500
            _blueBorderBrush = CreateFrozenBrush(Color.FromRgb(0, 102, 204)); // #0066CC
            _grayBorderBrush = CreateFrozenBrush(Color.FromRgb(200, 200, 200)); // #C8C8C8
        }

        private static SolidColorBrush CreateFrozenBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        /// <summary>
        /// 获取或创建缓存的SolidColorBrush
        /// </summary>
        public static SolidColorBrush GetOrCreateBrush(Color color)
        {
            return _brushCache.GetOrAdd(color, c =>
            {
                var brush = new SolidColorBrush(c);
                brush.Freeze();
                return brush;
            });
        }

        // 预定义画刷访问器
        public static SolidColorBrush White => _whiteBrush;
        public static SolidColorBrush LightBlue => _lightBlueBrush;
        public static SolidColorBrush LightGreen => _lightGreenBrush;
        public static SolidColorBrush LightYellow => _lightYellowBrush;
        public static SolidColorBrush LightOrange => _lightOrangeBrush;
        public static SolidColorBrush LightPurple => _lightPurpleBrush;
        public static SolidColorBrush OrangeBorder => _orangeBorderBrush;
        public static SolidColorBrush BlueBorder => _blueBorderBrush;
        public static SolidColorBrush GrayBorder => _grayBorderBrush;
    }

    /// <summary>
    /// 缓存版节点显示文本转换器 - 避免重复创建字符串
    /// </summary>
    public class CachedNodeDisplayTextConverter : IValueConverter
    {
        // 显示文本缓存（节点ID -> 显示文本）
        private static readonly ConcurrentDictionary<string, string> _textCache = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WorkflowNode node)
            {
                // 使用节点ID作为缓存键
                var cacheKey = $"{node.Id}_{node.Name}_{node.Index}";
                return _textCache.GetOrAdd(cacheKey, _ =>
                {
                    var adapter = NodeDisplayAdapterFactory.GetAdapter(node.AlgorithmType);
                    return adapter.GetDisplayText(node);
                });
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 清除缓存（当节点名称或索引变化时调用）
        /// </summary>
        public static void InvalidateCache(string nodeId)
        {
            // 移除以该节点ID开头的所有缓存项
            foreach (var key in _textCache.Keys)
            {
                if (key.StartsWith(nodeId + "_"))
                {
                    _textCache.TryRemove(key, out _);
                }
            }
        }
    }

    /// <summary>
    /// 缓存版节点背景颜色转换器 - 使用预定义画刷避免重复创建
    /// </summary>
    public class CachedNodeBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WorkflowNode node)
            {
                var adapter = NodeDisplayAdapterFactory.GetAdapter(node.AlgorithmType);
                var color = adapter.GetBackgroundColor(node);

                // 检查是否为常用颜色，使用预定义画刷
                if (color == Colors.White)
                    return CachedBrushes.White;

                // 其他颜色使用动态缓存
                return CachedBrushes.GetOrCreateBrush(color);
            }
            return CachedBrushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 缓存版节点边框颜色转换器 - 使用预定义画刷避免重复创建
    /// </summary>
    public class CachedNodeBorderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WorkflowNode node)
            {
                // 选中状态使用橙色边框
                if (node.IsSelected)
                    return CachedBrushes.OrangeBorder;

                var adapter = NodeDisplayAdapterFactory.GetAdapter(node.AlgorithmType);
                var color = adapter.GetBorderColor(node);

                // 检查是否为常用颜色
                if (color == Color.FromRgb(255, 149, 0)) // #ff9500
                    return CachedBrushes.OrangeBorder;
                if (color == Color.FromRgb(0, 102, 204)) // #0066CC
                    return CachedBrushes.BlueBorder;

                // 其他颜色使用动态缓存
                return CachedBrushes.GetOrCreateBrush(color);
            }
            return CachedBrushes.OrangeBorder;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
