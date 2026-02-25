#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
MainWindow.xaml.cs 编码修复脚本
基于代码上下文推断原始注释
"""

import re

# 基于代码上下文的注释修复映射
COMMENT_FIXES = {
    # 类和主窗口相关
    'MainWindow - ̫': 'MainWindow - 太阳视觉主窗口',
    'ʵ��������Ļ����Ӿ�ƽ̨�����棬���������������������䡢��������': '实现了机器视觉平台的核心，包含工具箱、画布、属性面板等模块',
    
    # 字段注释
    '// ����Ƿ���ͨ�����TabItem�������л�': '// 标识是否通过点击TabItem触发的切换',
    '// ��ǰ��ʾ��WorkflowCanvasControl': '// 当前显示的WorkflowCanvasControl',
    '// ��ǰ��ʾ��NativeDiagramControl': '// 当前显示的NativeDiagramControl',
    '// �����������������': '// 画布容器装饰器',
    '// �������': '// 缩放范围',
    '// ���������С': '// 画布虚拟大小',
    
    # 方法注释
    '/// ����־���ӵ�UI����': '/// 将日志添加到UI界面',
    '// ����־���ӵ� LogText��������ڣ�': '// 将日志添加到 LogText（如果存在）',
    '// ֻ�������100��': '// 只保留最新100行',
    '// ��ʼ��������������� - ����Ĭ������': '// 初始化画布引擎管理器 - 设置默认配置',
    '// ��̨�л���NativeDiagramControl��ʹ��ԭ��AIStudio.Wpf.DiagramDesigner�⣩': '// 后台切换到NativeDiagramControl（使用原生AIStudio.Wpf.DiagramDesigner库）',
    '/// �л���Ĭ�����ã�WorkflowCanvasControl���� + BezierPathCalculator·��������': '/// 切换到默认配置（WorkflowCanvasControl画布 + BezierPathCalculator路径计算器）',
    '// �л�������WorkflowCanvasControl���Զ��廭����': '// 切换到使用WorkflowCanvasControl（自定义画布）',
    '// ����·��������Ϊ Bezier�����������ߣ�': '// 设置路径计算器为 Bezier（贝塞尔曲线）',
    '// �����쳣': '// 忽略异常',
    
    # 缩放相关
    '/// ��Ŵ����': '/// 放大画布',
    '/// ��С����': '/// 缩小画布',
    '/// ��Ӧ����': '/// 适应窗口',
    '/// ��������Ϊ100%': '/// 重置缩放为100%',
    '/// ��ȡ��ǰ���Canvas': '/// 获取当前活动的Canvas',
    '/// ��ȡ��ǰ���ScrollViewer': '/// 获取当前活动的ScrollViewer',
    '/// ��ȡ����������Canvas�ϵ�����': '/// 获取鼠标光标在Canvas上的位置',
    
    # TabControl相关
    '/// TabControl ѡ�仯�¼� - �����л���ʽ�����Ƿ����': '/// TabControl 选中变化事件 - 处理切换动画和是否居中',
    '// ��ȡѡ�е�Tab': '// 获取选中的Tab',
    '// �Ż�������WorkflowCanvasControl��DataContext��ObservableCollection���Զ�֪ͨUI���£�': '// 优化：更新WorkflowCanvasControl的DataContext（ObservableCollection会自动通知UI更新）',
    '// �Ż����ϲ�Dispatcher���ã�����UI�ػ����': '// 优化：合并多次Dispatcher调用，减少UI重绘次数',
    
    # 拖放相关
    '/// �Ϸ�¼�': '/// 拖放事件',
    '// �����½ڵ㣬ʹ��ToolId��ΪAlgorithmType': '// 创建新节点，使用ToolId作为AlgorithmType',
    '// �����Ϸ�λ��(���з���,�ڵ��С140x90)': '// 调整拖放位置(居中放置,节点大小140x90)',
    '// ʹ������ģʽ���ӽڵ�': '// 使用批量模式添加节点',
    
    # 区域标记
    '#region �����¼�': '#region 窗口事件',
    '#region ��ʼ��': '#region 初始化',
    '#region ���Ź���': '#region 缩放功能',
    '#region ��������': '#region 辅助方法',
    '#region �Ϸ�¼�': '#region 拖放事件',
    
    # 状态文本
    '"�Ѽ���': '"已加载',
    '"��½ڵ�:': '"添加节点:',
    '"��������:': '"正在运行:',
    '"�ѱ�ɾ��������:': '"已删除工作流:',
}

def fix_mainwindow():
    filepath = 'src/UI/Views/Windows/MainWindow.xaml.cs'
    
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
    except:
        with open(filepath, 'r', encoding='utf-8', errors='replace') as f:
            content = f.read()
    
    fixed_count = 0
    for broken, correct in COMMENT_FIXES.items():
        if broken in content:
            content = content.replace(broken, correct)
            fixed_count += 1
    
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)
    
    print(f"MainWindow.xaml.cs: 修复了 {fixed_count} 处编码问题")

if __name__ == '__main__':
    fix_mainwindow()
