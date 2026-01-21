using System.Collections.ObjectModel;
using System.Windows.Input;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.ViewModels
{
    /// <summary>
    /// 工具箱视图模型
    /// </summary>
    public class ToolboxViewModel : ViewModelBase
    {
        private string _selectedCategory = "全部";
        private string _searchText = "";

        public ObservableCollection<ToolCategory> Categories { get; }
        public ObservableCollection<ToolItem> Tools { get; }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    FilterTools();
                }
            }
        }

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

        public ObservableCollection<ToolItem> FilteredTools { get; }

        public ICommand SelectCategoryCommand { get; }
        public ICommand UseToolCommand { get; }

        public ToolboxViewModel()
        {
            Categories = new ObservableCollection<ToolCategory>();
            Tools = new ObservableCollection<ToolItem>();
            FilteredTools = new ObservableCollection<ToolItem>();

            SelectCategoryCommand = new RelayCommand<string>(ExecuteSelectCategory);
            UseToolCommand = new RelayCommand<ToolItem>(ExecuteUseTool);

            InitializeData();
        }

        private void InitializeData()
        {
            Categories.Add(new ToolCategory("全部", "", "所有工具", 0));
            Categories.Add(new ToolCategory("图像处理", "", "基本图像操作", 0));
            Categories.Add(new ToolCategory("图像增强", "", "图像增强算法", 0));
            Categories.Add(new ToolCategory("特征提取", "", "特征提取与识别", 0));
            Categories.Add(new ToolCategory("深度学习", "", "AI深度学习模型", 0));
            Categories.Add(new ToolCategory("设备控制", "", "硬件设备管理", 0));

            Tools.Add(new ToolItem("灰度转换", "图像处理", "", "将彩色图像转换为灰度图像", "GrayScale"));
            Tools.Add(new ToolItem("高斯模糊", "图像处理", "", "应用高斯模糊滤镜", "GaussianBlur"));
            Tools.Add(new ToolItem("图像缩放", "图像处理", "", "调整图像尺寸", "Resize"));
            Tools.Add(new ToolItem("图像旋转", "图像处理", "", "旋转或翻转图像", "Rotate"));
            Tools.Add(new ToolItem("图像裁剪", "图像处理", "", "裁剪图像区域", "Crop"));
            Tools.Add(new ToolItem("二值化", "图像增强", "", "将图像转换为二值图像", "Threshold"));
            Tools.Add(new ToolItem("边缘检测", "图像增强", "", "检测图像边缘", "EdgeDetection"));
            Tools.Add(new ToolItem("形态学操作", "图像增强", "", "形态学操作（腐蚀、膨胀等）", "Morphology"));
            Tools.Add(new ToolItem("直方图", "图像增强", "", "增强图像对比度", "Histogram"));
            Tools.Add(new ToolItem("Blob检测", "特征提取", "", "检测图像中的斑点", "BlobDetection"));
            Tools.Add(new ToolItem("直线检测", "特征提取", "", "检测图像中的直线", "LineDetection"));
            Tools.Add(new ToolItem("圆形检测", "特征提取", "", "检测图像中的圆形", "CircleDetection"));
            Tools.Add(new ToolItem("图像分类", "深度学习", "", "使用深度学习进行图像分类", "ImageClassification"));
            Tools.Add(new ToolItem("目标检测", "深度学习", "", "使用深度学习检测图像中的目标", "ObjectDetection"));
            Tools.Add(new ToolItem("图像采集", "设备控制", "", "从相机采集图像", "ImageCapture"));

            FilterTools();
        }

        private void FilterTools()
        {
            FilteredTools.Clear();

            foreach (var tool in Tools)
            {
                bool categoryMatch = SelectedCategory == "全部" || tool.Category == SelectedCategory;
                bool searchMatch = string.IsNullOrEmpty(SearchText) ||
                                 tool.Name.Contains(SearchText) ||
                                 tool.Description.Contains(SearchText);

                if (categoryMatch && searchMatch)
                {
                    FilteredTools.Add(tool);
                }
            }
        }

        private void ExecuteSelectCategory(string category)
        {
            SelectedCategory = category;
        }

        private void ExecuteUseTool(ToolItem tool)
        {
            // TODO: 实现工具使用事件
        }
    }
}
