using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SunEyeVision.Plugin.SDK.Models;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Models;
using SunEyeVision.Plugin.SDK.Logging;

namespace SunEyeVision.Plugin.SDK.UI.Controls.Region.ViewModels
{
    /// <summary>
    /// 参数面板视图模型
    /// </summary>
    public class ParameterPanelViewModel : ObservableObject, IDisposable
    {
        private readonly IRegionDataSourceProvider? _dataProvider;
        private ShapeType _currentShapeType;
        private bool _isEditable = true;
        private ParameterBindingItem? _selectedParameter;
        private readonly Dictionary<string, IDisposable> _subscriptions = new();

        /// <summary>
        /// 参数绑定项列表
        /// </summary>
        public ObservableCollection<ParameterBindingItem> Parameters { get; } = new();

        /// <summary>
        /// 当前形状类型
        /// </summary>
        public ShapeType CurrentShapeType
        {
            get => _currentShapeType;
            set
            {
                if (SetProperty(ref _currentShapeType, value))
                {
                    UpdateParameterDefinitions();
                }
            }
        }

        /// <summary>
        /// 是否可编辑
        /// </summary>
        public bool IsEditable
        {
            get => _isEditable;
            set => SetProperty(ref _isEditable, value);
        }

        /// <summary>
        /// 选中的参数（用于打开绑定选择器）
        /// </summary>
        public ParameterBindingItem? SelectedParameter
        {
            get => _selectedParameter;
            set => SetProperty(ref _selectedParameter, value);
        }

        /// <summary>
        /// 命令
        /// </summary>
        public ICommand BindParameterCommand { get; }
        public ICommand ClearBindingCommand { get; }

        /// <summary>
        /// 参数绑定变更事件
        /// </summary>
        public event EventHandler<ParameterBindingChangedEventArgs>? ParameterBindingChanged;

        public ParameterPanelViewModel(IRegionDataSourceProvider? dataProvider = null)
        {
            _dataProvider = dataProvider;

            BindParameterCommand = new RelayCommand<ParameterBindingItem>(BindParameter);
            ClearBindingCommand = new RelayCommand<ParameterBindingItem>(ClearBinding);
        }

        /// <summary>
        /// 更新参数定义（根据形状类型）
        /// </summary>
        private void UpdateParameterDefinitions()
        {
            // 清理旧的订阅
            foreach (var subscription in _subscriptions.Values)
            {
                subscription.Dispose();
            }
            _subscriptions.Clear();

            // 清空参数列表
            Parameters.Clear();

            // 根据形状类型定义参数
            var parameterDefs = GetParameterDefinitions(_currentShapeType);

            // 添加参数项
            foreach (var def in parameterDefs)
            {
                Parameters.Add(new ParameterBindingItem
                {
                    ParameterName = def.Name,
                    DisplayName = def.DisplayName,
                    DataType = def.DataType
                });
            }
        }

        /// <summary>
        /// 获取参数定义
        /// </summary>
        private List<(string Name, string DisplayName, string DataType)> GetParameterDefinitions(ShapeType shapeType)
        {
            return shapeType switch
            {
                ShapeType.Point => new List<(string, string, string)>
                {
                    ("CenterX", "中心X", "double"),
                    ("CenterY", "中心Y", "double")
                },
                ShapeType.Line => new List<(string, string, string)>
                {
                    ("StartX", "起点X", "double"),
                    ("StartY", "起点Y", "double"),
                    ("EndX", "终点X", "double"),
                    ("EndY", "终点Y", "double")
                },
                ShapeType.Circle => new List<(string, string, string)>
                {
                    ("CenterX", "中心X", "double"),
                    ("CenterY", "中心Y", "double"),
                    ("Radius", "半径", "double")
                },
                ShapeType.Rectangle => new List<(string, string, string)>
                {
                    ("CenterX", "中心X", "double"),
                    ("CenterY", "中心Y", "double"),
                    ("Width", "宽度", "double"),
                    ("Height", "高度", "double")
                },
                ShapeType.RotatedRectangle => new List<(string, string, string)>
                {
                    ("CenterX", "中心X", "double"),
                    ("CenterY", "中心Y", "double"),
                    ("Width", "宽度", "double"),
                    ("Height", "高度", "double"),
                    ("Angle", "角度", "double")
                },
                ShapeType.Polygon => new List<(string, string, string)>
                {
                    ("Points", "顶点", "Point2D[]")
                },
                _ => new List<(string, string, string)>()
            };
        }

