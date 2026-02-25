using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SunEyeVision.UI.Services.Rendering
{
    /// <summary>
    /// 工作流视觉辅助类
    /// 提供视觉树查找、路径计算等辅助功能
    /// </summary>
    public static class WorkflowVisualHelper
    {
        /// <summary>
        /// 在视觉树中查找指定类型的子元素
        /// </summary>
        public static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
                return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T result)
                {
                    return result;
                }

                var descendant = FindVisualChild<T>(child);
                if (descendant != null)
                {
                    return descendant;
                }
            }

            return null;
        }

        /// <summary>
        /// 在视觉树中查找所有指定类型的子元素
        /// </summary>
        public static IEnumerable<T> FindAllVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
                yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T result)
                {
                    yield return result;
                }

                foreach (var descendant in FindAllVisualChildren<T>(child))
                {
                    yield return descendant;
                }
            }
        }

        /// <summary>
        /// 计算两个点之间的距离
        /// </summary>
        public static double Distance(System.Windows.Point p1, System.Windows.Point p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }

        /// <summary>
        /// 查找距离指定点最近的端口
        /// </summary>
        public static Ellipse? FindNearestPort(
            IEnumerable<Ellipse> ports,
            System.Windows.Point targetPoint,
            string? portNameFilter = null)
        {
            return ports
                .Where(p => string.IsNullOrEmpty(portNameFilter) || p.Name?.Contains(portNameFilter) == true)
                .OrderBy(p => Distance(
                    p.TransformToVisual(VisualTreeHelper.GetParent(p) as Visual ?? p).Transform(new System.Windows.Point(p.ActualWidth / 2, p.ActualHeight / 2)),
                    targetPoint))
                .FirstOrDefault();
        }

        /// <summary>
        /// 获取元素的视觉父级
        /// </summary>
        public static DependencyObject? GetVisualParent(this DependencyObject element)
        {
            return VisualTreeHelper.GetParent(element);
        }
    }
}
