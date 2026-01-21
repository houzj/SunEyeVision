using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SunEyeVision.PluginSystem;
using SunEyeVision.PluginSystem.SampleTools;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.ViewModels
{
    /// <summary>
    /// 工具箱视图模型 - 支持动态插件加载
    /// </summary>
    public class ToolboxViewModel : ViewModelBase
    {
        private string _searchText = "";
        private ObservableCollection<ToolItem> _filteredTools;
        private readonly ToolRegistry _toolRegistry;

        public ObservableCollection<ToolCategory> Categories { get; }
        public ObservableCollection<ToolItem> AllTools { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterTools();
                }
            }
        }

        public ObservableCollection<ToolItem> FilteredTools
        {
            get => _filteredTools;
            set => SetProperty(ref _filteredTools, value);
        }

        public ICommand ToggleCategoryCommand { get; }
        public ICommand UseToolCommand { get; }
        public ICommand ExpandAllCommand { get; }
        public ICommand CollapseAllCommand { get; }

        public ToolboxViewModel()
        {
            Categories = new ObservableCollection<ToolCategory>();
            AllTools = new ObservableCollection<ToolItem>();
            FilteredTools = new ObservableCollection<ToolItem>();
            _toolRegistry = new ToolRegistry();

            ToggleCategoryCommand = new RelayCommand<ToolCategory>(ExecuteToggleCategory);
            UseToolCommand = new RelayCommand<ToolItem>(ExecuteUseTool);
            ExpandAllCommand = new RelayCommand(ExecuteExpandAll);
            CollapseAllCommand = new RelayCommand(ExecuteCollapseAll);

            InitializeFromPlugins();
        }

        /// <summary>
        /// 从插件初始化工具箱
        /// </summary>
        private void InitializeFromPlugins()
        {
            // 清空现有数据
            Categories.Clear();
            AllTools.Clear();
            _toolRegistry.Clear();

            // 创建并注册示例工具插件
            var imageCapturePlugin = new ImageCaptureTool();
            var templateMatchingPlugin = new TemplateMatchingTool();
            var gaussianBlurPlugin = new GaussianBlurTool();
            var ocrPlugin = new OCRTool();

            // 注册插件
            RegisterPlugin(imageCapturePlugin);
            RegisterPlugin(templateMatchingPlugin);
            RegisterPlugin(gaussianBlurPlugin);
            RegisterPlugin(ocrPlugin);

            // 从ToolRegistry加载工具
            LoadToolsFromRegistry();

            // 更新分类的工具数量
            UpdateCategoryToolCounts();

            // 初始化过滤后的工具
            FilteredTools = new ObservableCollection<ToolItem>(AllTools);
        }

        /// <summary>
        /// 注册工具插件
        /// </summary>
        private void RegisterPlugin(IToolPlugin plugin)
        {
            plugin.Initialize();
            _toolRegistry.RegisterFromPlugin(plugin);
        }

        /// <summary>
        /// 从工具注册中心加载工具
        /// </summary>
        private void LoadToolsFromRegistry()
        {
            var categories = _toolRegistry.GetAllCategories();
            foreach (var category in categories)
            {
                var categoryIcon = GetCategoryIcon(category);
                var categoryDesc = GetCategoryDescription(category);
                Categories.Add(new ToolCategory(category, categoryIcon, categoryDesc, 0, false));
            }

            var tools = _toolRegistry.GetAllTools();
            foreach (var tool in tools)
            {
                var toolItem = new ToolItem(
                    tool.DisplayName,
                    tool.Category,
                    tool.Icon,
                    tool.Description,
                    tool.AlgorithmType?.Name
                );
                AllTools.Add(toolItem);
            }
        }

        /// <summary>
        /// 获取分类图标
        /// </summary>
        private string GetCategoryIcon(string category)
        {
            return category switch
            {
                "采集" => "📷",
                "定位" => "📍",
                "图像处理" => "🖼️",
                "识别" => "🔍",
                "测量" => "📏",
                _ => "🔧"
            };
        }

        /// <summary>
        /// 获取分类描述
        /// </summary>
        private string GetCategoryDescription(string category)
        {
            return category + "相关工具";
        }

        private void UpdateCategoryToolCounts()
        {
            foreach (var category in Categories)
            {
                category.ToolCount = AllTools.Count(t => t.Category == category.Name);
                // 为每个分类过滤工具
                var filtered = AllTools.Where(t => t.Category == category.Name).ToList();
                category.FilteredToolsForCategory = new System.Collections.ObjectModel.ObservableCollection<ToolItem>(filtered);
            }
        }

        private void FilterTools()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredTools = new ObservableCollection<ToolItem>(AllTools);
            }
            else
            {
                var filtered = AllTools.Where(t =>
                    t.Name.Contains(SearchText) ||
                    t.Description.Contains(SearchText)
                ).ToList();
                FilteredTools = new ObservableCollection<ToolItem>(filtered);
            }
        }

        private void ExecuteToggleCategory(ToolCategory category)
        {
            category.IsExpanded = !category.IsExpanded;
        }

        private void ExecuteUseTool(ToolItem tool)
        {
            // TODO: 实现工具使用事件
        }

        private void ExecuteExpandAll()
        {
            foreach (var category in Categories)
            {
                category.IsExpanded = true;
            }
        }

        private void ExecuteCollapseAll()
        {
            foreach (var category in Categories)
            {
                category.IsExpanded = false;
            }
        }
    }
}
