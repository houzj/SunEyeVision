using System;
using System.Collections.Generic;
using System.Windows;

namespace SunEyeVision.UI.Services
{
    /// <summary>
    /// 画布状态枚举
    /// </summary>
    public enum CanvasState
    {
        /// <summary>
        /// 空闲状态
        /// </summary>
        Idle,
        
        /// <summary>
        /// 拖拽节点
        /// </summary>
        DraggingNode,
        
        /// <summary>
        /// 拖拽连接
        /// </summary>
        DraggingConnection,
        
        /// <summary>
        /// 框选
        /// </summary>
        BoxSelecting,
        
        /// <summary>
        /// 创建连接
        /// </summary>
        CreatingConnection
    }

    /// <summary>
    /// 画布状态变化事件参数
    /// </summary>
    public class CanvasStateChangedEventArgs : EventArgs
    {
        public CanvasState OldState { get; }
        public CanvasState NewState { get; }
        public DateTime Timestamp { get; }

        public CanvasStateChangedEventArgs(CanvasState oldState, CanvasState newState)
        {
            OldState = oldState;
            NewState = newState;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 画布状态管理器 - 管理画布的交互状态
    /// </summary>
    public class CanvasStateManager
    {
        private CanvasState _currentState = CanvasState.Idle;
        private readonly Stack<CanvasState> _stateHistory = new Stack<CanvasState>();
        private readonly object _lockObj = new object();

        /// <summary>
        /// 当前状态
        /// </summary>
        public CanvasState CurrentState
        {
            get
            {
                lock (_lockObj)
                {
                    return _currentState;
                }
            }
        }

        /// <summary>
        /// 状态变化事件
        /// </summary>
        public event EventHandler<CanvasStateChangedEventArgs>? StateChanged;

        /// <summary>
        /// 检查是否可以转换到指定状态
        /// </summary>
        /// <param name="newState">目标状态</param>
        /// <returns>是否可以转换</returns>
        public bool CanTransitionTo(CanvasState newState)
        {
            lock (_lockObj)
            {
                // 定义状态转换规则
                return (_currentState, newState) switch
                {
                    (CanvasState.Idle, _) => true, // 空闲状态可以转换到任何状态
                    (CanvasState.DraggingNode, CanvasState.Idle) => true, // 拖拽节点可以回到空闲
                    (CanvasState.DraggingConnection, CanvasState.Idle) => true, // 拖拽连接可以回到空闲
                    (CanvasState.BoxSelecting, CanvasState.Idle) => true, // 框选可以回到空闲
                    (CanvasState.CreatingConnection, CanvasState.Idle) => true, // 创建连接可以回到空闲
                    _ => (newState == CanvasState.Idle) // 任何状态都可以强制回到空闲
                };
            }
        }

        /// <summary>
        /// 转换到指定状态
        /// </summary>
        /// <param name="newState">目标状态</param>
        /// <exception cref="InvalidOperationException">当无法转换时抛出</exception>
        public void TransitionTo(CanvasState newState)
        {
            lock (_lockObj)
            {
                if (!CanTransitionTo(newState))
                {
                    throw new InvalidOperationException($"无法从 {_currentState} 转换到 {newState}");
                }

                var oldState = _currentState;
                _stateHistory.Push(_currentState);
                _currentState = newState;

                OnStateChanged(new CanvasStateChangedEventArgs(oldState, newState));
            }
        }

        /// <summary>
        /// 重置到空闲状态
        /// </summary>
        public void Reset()
        {
            lock (_lockObj)
            {
                var oldState = _currentState;
                _stateHistory.Clear();
                _currentState = CanvasState.Idle;

                OnStateChanged(new CanvasStateChangedEventArgs(oldState, CanvasState.Idle));
            }
        }

        /// <summary>
        /// 撤销到上一个状态
        /// </summary>
        /// <returns>是否成功撤销</returns>
        public bool Undo()
        {
            lock (_lockObj)
            {
                if (_stateHistory.Count == 0)
                {
                    return false;
                }

                var oldState = _currentState;
                _currentState = _stateHistory.Pop();

                OnStateChanged(new CanvasStateChangedEventArgs(oldState, _currentState));
                return true;
            }
        }

        /// <summary>
        /// 获取状态历史
        /// </summary>
        /// <returns>状态历史列表</returns>
        public List<CanvasState> GetHistory()
        {
            lock (_lockObj)
            {
                return new List<CanvasState>(_stateHistory);
            }
        }

        /// <summary>
        /// 获取当前状态的描述
        /// </summary>
        /// <returns>状态描述</returns>
        public string GetStateDescription()
        {
            lock (_lockObj)
            {
                return _currentState switch
                {
                    CanvasState.Idle => "空闲",
                    CanvasState.DraggingNode => "拖拽节点中",
                    CanvasState.DraggingConnection => "拖拽连接中",
                    CanvasState.BoxSelecting => "框选中",
                    CanvasState.CreatingConnection => "创建连接中",
                    _ => "未知状态"
                };
            }
        }

        /// <summary>
        /// 触发状态变化事件
        /// </summary>
        /// <param name="e">事件参数</param>
        protected virtual void OnStateChanged(CanvasStateChangedEventArgs e)
        {
            StateChanged?.Invoke(this, e);
        }
    }
}
