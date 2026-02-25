#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
MainWindow.xaml.cs 完整编码修复脚本
"""

import re

# 完整的注释修复映射
FIXES = [
    # 构造函数和初始化
    ('// ��ʼ��������������� - ����Ĭ������', '// 初始化画布引擎管理器 - 设置默认配置'),
    ('// ��̨�л���NativeDiagramControl��ʹ��ԭ��AIStudio.Wpf.DiagramDesigner�⣩', '// 后台切换到NativeDiagramControl（使用原生AIStudio.Wpf.DiagramDesigner库）'),
    ('/// �л���Ĭ�����ã�WorkflowCanvasControl���� + BezierPathCalculator·��������', '/// 切换到默认配置（WorkflowCanvasControl画布 + BezierPathCalculator路径计算器）'),
    ('// �л�������WorkflowCanvasControl���Զ��廭����', '// 切换到使用WorkflowCanvasControl（自定义画布）'),
    ('// ����·��������Ϊ Bezier�����������ߣ�', '// 设置路径计算器为 Bezier（贝塞尔曲线）'),
    ('// �����쳣', '// 忽略异常'),
    ('/// �л���WorkflowCanvasControl������ʹ�ñ��������ߣ�', '/// 切换到WorkflowCanvasControl画布（使用贝塞尔曲线）'),
    ('// ʹ�� CanvasEngineManager ����·��������Ϊ����������', '// 使用 CanvasEngineManager 设置路径计算器为贝塞尔曲线'),
    ('/// NativeDiagramControl Loaded�¼�����', '/// NativeDiagramControl Loaded事件处理'),
    ('// ���� NativeDiagramControl ����', '// 保存 NativeDiagramControl 引用'),
    ('// �ӳٸ���������ʾ�ȷ��DiagramViewModel�ѳ�ʼ��', '// 延迟更新确保显示正确（DiagramViewModel已初始化）'),
    ('/// ע���ݼ�', '/// 注册快捷键'),
    ('// �ļ�������ݼ�', '// 文件操作快捷键'),
    ('// ���п��ƿ�ݼ�', '// 运行控制快捷键'),
    ('// ������ݼ�', '// 帮助快捷键'),
    ('// �༭�ݼ�', '// 编辑快捷键'),
    
    # 区域标记
    ('#region �����¼�', '#region 窗口事件'),
    ('#region ��ʼ��', '#region 初始化'),
    ('#region TabControl �����̹����¼�����', '#region TabControl 相关事件和方法处理'),
    ('#region WorkflowCanvasControl �¼�����', '#region WorkflowCanvasControl 事件处理'),
    ('#region �Ϸ�¼�', '#region 拖放事件'),
    ('#region ���Ź���', '#region 缩放功能'),
    ('#region ��������', '#region 辅助方法'),
    ('#region SplitterWithToggle �¼�����', '#region SplitterWithToggle 事件处理'),
    ('#region �������������֧��', '#region 多画布类型切换支持'),
    ('#region �����װ����', '#region 命令包装类'),
    
    # 方法注释
    ('// TODO: ������Դ', '// TODO: 释放资源'),
    ('// ���߲������ͨ��ToolboxViewModel�Զ�����', '// 工具参数数量通过ToolboxViewModel自动更新'),
    ('// ��ʼ������·��ת�����Ľڵ㼯�ϣ�ʹ�õ�ǰѡ�е� Tab �Ľڵ㼯�ϣ�', '// 初始化智能路径转换器的节点集合，使用当前选中的 Tab 的节点集合'),
    ('// ��ʼ��������ʾ', '// 初始化缩放显示'),
    ('// ע�ͣ����´����ѷ�����������ָ�����ɾ����2026-02-10��', '// 注：以下代码已废弃，分割线相关代码已删除（2026-02-10）'),
    ('// TODO: ���ع�����', '// TODO: 重构工具箱'),
    ('"���ع��߲��ʱ����:', '"重构工具箱时出错:'),
    ('"����ʧ��"', '"初始化失败"'),
    ('/// ����������������', '/// 初始化画布容器装饰器'),
    ('// ��ʼ��������ʾ', '// 初始化缩放显示'),
    
    # TabControl相关
    ('/// WorkflowCanvasControl�����¼� - ��������', '/// WorkflowCanvasControl加载事件 - 画布初始化'),
    ('// ��� NativeDiagram ���棨��ǰ���ص��� WorkflowCanvas��', '// 清除 NativeDiagram 引用（当前隐藏的是 WorkflowCanvas）'),
    ('// ���DataContext', '// 获取DataContext'),
    ('// ���DataContextΪnull���ֶ�����Ϊ��ǰѡ�е�Tab', '// 若DataContext为null，则手动设置为当前选中的Tab'),
    ('// ����DataContextChanged�¼����Ա���CanvasType�仯ʱ����Visibility', '// 监听DataContextChanged事件，以便在CanvasType变化时更新Visibility'),
    ('// ��������CanvasType����Visibility', '// 根据当前CanvasType更新Visibility'),
    ('// �ӳٸ���������ʾ', '// 延迟更新缩放显示'),
    ('/// ����CanvasType���»�����Visibility', '/// 根据CanvasType更新画布Visibility'),
    ('// ��������������ScrollViewer', '// 获取当前选项卡的ScrollViewer'),
    ('// ����WorkflowCanvasControl�ĸ���ScrollViewer', '// 获取WorkflowCanvasControl的父级ScrollViewer'),
    ('// �����쳣', '// 捕获异常'),
    
    # 按钮和位置相关
    ('/// TabControl ������ɺ�,���ScrollViewer��ScrollableWidth�仯', '/// TabControl 加载完成后，监听ScrollViewer的ScrollableWidth变化'),
    ('// �ҵ�ScrollViewer', '// 找到ScrollViewer'),
    ('// ����ScrollViewer��SizeChanged�¼�', '// 监听ScrollViewer的SizeChanged事件'),
    ('// ��ʼ��� - ��Ҫ����TabControl���Ӿ�����Ԫ��������������ť', '// 初始化 - 需要等TabControl加载视觉树元素后才能定位按钮'),
    ('/// ScrollViewer��С�仯�¼� - �������Ӱ�ťλ��', '/// ScrollViewer大小变化事件 - 更新添加按钮位置'),
    ('// ��ScrollViewer�����ҵ�TabControl��Ȼ�����������ť', '// 从ScrollViewer向上找到TabControl，然后更新添加按钮'),
    ('/// ����ScrollableWidth�ж�TabItems�Ƿ񳬳�,��̬�������Ӱ�ťλ��', '/// 根据ScrollableWidth判断TabItems是否溢出，动态调整添加按钮位置'),
    ('// �ҵ�������ť��Border���� - ��TabControl���Ӿ����в���', '// 找到添加按钮的Border容器 - 在TabControl视觉树中查找'),
    ('// ScrollableWidth > 0 ��ʾ�й�����,��TabItems�����˿�������', '// ScrollableWidth > 0 表示有滚动条，即TabItems已超出了可视区域'),
    ('// ����ʱ:��ʾ�Ҳ�̶���ť,���ع��������ڵİ�ť', '// 溢出时：显示右侧固定按钮，隐藏滚动区域内的按钮'),
    ('// δ����ʱ:��ʾ���������ڵİ�ť(����TabItems),�����Ҳ�̶���ť', '// 未溢出时：显示滚动区域内的按钮(紧挨TabItems)，隐藏右侧固定按钮'),
    ('/// ���Ӿ�����ͨ��Name����ָ�����͵���Ԫ��', '/// 从视觉树中通过Name查找指定类型的元素'),
    
    # 选择变化事件
    ('/// TabControl ѡ�仯�¼� - �����л���ʽ�����Ƿ����', '/// TabControl 选中变化事件 - 处理切换动画和是否居中'),
    ('// ��ȡѡ�е�Tab', '// 获取选中的Tab'),
    ('// �Ż�������WorkflowCanvasControl��DataContext��ObservableCollection���Զ�֪ͨUI���£�', '// 优化：更新WorkflowCanvasControl的DataContext（ObservableCollection会自动通知UI更新）'),
    ('// �Ż����ϲ�Dispatcher���ã�����UI�ػ����', '// 优化：合并多次Dispatcher调用，减少UI重绘次数'),
    ('// ֻ��ͨ���������л�ʱ�Ź������м䣬���TabItemʱ������', '// 只在通过滚动条切换时才滚动到中间，点击TabItem时跳过'),
    ('// �������Ӱ�ťλ��', '// 更新添加按钮位置'),
    ('// ���ñ�־', '// 重置标志'),
    ('// Ӧ������', '// 应用缩放'),
    ('// ����������ʾ', '// 更新缩放显示'),
    
    # 预览鼠标事件
    ('/// TabControl Ԥ�������������¼� - ����Ƿ�����TabItem', '/// TabControl 预览鼠标左键按下事件 - 判断是否点击TabItem'),
    ('// ��������Ƿ���TabItem', '// 判断点击源是否是TabItem'),
    ('// ���ΪTabItem���', '// 标记为TabItem点击'),
    
    # 添加工作流
    ('/// ���ӹ���������¼�', '/// 添加工作流按钮点击事件'),
    ('"�������¹�����"', '"创建了新工作流"'),
    ('// �Զ������������ӵ�TabItem��ʹ�������ʾ', '// 自动滚动到新添加的TabItem，使其显示'),
    ('// �ȴ� Canvas ������ɺ�Ӧ�ó�ʼ����', '// 等待 Canvas 加载完成后应用初始缩放'),
    
    # 滚动到选中的TabItem
    ('/// ������ѡ�е�TabItem��ʹ����ʾ�ڿɼ���Χ���м�', '/// 滚动到选中的TabItem，使其显示在可见范围中间'),
    ('// ȡTabItem', '// 获取TabItem'),
    ('// ȡTabPanel作为参考', '// 获取TabPanel作为参考'),
    ('// 计算TabItem在TabPanel中的位置（相对于容器的绝对位置）', '// 计算TabItem在TabPanel中的位置（相对于容器的绝对位置）'),
    ('// 计算使TabItem居中的滚动位置', '// 计算使TabItem居中的滚动位置'),
    ('// TabItem中心位置 = position.X + selectedTabItem.ActualWidth / 2', '// TabItem中心位置 = position.X + selectedTabItem.ActualWidth / 2'),
    ('// 视口中心位置 = scrollViewer.ViewportWidth / 2', '// 视口中心位置 = scrollViewer.ViewportWidth / 2'),
    ('// 目标滚动位置 = TabItem中心位置 - 视口中心位置', '// 目标滚动位置 = TabItem中心位置 - 视口中心位置'),
    ('// 确保滚动位置在有效范围内', '// 确保滚动位置在有效范围内'),
    ('// 滚动到目标位置，使TabItem居中显示', '// 滚动到目标位置，使TabItem居中显示'),
    
    # 辅助方法
    ('/// ���Ӿ����в���ָ�����͵ĵ�һ����Ԫ��', '/// 从视觉树中查找指定类型的第一个元素'),
    ('/// ��ȡ��ǰ��ʾ��WorkflowCanvasControl', '/// 获取当前显示的WorkflowCanvasControl'),
    ('/// ���Ӿ����в���ָ�����ݶ�Ӧ��TabItem', '/// 从视觉树中查找指定数据对象对应的TabItem'),
    ('// ��TabControl��Items�в���TabItem', '// 在TabControl的Items中查找TabItem'),
    ('// ͨ������TabControl���Ӿ����ҵ�����TabItem', '// 通过遍历TabControl视觉树找到所有TabItem'),
    ('/// ���Ӿ����в���ָ�����͵�������Ԫ��', '/// 从视觉树中查找指定类型的所有元素'),
    ('/// ���Ӿ����в���ָ�����͵�������Ԫ�أ�����IEnumerable�ı�ݷ����', '/// 从视觉树中查找指定类型的所有元素（IEnumerable递归版本）'),
    ('/// ���Ӿ����в���ָ�����͵ĸ�Ԫ��', '/// 从视觉树中查找指定类型的父元素'),
    ('/// ���Ӿ����в���ָ�����͵�������Ԫ��', '/// 从视觉树中查找指定类型的所有子元素'),
    
    # TabItem 菜单事件
    ('/// TabItem �������е���¼�', '/// TabItem 单次运行按钮点击事件'),
    ('// ����ѡ�еĹ�����', '// 设置选中的工作流'),
    ('// ���� RunWorkflowCommand �� Execute ����', '// 调用 RunWorkflowCommand 的 Execute 方法'),
    ('// RunWorkflowCommand ���첽���Execute �����������첽����', '// RunWorkflowCommand 是异步命令，Execute 方法会自动处理异步执行'),
    ('"��������:', '"正在运行:'),
    ('/// TabItem ��������/ֹͣ����¼�', '/// TabItem 连续运行/停止按钮点击事件'),
    ('"��ʼ��������"', '"开始连续运行"'),
    ('"ֹͣ"', '"停止"'),
    ('/// TabItem ɾ�����¼�', '/// TabItem 删除按钮点击事件'),
    ('"����ֹͣ�ù�����"', '"请先停止该工作流"'),
    ('"��ʾ"', '"提示"'),
    ('"ȷ��Ҫɾ��������', '"确定要删除工作流'),
    ('"ȷ��ɾ��"', '"确认删除"'),
    ('"�ѱ�ɾ��������:', '"已删除工作流:'),
    ('"������Ҫ����һ��������"', '"至少需要保留一个工作流"'),
    
    # 工作流执行请求
    ('/// ������ִ�������¼�����', '/// 工作流执行请求事件处理'),
    ('"? û��ѡ�еĹ��������޷�ִ��', '": 没有选中的工作流，无法执行'),
    ('"? ִ�й����� - ͼ��:', '": 执行工作流 - 图像:'),
    ('// ���õ�ǰͼ������', '// 设置当前图像索引'),
    ('// ����������ִ��', '// 触发工作流执行'),
    ('"���й�����:', '"运行工作流:'),
    
    # WorkflowCanvasControl 事件
    ('/// �ڵ������¼�����', '/// 节点添加事件处理'),
    ('"��½ڵ�:', '"添加节点:'),
    ('/// �ڵ�ѡ���¼�����', '/// 节点选中事件处理'),
    ('// ͨ�����Է��ʣ��������Ա�֪ͨ�ͺ����߼�', '// 通过属性访问，简化成员通知和业务逻辑'),
    ('/// �ڵ�˫���¼�����', '/// 节点双击事件处理'),
    
    # 拖放事件
    ('// ��ѡ:�����뿪����ʱ���Ӿ�Ч��', '// 可选：当拖放离开画布时添加视觉效果'),
    ('// �����½ڵ㣬ʹ��ToolId��ΪAlgorithmType', '// 创建新节点，使用ToolId作为AlgorithmType'),
    ('// ʹ��ToolId������AlgorithmType', '// 使用ToolId作为AlgorithmType'),
    ('// �����Ϸ�λ��(���з���,�ڵ��С140x90)', '// 调整拖放位置(居中放置,节点大小140x90)'),
    ('// ʹ������ģʽ���ӽڵ�', '// 使用批量模式添加节点'),
    ('"���ӽڵ�ʱ����:', '"添加节点时出错:'),
    ('"����"', '"错误"'),
    
    # 缩放功能
    ('/// ��ȡ��ǰ���NativeDiagramControl', '/// 获取当前活动的NativeDiagramControl'),
    ('// ֻ�е�ǰ����������NativeDiagramʱ�ŷ���', '// 只有当前画布是NativeDiagram时才返回'),
    ('// ֱ�ӷ��ػ�������ã�ͨ�� NativeDiagramControl_Loaded �¼����棩', '// 直接返回缓存的引用（通过 NativeDiagramControl_Loaded 事件缓存）'),
    ('/// ��ȡNativeDiagramControl��DiagramViewModel', '/// 获取NativeDiagramControl的DiagramViewModel'),
    ('// ʹ�ù����� GetDiagramViewModel ����', '// 使用公共方法 GetDiagramViewModel 获取'),
    ('/// NativeDiagramControl�ķŴ�', '/// NativeDiagramControl的放大'),
    ('/// NativeDiagramControl����С', '/// NativeDiagramControl的缩小'),
    ('/// NativeDiagramControl������', '/// NativeDiagramControl的重置'),
    ('/// NativeDiagramControl����Ӧ����', '/// NativeDiagramControl的适应窗口'),
    ('// Ĭ�ϻ��С10000x10000', '// 默认画布大小10000x10000'),
    
    # 缩放按钮事件
    ('/// ��Ŵ����', '/// 放大画布'),
    ('// ԭ�е�WorkflowCanvas�����߼�', '// 原有的WorkflowCanvas缩放逻辑'),
    ('/// ��С����', '/// 缩小画布'),
    ('/// ��Ӧ����', '/// 适应窗口'),
    ('// �ӳ�ִ����ȷ�� UI �Ѹ���', '// 延迟执行确保 UI 已更新'),
    ('// �����ʺϵ����ű���������10%�߾�', '// 计算适合的缩放比例（保留10%边距）'),
    ('// �����ڷ�Χ��', '// 限制在范围内'),
    ('/// ��������Ϊ100%', '/// 重置缩放为100%'),
    ('/// �л����������߻��� (WorkflowCanvas)', '/// 切换到自定义画布引擎 (WorkflowCanvas)'),
    ('/// �л������������߻��� (NativeDiagram)', '/// 切换到原生画布引擎 (NativeDiagram)'),
    
    # ApplyZoom方法
    ('/// Ӧ�����ű任务任务�֧��Χ�ָ��λ�����ţ�', '/// 应用缩放比例（支持范围指定位置的缩放）'),
    ('/// <param name="oldScale">����ǰ������ֵ</param>', '/// <param name="oldScale">旧缩放比例值</param>'),
    ('/// <param name="newScale">���ź������ֵ</param>', '/// <param name="newScale">新缩放比例值</param>'),
    ('/// <param name="centerPosition">�������������ScrollViewer�����꣨��ѡ��</param>', '/// <param name="centerPosition">缩放中心点在ScrollViewer中的坐标（可选）</param>'),
    ('/// <param name="scrollViewer">���õ�ScrollViewerʵ������ѡ������ṩ����Ҫ���µ���ң�</param>', '/// <param name="scrollViewer">要用的ScrollViewer实例（可选，若提供则不需要重新查找）</param>'),
    ('// ����CurrentScale', '// 更新CurrentScale'),
    ('// ���û����ṩScrollViewer�����Բ���', '// 若没有提供ScrollViewer则去查找'),
    ('// ����ṩ��������������ScrollViewer�����㲢��������ƫ��', '// 若提供了缩放中心点且有ScrollViewer，则计算并更新滚动偏移'),
    ('// ��������ǰ�ı����仯', '// 计算缩放前后比例的变化'),
    ('// �������ֵû�б仯��ֱ�ӷ���', '// 若缩放值没有变化，则直接返回'),
    ('// ��ȡ��ǰ����ƫ��', '// 获取当前滚动偏移'),
    ('// �������������ڻ�������ϵ�е�λ�ã����ǵ�ǰ���ţ�', '// 计算中心点在画布坐标系中的位置（不受当前缩放影响）'),
    ('// Ӧ���µ�����ֵ����ʹ��CenterX/CenterY����Ϊ�����ڵ�������ƫ�ƣ�', '// 应用新的缩放值（使用CenterX/CenterY作为相对于画布左上角的偏移）'),
    ('// �����µĹ���ƫ�ƣ�������������ָ�������λ�ò���', '// 计算新的滚动偏移，使画布中心点保持在指定的屏幕位置不变'),
    ('// �µĹ���ƫ�� = ���������ڻ������� * �����ű��� - ����������ScrollViewerλ��', '// 新的滚动偏移 = 中心点在画布上坐标 * 新缩放比例 - 中心点在ScrollViewer中位置'),
    ('// Ӧ���µĹ���ƫ��', '// 应用新的滚动偏移'),
    ('// û���������Ļ�û��ScrollViewerʱ��ֱ��Ӧ�����ţ����ڳ�ʼ���ػ����ã�', '// 没有缩放中心点或没有ScrollViewer时，直接应用缩放（用于初始化或重置）'),
    ('// ������ʾ', '// 更新缩放显示'),
    ('"��������:', '"当前缩放:'),
    
    # 缩放指示器
    ('/// ��������ָʾ��', '/// 更新缩放指示器'),
    ('// �ڵ�ǰTab�в�������֌ʾ��', '// 在当前Tab中查找缩放指示器'),
    ('// �������� TextBlock Ԫ��', '// 查找所有 TextBlock 元素'),
    ('/// �������Űٷֱ���ʾ', '/// 更新缩放百分比显示'),
    ('// ʹ�� Dispatcher �ӳ�ִ�У�ȷ�� TabItem ��������ȫ����', '// 使用 Dispatcher 延迟执行，确保 TabItem 视觉树已完全生成'),
    ('// ֱ�ʹ��������ZoomTextBlock�ؼ�', '// 直接使用后台的ZoomTextBlock控件'),
    
    # 鼠标滚轮缩放
    ('/// �����������¼�', '/// 画布滚动鼠标滚轮事件'),
    ('// ֱ�ʹ�ù�ֽ�������ţ���Ҫ��Ctrl����', '// 直接使用滚轮进行缩放，不需要Ctrl键'),
    ('// sender ���� ScrollViewer', '// sender 应该是 ScrollViewer'),
    ('// ��ȡ���λ��', '// 获取鼠标位置'),
    ('// ���Ϲ������Ŵ�', '// 向上滚动滚轮，放大'),
    ('// ���λ����Ϊ��������', '// 鼠标位置作为缩放中心'),
    ('// ���¹�������С', '// 向下滚动滚轮，缩小'),
    
    # 获取Canvas和ScrollViewer
    ('/// ��ȡ��ǰ���Canvas', '/// 获取当前活动的Canvas'),
    ('// ������ TabItem �в������� Canvas', '// 在当前 TabItem 中查找所有 Canvas'),
    ('// �ҵ���Ϊ WorkflowCanvas �� Canvas', '// 找到名为 WorkflowCanvas 的 Canvas'),
    ('// ���û���ҵ���Ϊ WorkflowCanvas ��,���ص�һ�� Canvas', '// 若没找到名为 WorkflowCanvas 的，返回第一个 Canvas'),
    ('// �Ĭ�����쳣', '// 捕获默认异常'),
    ('/// ��ȡ��ǰ���ScrollViewer', '/// 获取当前活动的ScrollViewer'),
    ('// TabControl������ͨ��ContentPresenter��ʾ�ģ����,��������TabItem���Ӿ�����', '// TabControl的内容是通过ContentPresenter显示的，不一定是TabItem视觉树'),
    ('// ����ֱ�Ӵ�WorkflowTabControl���Ӿ����в���ScrollViewer', '// 所以直接从WorkflowTabControl视觉树中查找ScrollViewer'),
    ('// ������Ϊ CanvasScrollViewer ��', '// 查找名为 CanvasScrollViewer 的'),
    ('// ����Ҳ���ָ�����Ƶ�,���ص�һ��', '// 若找不到指定名称的，返回第一个'),
    ('// ע�⣺�˴�޷����� _viewModel����Ϊ˷����Ǹ�������', '// 注意：此处无法使用 _viewModel，因为此方法是非公开的'),
    ('// �����¼�����־�����Դ��� ViewModel �����ʹ��������־����', '// 若需要记录日志，请从 ViewModel 调用或使用其他日志机制'),
    ('/// ��ȡ����������Canvas�ϵ�����', '/// 获取鼠标光标在Canvas上的位置'),
    ('// �����ӿ����������ScrollViewer�����꣨��������ӿ����ĵ�λ�ã�', '// 返回视口中心在ScrollViewer中的坐标（即视口中心的局部位置）'),
    
    # 辅助方法
    ('/// ���Ҹ���Canvas', '/// 查找父级Canvas'),
    
    # Splitter事件
    ('// ע�ͣ����´����ѷ�����������˫ģ�л���������ȫ��ToolboxControl�ڲ���ťʵ�֣�2026-02-10��', '// 注：以下代码已废弃，双模式切换功能已全部由ToolboxControl内部按钮实现（2026-02-10）'),
    ('/// ͼ��-���Էָ�����ʼ�϶�', '/// 图像-属性分割线开始拖动'),
    ('// ��¼�϶���ʼǰ��״̬', '// 记录拖动开始前的状态'),
    ('"[�ָ����϶�] ��ʼ�϶�����ǰλ��:', '"[分割线拖动] 开始拖动，当前位置:'),
    ('/// 图像-属性分割线拖动中 - 实时更新高度', '/// 图像-属性分割线拖动中 - 实时更新高度'),
    ('// 获取当前图像显示区的高度', '// 获取当前图像显示区的高度'),
    ('// 实时记录拖动过程中的位置变化（用于调试）', '// 实时记录拖动过程中的位置变化（用于调试）'),
    ('"[�ָ����϶���] ��ǰλ��:', '"[分割线拖动中] 当前位置:'),
    ('// 注意：由于ShowsPreview="False"，GridSplitter会自动更新相关Row的高度', '// 注意：由于ShowsPreview="False"，GridSplitter会自动更新相关Row的高度'),
    ('// 不需要手动设置Height，只需要记录状态即可', '// 不需要手动设置Height，只需要记录状态即可'),
    ('/// 图像-属性分割线拖动完成', '/// 图像-属性分割线拖动完成'),
    ('// 保存新的分割线位置到ViewModel', '// 保存新的分割线位置到ViewModel'),
    ('"[�ָ����϶�] ����϶�����λ��:', '"[分割线拖动] 结束拖动，位置:'),
    ('/// 属性面板分割线折叠/展开事件（已废弃 - 切换功能由ToolboxControl内部按钮实现）', '/// 属性面板分割线折叠/展开事件（已废弃 - 切换功能由ToolboxControl内部按钮实现）'),
    ('// 展开：切换到展开模式（260px）', '// 展开：切换到展开模式（260px）'),
    ('// 折叠：切换到折叠模式（60px）', '// 折叠：切换到折叠模式（60px）'),
    ('/// 以下功能已废弃 - 分割线已删除', '/// 以下功能已废弃 - 分割线已删除'),
    ('/// �Ҳ����ָ������۵�/展���¼�', '/// 右侧面板分割线折叠/展开事件'),
    ('// չ�������Ҳ����', '// 展开：展开右侧面板'),
    ('// �۵������Ҳ����', '// 折叠：收起右侧面板'),
    ('/// �����Ҳ����ָ�����ͷ�����', '/// 更新右侧面板分割线箭头方向'),
    
    # 多画布类型切换支持
    ('/// ͨ�������л��������ͣ����ڵ�Ԫ���Ժ����ⳡ����', '/// 通过属性切换画布类型，便于单元测试和外部调用'),
    ('"����ѡ��һ���������ǩҳ"', '"请选择一个工作流标签页"'),
    ('/// ����·�����������ͣ����ڵ�Ԫ���ԣ�', '/// 设置路径计算器类型，便于单元测试和调试'),
    ('// ��ȡ��ǰ��������', '// 获取当前画布类型'),
    ('// ���ݻ�����͵��ö�Ӧ�Ŀؼ�����', '// 根据画布类型调用对应的控件方法'),
    ('// ���ҵ�ǰ��ʾ��WorkflowCanvasControl', '// 若找到当前显示的WorkflowCanvasControl'),
    ('// ���ҵ�ǰ��ʾ��NativeDiagramControl', '// 若找到当前显示的NativeDiagramControl'),
    ('// ͬʱ����CanvasEngineManager�����ּ�����', '// 同时更新CanvasEngineManager中的记录'),
    ('/// ��ȡ��ǰ��������', '/// 获取当前画布类型'),
    ('/// ��ȡ��ǰ��������', '/// 获取当前画布引擎'),
    
    # 状态文本中的中文
    ('"�Ѽ���', '"已加载'),
    ('"�½ڵ�:': '"添加节点:'),
    ('"��������:': '"正在运行:'),
    ('"����:': '"缩放:'),
    ('"��ɾ��������:': '"已删除工作流:'),
    ('"�����¹�����"', '"创建了新工作流"'),
    ('"ֹͣ:': '"停止:'),
]

def fix_file(filepath):
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
    except:
        with open(filepath, 'r', encoding='utf-8', errors='replace') as f:
            content = f.read()
    
    fixed_count = 0
    for broken, correct in FIXES:
        if broken in content:
            content = content.replace(broken, correct)
            fixed_count += 1
            print(f"  修复: {broken[:30]}... -> {correct[:30]}...")
    
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)
    
    return fixed_count

if __name__ == '__main__':
    filepath = 'src/UI/Views/Windows/MainWindow.xaml.cs'
    count = fix_file(filepath)
    print(f"\n总计修复 {count} 处编码问题")
