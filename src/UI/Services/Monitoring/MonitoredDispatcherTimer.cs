using System;
using System.Windows.Threading;

namespace SunEyeVision.UI.Services.Monitoring;

/// <summary>
/// 可监控的DispatcherTimer包装器
/// 自动注册到DispatcherTimerMonitor并进行监控
/// </summary>
public class MonitoredDispatcherTimer
{
    private readonly DispatcherTimer _timer;
    private readonly int _timerId;
    private readonly string _timerName;

    /// <summary>
    /// 创建一个可监控的DispatcherTimer
    /// </summary>
    /// <param name="interval">时间间隔</param>
    /// <param name="ownerName">所有者名称（自动推断）</param>
    /// <param name="timerName">定时器名称</param>
    public MonitoredDispatcherTimer(TimeSpan interval, string? ownerName = null, string? timerName = null)
    {
        _timer = new DispatcherTimer { Interval = interval };
        _timerName = timerName ?? "UnnamedTimer";

        // 注册到监控
        _timerId = DispatcherTimerMonitor.Instance.RegisterTimer(_timer, ownerName, _timerName);

        // 包装Tick事件
        _timer.Tick += (sender, e) =>
        {
            DispatcherTimerMonitor.Instance.LogTimerTick(_timerId);

            // 调用原始的Tick事件处理器
            _originalTickHandler?.Invoke(sender, e);
        };
    }

    private EventHandler? _originalTickHandler;

    /// <summary>
    /// Tick事件
    /// </summary>
    public event EventHandler? Tick
    {
        add
        {
            _originalTickHandler += value;
            if (value != null)
            {
                _timer.Tick += value;
                DispatcherTimerMonitor.Instance.LogTimerStateChange(_timerId, "Add_Tick", value.Method?.Name);
            }
        }
        remove
        {
            if (value != null)
            {
                _timer.Tick -= value;
                _originalTickHandler -= value;
                DispatcherTimerMonitor.Instance.LogTimerStateChange(_timerId, "Remove_Tick", value.Method?.Name);
            }
        }
    }

    /// <summary>
    /// 时间间隔
    /// </summary>
    public TimeSpan Interval
    {
        get => _timer.Interval;
        set
        {
            _timer.Interval = value;
            DispatcherTimerMonitor.Instance.LogTimerStateChange(_timerId, "Set_Interval", value.ToString());
        }
    }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled
    {
        get => _timer.IsEnabled;
        set
        {
            _timer.IsEnabled = value;
            DispatcherTimerMonitor.Instance.LogTimerStateChange(_timerId, "Set_Enabled", value.ToString());
        }
    }

    /// <summary>
    /// 启动定时器
    /// </summary>
    public void Start()
    {
        _timer.Start();
        DispatcherTimerMonitor.Instance.LogTimerStateChange(_timerId, "Start");
    }

    /// <summary>
    /// 停止定时器
    /// </summary>
    public void Stop()
    {
        _timer.Stop();
        DispatcherTimerMonitor.Instance.LogTimerStateChange(_timerId, "Stop");
    }

    /// <summary>
    /// 获取Timer ID
    /// </summary>
    public int TimerId => _timerId;

    /// <summary>
    /// 获取Timer名称
    /// </summary>
    public string TimerName => _timerName;

    /// <summary>
    /// 获取内部DispatcherTimer（谨慎使用）
    /// </summary>
    public DispatcherTimer InnerTimer => _timer;

    /// <summary>
    /// 静态工厂方法：创建一个可监控的DispatcherTimer
    /// </summary>
    public static MonitoredDispatcherTimer Create(TimeSpan interval, string? ownerName = null, string? timerName = null)
    {
        return new MonitoredDispatcherTimer(interval, ownerName, timerName);
    }
}