        /// <summary>
        /// 绑定参数（打开选择器）
        /// </summary>
        private void BindParameter(ParameterBindingItem? item)
        {
            if (item == null) return;
            SelectedParameter = item;
            // 实际绑定在View层通过NodeSelectorPopup完成
        }

        /// <summary>
        /// 清除绑定
        /// </summary>
        private void ClearBinding(ParameterBindingItem? item)
        {
            if (item == null) return;

            item.Source = null;
            item.DisplayPath = string.Empty;
            item.CurrentValue = null;

            ParameterBindingChanged?.Invoke(this, new ParameterBindingChangedEventArgs(item.ParameterName, null));
        }

        /// <summary>
        /// 应用绑定选择结果
        /// </summary>
        public void ApplyBindingSelection(NodeOutputInfo selectedNode)
        {
            if (SelectedParameter == null) return;

            var source = new NodeOutputSource(
                selectedNode.NodeId,
                selectedNode.OutputName,
                null,
                selectedNode.PropertyPath)
            {
                DataType = SelectedParameter.DataType
            };

            SelectedParameter.Source = source;
            SelectedParameter.DisplayPath = selectedNode.DisplayPath;

            // 订阅值变更
            SubscribeToValue(SelectedParameter, source);

            ParameterBindingChanged?.Invoke(this, new ParameterBindingChangedEventArgs(SelectedParameter.ParameterName, source));
        }

        /// <summary>
        /// 订阅值变更
        /// </summary>
        private void SubscribeToValue(ParameterBindingItem item, NodeOutputSource source)
        {
            // 取消旧订阅
            if (_subscriptions.TryGetValue(item.ParameterName, out var oldSubscription))
            {
                oldSubscription.Dispose();
                _subscriptions.Remove(item.ParameterName);
            }

            if (_dataProvider == null) return;

            // 创建新订阅
            var subscription = _dataProvider.SubscribeOutputChanged(
                source.NodeId,
                source.OutputName,
                source.PropertyPath,
                value => item.CurrentValue = value);

            _subscriptions[item.ParameterName] = subscription;

            // 立即获取当前值
            item.CurrentValue = _dataProvider.GetCurrentBindingValue(
                source.NodeId,
                source.OutputName,
                source.PropertyPath);
        }

        /// <summary>
        /// 从ComputedRegion加载绑定
        /// </summary>
        public void LoadFromComputedRegion(ComputedRegion computedRegion)
        {
            CurrentShapeType = computedRegion.TargetShapeType;

            foreach (var param in Parameters)
            {
                if (computedRegion.ParameterBindings.TryGetValue(param.ParameterName, out var source))
                {
                    param.Source = source;

                    if (source is NodeOutputSource nodeSource && _dataProvider != null)
                    {
                        param.DisplayPath = _dataProvider.GetBindingDisplayPath(
                            nodeSource.NodeId,
                            nodeSource.OutputName,
                            nodeSource.PropertyPath);

                        SubscribeToValue(param, nodeSource);
                    }
                }
            }
        }

        /// <summary>
        /// 导出到ComputedRegion
        /// </summary>
        public ComputedRegion ExportToComputedRegion()
        {
            var result = new ComputedRegion
            {
                TargetShapeType = CurrentShapeType
            };

            foreach (var param in Parameters)
            {
                if (param.Source != null)
                {
                    result.SetParameterBinding(param.ParameterName, param.Source);
                }
            }

            return result;
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions.Values)
            {
                subscription.Dispose();
            }
            _subscriptions.Clear();
        }
    }

    /// <summary>
    /// 参数绑定变更事件参数
    /// </summary>
    public class ParameterBindingChangedEventArgs : EventArgs
    {
        public string ParameterName { get; }
        public ParameterSource? NewSource { get; }

        public ParameterBindingChangedEventArgs(string parameterName, ParameterSource? newSource)
        {
            ParameterName = parameterName;
            NewSource = newSource;
        }
    }
}
