#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
SunEyeVision 编码问题批量修复脚本

使用方法:
1. 根据实际情况修改 ENCODING_FIXES 字典
2. 运行脚本: python fix_encoding_v2.py
3. 检查修复结果，必要时手动调整

注意: 运行前请确保已备份代码！
"""

import os
import re
import sys
from pathlib import Path
from typing import Dict, List, Tuple

# ============================================================================
# 常见编码错误修复映射 - 根据实际情况修改此字典
# ============================================================================
ENCODING_FIXES = {
    # 常见双字截断模式 (字 + ??)
    '调??': '调试',
    '信??': '信息',
    '接??': '接口',
    '结??': '结果',
    '问??': '问题',
    '配??': '配置',
    '文??': '文件',
    '数??': '数据',
    '时??': '时间',
    '次??': '次数',
    '比??': '比例',
    '态??': '状态',
    '置??': '设置',
    '项??': '项目',
    '类??': '类型',
    '方??': '方法',
    '功??': '功能',
    '属??': '属性',
    '参??': '参数',
    '变??': '变量',
    '表??': '表',
    '列??': '列表',
    '集??': '集合',
    '对??': '对象',
    '实??': '实例',
    '模??': '模块',
    '组??': '组件',
    '系??': '系统',
    '程??': '程序',
    '流??': '流程',
    '器??': '器',
    '场??': '场景',
    '操??': '操作',
    '执??': '执行',
    '运??': '运行',
    '停??': '停止',
    '启??': '启动',
    '载??': '加载',
    '存??': '保存',
    '删??': '删除',
    '添??': '添加',
    '修??': '修改',
    '更??': '更新',
    '创??': '创建',
    '初??': '初始',
    '清??': '清理',
    '释??': '释放',
    '销??': '销毁',
    '析??': '析构',
    '复??': '复制',
    '粘??': '粘贴',
    '剪??': '剪切',
    '撤??': '撤销',
    '重??': '重做',
    '恢??': '恢复',
    '回??': '回退',
    '进??': '进度',
    '错??': '错误',
    '异??': '异常',
    '警??': '警告',
    '提??': '提示',
    '消??': '消息',
    '日??': '日志',
    '记??': '记录',
    '跟??': '跟踪',
    '监??': '监控',
    '测??': '测试',
    '验??': '验证',
    '检??': '检查',
    '校??': '校验',
    '确??': '确认',
    '取??': '取消',
    '拒??': '拒绝',
    '允??': '允许',
    '禁??': '禁止',
    '限??': '限制',
    '过??': '过滤',
    '筛??': '筛选',
    '排??': '排序',
    '分??': '分组',
    '聚??': '聚合',
    '统??': '统计',
    '计??': '计算',
    '处??': '处理',
    '解??': '解析',
    '转??': '转换',
    '格??': '格式',
    '编??': '编码',
    '压??': '压缩',
    '加??': '加密',
    '签??': '签名',
    '授??': '授权',
    '认??': '认证',
    '登??': '登录',
    '注??': '注册',
    '退??': '退出',
    '连??': '连接',
    '断??': '断开',
    '发??': '发送',
    '接??': '接收',
    '请??': '请求',
    '响??': '响应',
    '服??': '服务',
    '客??': '客户端',
    '端??': '端口',
    '地??': '地址',
    '路??': '路径',
    '目??': '目标',
    '源??': '源',
    '目??': '目录',
    '根??': '根',
    '父??': '父',
    '子??': '子',
    '兄??': '兄弟',
    '节??': '节点',
    '元??': '元素',
    '项??': '项',
    '条??': '条件',
    '规??': '规则',
    '策??': '策略',
    '算??': '算法',
    '模??': '模型',
    '视??': '视图',
    '控??': '控件',
    '窗??': '窗口',
    '对??': '对话框',
    '菜??': '菜单',
    '工??': '工具',
    '状??': '状态栏',
    '导??': '导航',
    '布??': '布局',
    '样??': '样式',
    '主??': '主题',
    '资??': '资源',
    '图??': '图像',
    '视??': '视频',
    '音??': '音频',
    '动??': '动画',
    '效??': '效果',
    '过??': '过渡',
    '事??': '事件',
    '命??': '命令',
    '绑??': '绑定',
    '触??': '触发',
    '响??': '响应',
    '处??': '处理器',
    '回??': '回调',
    '委??': '委托',
    '任??': '任务',
    '线??': '线程',
    '同??': '同步',
    '异??': '异步',
    '并??': '并行',
    '串??': '串行',
    '队??': '队列',
    '栈??': '栈',
    '堆??': '堆',
    '缓??': '缓存',
    '池??': '池',
    '资??': '资源',
    '内??': '内存',
    '磁??': '磁盘',
    '网??': '网络',
    '数??': '数据库',
    '表??': '表',
    '字??': '字段',
    '记??': '记录',
    '索??': '索引',
    '键??': '键',
    '值??': '值',
    '空??': '空',
    '非??': '非空',
    '主??': '主键',
    '外??': '外键',
    '唯??': '唯一',
    '默??': '默认',
    '自??': '自动',
    '手??': '手动',
    '全??': '全局',
    '局??': '局部',
    '公??': '公共',
    '私??': '私有',
    '保??': '保护',
    '内??': '内部',
    '外??': '外部',
    '静??': '静态',
    '动??': '动态',
    '常??': '常量',
    '变??': '变量',
    '只??': '只读',
    '可??': '可写',
    '可??': '可选',
    '必??': '必需',
    '输??': '输入',
    '输??': '输出',
    '返??': '返回',
    '参??': '参数',
    '泛??': '泛型',
    '继??': '继承',
    '实??': '实现',
    '重??': '重写',
    '覆??': '覆盖',
    '隐??': '隐藏',
    '封??': '封装',
    '抽??': '抽象',
    '虚??': '虚',
    '接??': '接口',
    '类??': '类',
    '结??': '结构',
    '枚??': '枚举',
    '委??': '委托',
    '事??': '事件',
    '属??': '属性',
    '索??': '索引器',
    '运??': '运算符',
    '转??': '转换',
    '构??': '构造',
    '析??': '析构',
    '静??': '静态',
}

# 更复杂的上下文修复模式
CONTEXT_FIXES = [
    # (错误模式, 正确文本, 描述)
    (r'用于开发调\?\?', '用于开发调试', '调试'),
    (r'一般信\?\?', '一般信息', '信息'),
    (r'配置管理器接\?\?', '配置管理器接口', '接口'),
    (r'工作流执行结\?\?', '工作流执行结果', '结果'),
    (r'是否被停\?\?', '是否被停止', '停止'),
    (r'进度百分\?\?', '进度百分比', '百分比'),
    (r'总迭代次\?\?', '总迭代次数', '次数'),
    (r'执行开始时\?\?', '执行开始时间', '时间'),
    (r'中文乱码问\?\?', '中文乱码问题', '问题'),
    (r'绑定信\?\?', '绑定信息', '信息'),
    (r'配置\?\?</param>', '配置键</param>', '配置键'),
    (r'所有配\?\?</returns>', '所有配置</returns>', '所有配置'),
]

def find_cs_files(root_dir: str) -> List[Path]:
    """查找所有C#源文件"""
    cs_files = []
    for root, dirs, files in os.walk(root_dir):
        # 排除obj和bin目录
        dirs[:] = [d for d in dirs if d not in ['obj', 'bin', '.git']]
        for file in files:
            if file.endswith('.cs'):
                cs_files.append(Path(root) / file)
    return cs_files

