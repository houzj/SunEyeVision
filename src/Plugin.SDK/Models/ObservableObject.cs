using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SunEyeVision.Plugin.SDK.Logging;

namespace SunEyeVision.Plugin.SDK.Models
{
    /// <summary>
    /// 可观察对象基类 - 项目中所有属性变化通知的唯一源头
    /// </summary>
    /// <remarks>
    /// 特性：
    /// 1. 统一的属性变化通知机制
    /// 2. 可选的自动日志记录（通过 displayName 参数启用）
    /// 3. 扩展点支持子类自定义行为
    /// 
    /// 使用方式：
    /// <code>
    /// // 不记录日志的属性
    /// public int InternalValue
    /// {
    ///     get => _internalValue;
    ///     set => SetProperty(ref _internalValue, value);
    /// }
    /// 
    /// // 自动记录日志的属性（指定显示名称）
    /// public int Threshold
    /// {
    ///     get => _threshold;
    ///     set => SetProperty(ref _threshold, value, "阈值");
    /// }
    /// </code>
    /// </remarks>
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        /// <summary>
        /// 属性变更事件
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 触发属性变更通知
        /// </summary>
        /// <param name="propertyName">属性名称（自动获取）</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region SetProperty 方法

        /// <summary>
        /// 设置属性值（可选自动记录日志）
        /// </summary>
        /// <typeparam name="T">属性类型</typeparam>
        /// <param name="field">字段引用</param>
        /// <param name="value">新值</param>
        /// <param name="displayName">显示名称（传入时自动记录日志）</param>
        /// <param name="propertyName">属性名（自动获取）</param>
        /// <returns>是否发生了变化</returns>
        /// <remarks>
        /// 使用方式：
        /// <code>
        /// // 不记录日志
        /// SetProperty(ref _internalValue, value);
        /// 
        /// // 自动记录日志
        /// SetProperty(ref _threshold, value, "阈值");
        /// </code>
        /// </remarks>
        protected bool SetProperty<T>(
            ref T field,
            T value,
            string? displayName = null,
            [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            var oldValue = field;
            field = value;

            // 触发扩展处理
            OnPropertyChanging(propertyName, oldValue, value);

            // 自动记录日志（displayName 不为空时）
            if (!string.IsNullOrEmpty(displayName))
            {
                LogPropertyChange(displayName, oldValue, value);
            }

            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

        #region 扩展点

        /// <summary>
        /// 属性即将变化时的扩展处理（子类可重写）
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        /// <param name="oldValue">旧值</param>
        /// <param name="newValue">新值</param>
        protected virtual void OnPropertyChanging(string? propertyName, object? oldValue, object? newValue)
        {
            // 默认不做任何事，子类可以重写来添加额外逻辑
        }

        /// <summary>
        /// 记录属性变化日志（子类可重写自定义日志格式）
        /// </summary>
        /// <param name="displayName">显示名称</param>
        /// <param name="oldValue">旧值</param>
        /// <param name="newValue">新值</param>
        protected virtual void LogPropertyChange(string displayName, object? oldValue, object? newValue)
        {
            var source = GetLogSource();
            System.Diagnostics.Debug.WriteLine(
                $"[ObservableObject.LogPropertyChange] displayName={displayName}, oldValue={oldValue}, newValue={newValue}, source={source}");
            PluginLogger.ParameterChanged(displayName, oldValue, newValue, source);
            System.Diagnostics.Debug.WriteLine(
                $"[ObservableObject.LogPropertyChange] PluginLogger.ParameterChanged 调用完成");
        }

        /// <summary>
        /// 获取日志来源（子类可重写）
        /// </summary>
        /// <returns>日志来源名称</returns>
        protected virtual string? GetLogSource() => null;

        #endregion
    }
}
