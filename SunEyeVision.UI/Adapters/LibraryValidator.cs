using System;
using System.Reflection;

namespace SunEyeVision.UI.Adapters
{
    /// <summary>
    /// 库验证器 - 验证AIStudio.Wpf.DiagramDesigner的API和连接算法
    /// </summary>
    public static class LibraryValidator
    {
        /// <summary>
        /// 验证库是否可用并输出支持的连接算法
        /// </summary>
        public static void ValidateConnectionAlgorithms()
        {
            try
            {
                Console.WriteLine("========== 验证 AIStudio.Wpf.DiagramDesigner 库 ==========");

                // 加载程序集
                var assembly = Assembly.Load("AIStudio.Wpf.DiagramDesigner");
                Console.WriteLine($"✅ 程序集加载成功: {assembly.FullName}");
                Console.WriteLine($"   位置: {assembly.Location}");

                // 获取核心类型
                var diagramType = assembly.GetType("AIStudio.Wpf.DiagramDesigner.DiagramDesigner");
                var nodeType = assembly.GetType("AIStudio.Wpf.DiagramDesigner.Node");
                var linkType = assembly.GetType("AIStudio.Wpf.DiagramDesigner.Link");

                Console.WriteLine($"\n核心类型:");
                Console.WriteLine($"   DiagramDesigner: {diagramType != null}");
                Console.WriteLine($"   Node: {nodeType != null}");
                Console.WriteLine($"   Link: {linkType != null}");

                // 检查连接算法枚举
                var algorithmEnumType = assembly.GetType("AIStudio.Wpf.DiagramDesigner.LinkAlgorithm");
                if (algorithmEnumType != null)
                {
                    Console.WriteLine($"\n✅ LinkAlgorithm 枚举类型存在");
                    Console.WriteLine("\n支持的连接算法:");
                    foreach (var value in Enum.GetValues(algorithmEnumType))
                    {
                        Console.WriteLine($"   - {value}");
                    }
                }
                else
                {
                    Console.WriteLine($"\n❌ LinkAlgorithm 枚举类型不存在");
                }

                // 检查Diagram类型
                var diagramClassType = assembly.GetType("AIStudio.Wpf.DiagramDesigner.Diagram");
                if (diagramClassType != null)
                {
                    Console.WriteLine($"\n✅ Diagram 类存在");
                    
                    // 检查关键属性
                    var nodesProperty = diagramClassType.GetProperty("Nodes");
                    var linksProperty = diagramClassType.GetProperty("Links");
                    var zoomProperty = diagramClassType.GetProperty("Zoom");

                    Console.WriteLine("\n关键属性:");
                    Console.WriteLine($"   Nodes: {nodesProperty != null}");
                    Console.WriteLine($"   Links: {linksProperty != null}");
                    Console.WriteLine($"   Zoom: {zoomProperty != null}");
                }

                Console.WriteLine("\n========== 验证完成 ==========\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 库验证失败: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
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

                System.Diagnostics.Debug.WriteLine("========== AIStudio.Wpf.DiagramDesigner 所有类型 ==========");
                System.Diagnostics.Debug.WriteLine($"类型总数: {types.Length}");

                // 分组显示
                var controls = types.Where(t => typeof(System.Windows.Controls.Control).IsAssignableFrom(t)).ToList();
                var models = types.Where(t => !typeof(System.Windows.Controls.Control).IsAssignableFrom(t) &&
                                            !t.IsInterface && !t.IsEnum && !t.IsValueType).ToList();

                System.Diagnostics.Debug.WriteLine("\n【控件类型】(继承自Control):");
                foreach (var type in controls)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {type.FullName}");
                }

                System.Diagnostics.Debug.WriteLine("\n【模型/数据类型】:");
                foreach (var type in models.Take(50))
                {
                    System.Diagnostics.Debug.WriteLine($"  - {type.FullName}");
                }

                if (models.Count > 50)
                {
                    System.Diagnostics.Debug.WriteLine($"  ... 还有 {models.Count - 50} 个模型类型");
                }

                System.Diagnostics.Debug.WriteLine("\n========== 列出完成 ==========");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ 列出类型失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查DiagramControl的属性
        /// </summary>
        public static void InspectDiagramControlProperties(object diagramControl)
        {
            try
            {
                var diagramType = diagramControl.GetType();
                var properties = diagramType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                // 检查SourceItemsContainer的详细信息
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
        /// 检查BlockItemsContainer的创建问题
        /// </summary>
        public static void CheckBlockItemsContainerCreation()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("\n========== BlockItemsContainer 创建测试 ==========");

                var assembly = Assembly.Load("AIStudio.Wpf.DiagramDesigner");
                var blockItemsContainerType = assembly.GetType("AIStudio.Wpf.DiagramDesigner.BlockItemsContainer");

                if (blockItemsContainerType == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ BlockItemsContainer类型未找到");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"✅ BlockItemsContainer类型: {blockItemsContainerType.FullName}");

                // 检查构造函数
                var constructors = blockItemsContainerType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                System.Diagnostics.Debug.WriteLine($"\n公共构造函数数量: {constructors.Length}");

                foreach (var ctor in constructors)
                {
                    var parameters = string.Join(", ", ctor.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                    System.Diagnostics.Debug.WriteLine($"  - {blockItemsContainerType.Name}({parameters})");
                }

                // 尝试创建实例
                System.Diagnostics.Debug.WriteLine("\n尝试创建实例:");
                try
                {
                    var instance = Activator.CreateInstance(blockItemsContainerType);
                    System.Diagnostics.Debug.WriteLine($"✅ 实例创建成功: {instance.GetType().FullName}");

                    // 检查实例的属性
                    var props = instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    System.Diagnostics.Debug.WriteLine("\n实例属性:");
                    foreach (var prop in props)
                    {
                        var value = prop.GetValue(instance);
                        System.Diagnostics.Debug.WriteLine($"  - {prop.Name}: {value ?? "null"}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ 实例创建失败: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"堆栈: {ex.StackTrace}");
                }

                System.Diagnostics.Debug.WriteLine("\n========== 测试完成 ==========");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ 测试失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查DesignerCanvas的SourceItemsContainer设置
        /// </summary>
        public static void CheckSourceItemsContainerSetting()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("\n========== SourceItemsContainer 设置测试 ==========");

                var assembly = Assembly.Load("AIStudio.Wpf.DiagramDesigner");
                var designerCanvasType = assembly.GetType("AIStudio.Wpf.DiagramDesigner.DesignerCanvas");
                var blockItemsContainerType = assembly.GetType("AIStudio.Wpf.DiagramDesigner.BlockItemsContainer");

                // 创建DesignerCanvas实例
                var designerCanvas = Activator.CreateInstance(designerCanvasType);
                System.Diagnostics.Debug.WriteLine($"✅ DesignerCanvas实例创建成功");

                // 创建BlockItemsContainer实例
                var container = Activator.CreateInstance(blockItemsContainerType);
                System.Diagnostics.Debug.WriteLine($"✅ BlockItemsContainer实例创建成功");

                // 获取SourceItemsContainer属性
                var sourceItemsContainerProperty = designerCanvasType.GetProperty("SourceItemsContainer");
                System.Diagnostics.Debug.WriteLine($"✅ SourceItemsContainer属性: {sourceItemsContainerProperty?.Name}");

                // 尝试设置属性
                System.Diagnostics.Debug.WriteLine("\n尝试设置SourceItemsContainer属性:");
                try
                {
                    sourceItemsContainerProperty?.SetValue(designerCanvas, container);
                    System.Diagnostics.Debug.WriteLine($"✅ SourceItemsContainer属性设置成功");

                    // 验证设置
                    var value = sourceItemsContainerProperty?.GetValue(designerCanvas);
                    System.Diagnostics.Debug.WriteLine($"✅ 验证: SourceItemsContainer = {(value != null ? value.GetType().Name : "null")}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ SourceItemsContainer属性设置失败: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"堆栈: {ex.StackTrace}");

                    // 检查属性是否可写
                    if (sourceItemsContainerProperty != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"\n属性详细信息:");
                        System.Diagnostics.Debug.WriteLine($"  - CanRead: {sourceItemsContainerProperty.CanRead}");
                        System.Diagnostics.Debug.WriteLine($"  - CanWrite: {sourceItemsContainerProperty.CanWrite}");
                        System.Diagnostics.Debug.WriteLine($"  - PropertyType: {sourceItemsContainerProperty.PropertyType.Name}");
                    }
                }

                System.Diagnostics.Debug.WriteLine("\n========== 测试完成 ==========");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ 测试失败: {ex.Message}");
            }
        }
    }
}