def has_encoding_issues(content: str) -> bool:
    """检查内容是否有编码问题"""
    return '\ufffd' in content

def fix_encoding(content: str) -> Tuple[str, int]:
    """修复编码问题，返回修复后的内容和修复数量"""
    fix_count = 0
    
    # 应用简单替换
    for wrong, correct in ENCODING_FIXES.items():
        if wrong in content:
            count = content.count(wrong)
            content = content.replace(wrong, correct)
            fix_count += count
    
    # 应用上下文修复
    for pattern, correct, desc in CONTEXT_FIXES:
        matches = re.findall(pattern, content)
        if matches:
            content = re.sub(pattern, correct, content)
            fix_count += len(matches)
    
    return content, fix_count

def process_file(file_path: Path, dry_run: bool = True) -> Tuple[bool, int]:
    """处理单个文件"""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        if not has_encoding_issues(content):
            return False, 0
        
        fixed_content, fix_count = fix_encoding(content)
        
        if fix_count > 0 and not dry_run:
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(fixed_content)
        
        return True, fix_count
    except Exception as e:
        print(f"  错误处理文件 {file_path}: {e}")
        return False, 0

def main():
    print("=" * 60)
    print("SunEyeVision 编码问题批量修复脚本")
    print("=" * 60)
    
    # 获取src目录
    src_dir = Path(__file__).parent / 'src'
    if not src_dir.exists():
        print(f"错误: 找不到src目录: {src_dir}")
        return
    
    # 查找所有C#文件
    print(f"\n扫描目录: {src_dir}")
    cs_files = find_cs_files(src_dir)
    print(f"找到 {len(cs_files)} 个C#文件")
    
    # 处理文件
    print("\n分析编码问题...")
    problem_files = []
    total_fixes = 0
    
    for file_path in cs_files:
        has_issues, fix_count = process_file(file_path, dry_run=True)
        if has_issues:
            problem_files.append((file_path, fix_count))
            total_fixes += fix_count
    
    print(f"\n发现 {len(problem_files)} 个文件存在编码问题")
    print(f"总计可修复 {total_fixes} 处问题")
    
    if not problem_files:
        print("\n没有发现编码问题!")
        return
    
    # 显示问题文件列表
    print("\n问题文件列表 (按问题数量排序):")
    print("-" * 60)
    problem_files.sort(key=lambda x: x[1], reverse=True)
    for file_path, count in problem_files[:20]:
        rel_path = file_path.relative_to(src_dir.parent)
        print(f"  {rel_path}: {count} 处")
    
    if len(problem_files) > 20:
        print(f"  ... 还有 {len(problem_files) - 20} 个文件")
    
    # 询问是否执行修复
    print("\n" + "=" * 60)
    print("注意: 运行修复前请确保已备份代码!")
    print("=" * 60)
    
    response = input("\n是否执行修复? (y/n): ").strip().lower()
    if response != 'y':
        print("已取消修复")
        return
    
    # 执行修复
    print("\n开始修复...")
    fixed_count = 0
    for file_path, _ in problem_files:
        has_issues, fix_count = process_file(file_path, dry_run=False)
        if has_issues and fix_count > 0:
            fixed_count += 1
            rel_path = file_path.relative_to(src_dir.parent)
            print(f"  已修复: {rel_path} ({fix_count} 处)")
    
    print(f"\n修复完成! 共修复 {fixed_count} 个文件")

if __name__ == '__main__':
    main()
