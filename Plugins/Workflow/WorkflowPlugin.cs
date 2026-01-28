using SunEyeVision.Core.Interfaces.Plugins;
using SunEyeVision.UI.Shared.Controls.Common;
using System.Windows;
using System.Windows.Controls;

namespace SunEyeVision.Plugins.Workflow
{
    /// <summary>
    /// 工作流插件
    /// 实现IPlugin和INodePlugin接口
    /// 使用Hybrid模式（使用框架通用控件 + 自定义面板）
    /// </summary>
    public class WorkflowPlugin : INodePlugin, IPluginUIProvider
    {
        public string PluginId => "Workflow";
        public string PluginName => "Workflow Nodes Plugin";
        public string Version => "1.0.0";
        public string Description => "Provides workflow nodes";
        public string Author => "Team B";

        public string NodeType => "WorkflowNode";
        public string Icon => "workflow.png";
        public string Category => "Workflow";

        public UIProviderMode Mode => UIProviderMode.Hybrid;

        public PortDefinition[] InputPorts => new[]
        {
            new PortDefinition
            {
                Id = "input1",
                Name = "Input 1",
                DataType = "object",
                IsRequired = true
            },
            new PortDefinition
            {
                Id = "input2",
                Name = "Input 2",
                DataType = "object",
                IsRequired = false
            }
        };

        public PortDefinition[] OutputPorts => new[]
        {
            new PortDefinition
            {
                Id = "output",
                Name = "Output",
                DataType = "object",
                IsRequired = true
            }
        };

        private bool _isInitialized = false;
        private bool _isRunning = false;

        public void Initialize()
        {
            System.Console.WriteLine($"[WorkflowPlugin] Initializing...");
            _isInitialized = true;
        }

        public void Start()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
            System.Console.WriteLine($"[WorkflowPlugin] Starting...");
            _isRunning = true;
        }

        public void Stop()
        {
            System.Console.WriteLine($"[WorkflowPlugin] Stopping...");
            _isRunning = false;
        }

        public void Cleanup()
        {
            System.Console.WriteLine($"[WorkflowPlugin] Cleaning up...");
            _isInitialized = false;
        }

        public ParameterMetadata[] GetParameters()
        {
            return new[]
            {
                new ParameterMetadata
                {
                    Name = "nodeName",
                    DisplayName = "Node Name",
                    Type = "string",
                    DefaultValue = "New Node",
                    Description = "Display name of the node"
                },
                new ParameterMetadata
                {
                    Name = "executionMode",
                    DisplayName = "Execution Mode",
                    Type = "string",
                    DefaultValue = "Sync",
                    Options = new object[] { "Sync", "Async", "Batch" },
                    Description = "Node execution mode"
                },
                new ParameterMetadata
                {
                    Name = "timeout",
                    DisplayName = "Timeout (ms)",
                    Type = "int",
                    DefaultValue = 5000,
                    MinValue = 100,
                    MaxValue = 60000,
                    Description = "Execution timeout in milliseconds"
                }
            };
        }

        public object Execute(object[] inputs)
        {
            if (!_isRunning)
            {
                throw new System.InvalidOperationException("Plugin is not running");
            }

            System.Console.WriteLine($"[WorkflowPlugin] Executing with {inputs.Length} inputs");

            // 这里应该实现实际的工作流节点逻辑
            // 当前版本仅返回第一个输入作为示例
            return inputs.Length > 0 ? inputs[0] : null;
        }

        public object GetCustomControl()
        {
            // 使用框架通用控件
            return null;
        }

        public object GetCustomPanel()
        {
            // 返回自定义面板，使用共享UI组件
            var stackPanel = new StackPanel();
            
            // 添加状态指示器
            var statusIndicator = new StatusIndicator
            {
                Status = _isRunning ? "Running" : "Stopped",
                IsRunning = _isRunning
            };
            stackPanel.Children.Add(statusIndicator);

            // 添加进度面板
            var progressPanel = new ProgressPanel
            {
                ProgressText = "Ready",
                ProgressValue = 0
            };
            stackPanel.Children.Add(progressPanel);

            return stackPanel;
        }
    }
}
