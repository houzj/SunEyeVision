using System;
using System.Reflection;
using SunEyeVision.UI.Adapters;

namespace SunEyeVision.UI.Adapters
{
    /// <summary>
    /// 库验证器 - 验证AIStudio.Wpf.DiagramDesigner的API和连接算�?
    /// </summary>
    public static class LibraryValidator
    {
        /// <summary>
        /// 验证库是否可用并输出支持的连接算�?
        /// </summary>
        public static void ValidateConnectionAlgorithms()
        {
            try
            {

                // 加载程序�?
                var assembly = Assembly.Load("AIStudio.Wpf.DiagramDesigner");

                // 获取核心类型
                var diagramType = assembly.GetType("AIStudio.Wpf.DiagramDesigner.DiagramDesigner");
                var nodeType = assembly.GetType("AIStudio.Wpf.DiagramDesigner.Node");
                var linkType = assembly.GetType("AIStudio.Wpf.DiagramDesigner.Link");


                // 检查连接算法枚�?
                var algorithmEnumType = assembly.GetType("AIStudio.Wpf.DiagramDesigner.LinkAlgorithm");
                if (algorithmEnumType != null)
                {
                    foreach (var value in Enum.GetValues(algorithmEnumType))
                    {
                    }
                }
                else
                {
                }

                // 检查Diagram类型
                var diagramClassType = assembly.GetType("AIStudio.Wpf.DiagramDesigner.Diagram");
                if (diagramClassType != null)
                {
                    
                    // 检查关键属�?
                    var nodesProperty = diagramClassType.GetProperty("Nodes");
                    var linksProperty = diagramClassType.GetProperty("Links");
                    var zoomProperty = diagramClassType.GetProperty("Zoom");

                }

            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// 检查库是否可用
        /// </summary>
        public static bool IsLibraryAvailable()
        {
            try
            {
                var assembly = Assembly.Load("AIStudio.Wpf.DiagramDesigner");
                return assembly != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取LinkAlgorithm枚举类型
        /// </summary>
        public static Type? GetLinkAlgorithmEnumType()
        {
            try
            {
                var assembly = Assembly.Load("AIStudio.Wpf.DiagramDesigner");
                return assembly.GetType("AIStudio.Wpf.DiagramDesigner.LinkAlgorithm");
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 列出程序集中所有类型，帮助诊断API结构
        /// </summary>
        public static void ListAllTypes()
        {
            try
            {
                var assembly = Assembly.Load("AIStudio.Wpf.DiagramDesigner");
                var types = assembly.GetTypes();


            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// 检查DiagramControl的属�?
        /// </summary>
        public static void InspectDiagramControlProperties(object diagramControl)
        {
            try
            {
                var diagramType = diagramControl.GetType();
                var properties = diagramType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                // 检查SourceItemsContainer的详细信�?
                var sourceItemsContainerProp = diagramType.GetProperty("SourceItemsContainer");
                if (sourceItemsContainerProp != null)
                {
                    var container = sourceItemsContainerProp.GetValue(diagramControl);
                    if (container != null)
                    {
                        var containerProps = container.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        var methods = container.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
                    }
                }
            }
            catch (Exception ex)
            {
                // 忽略异常
            }
        }

        /// <summary>
        /// 检查BlockItemsContainer的创建问�?
        /// </summary>
        public static void CheckBlockItemsContainerCreation()
        {
            try
            {


                var assembly = Assembly.Load("AIStudio.Wpf.DiagramDesigner");
                var blockItemsContainerType = assembly.GetType("AIStudio.Wpf.DiagramDesigner.BlockItemsContainer");

                if (blockItemsContainerType == null)
                {

                    return;
                }



                // 检查构造函�?
                var constructors = blockItemsContainerType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);


                foreach (var ctor in constructors)
                {
                    var parameters = string.Join(", ", ctor.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));

                }

                // 尝试创建实例

                try
                {
                    var instance = Activator.CreateInstance(blockItemsContainerType);


                    // 检查实例的属�?
                    var props = instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

                    foreach (var prop in props)
                    {
                        var value = prop.GetValue(instance);

                    }
                }
                catch (Exception ex)
                {


                }


            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// 检查DesignerCanvas的SourceItemsContainer设置
        /// </summary>
        public static void CheckSourceItemsContainerSetting()
        {
            try
            {


                var assembly = Assembly.Load("AIStudio.Wpf.DiagramDesigner");
                var designerCanvasType = assembly.GetType("AIStudio.Wpf.DiagramDesigner.DesignerCanvas");
                var blockItemsContainerType = assembly.GetType("AIStudio.Wpf.DiagramDesigner.BlockItemsContainer");

                // 创建DesignerCanvas实例
                var designerCanvas = Activator.CreateInstance(designerCanvasType);


                // 创建BlockItemsContainer实例
                var container = Activator.CreateInstance(blockItemsContainerType);


                // 获取SourceItemsContainer属�?
                var sourceItemsContainerProperty = designerCanvasType.GetProperty("SourceItemsContainer");


                // 尝试设置属�?

                try
                {
                    sourceItemsContainerProperty?.SetValue(designerCanvas, container);


                    // 验证设置
                    var value = sourceItemsContainerProperty?.GetValue(designerCanvas);

                }
                catch (Exception ex)
                {



                    // 检查属性是否可�?
                    if (sourceItemsContainerProperty != null)
                    {




                    }
                }


            }
            catch (Exception ex)
            {

            }
        }
    }
}
