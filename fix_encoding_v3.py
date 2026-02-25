#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
SunEyeVision 编码问题批量修复脚本 v3
支持自动检测文件编码（UTF-8, GBK, GB2312等）
"""

import os
import re
import sys
from pathlib import Path
from typing import Dict, List, Tuple, Optional

def detect_encoding(file_path: Path) -> Optional[str]:
    """检测文件编码"""
    # 尝试的编码列表
    encodings = ['utf-8-sig', 'utf-8', 'gbk', 'gb2312', 'gb18030', 'utf-16', 'utf-16-le', 'utf-16-be']
    
    with open(file_path, 'rb') as f:
        raw = f.read()
    
    # 检查BOM
    if raw.startswith(b'\xef\xbb\xbf'):
        return 'utf-8-sig'
    elif raw.startswith(b'\xff\xfe'):
        return 'utf-16-le'
    elif raw.startswith(b'\xfe\xff'):
        return 'utf-16-be'
    
    # 尝试各种编码
    for encoding in encodings:
        try:
            raw.decode(encoding)
            return encoding
        except (UnicodeDecodeError, LookupError):
            continue
    
    return None

def read_file_with_encoding(file_path: Path) -> Tuple[Optional[str], Optional[str]]:
    """使用正确的编码读取文件"""
    encoding = detect_encoding(file_path)
    if encoding:
        try:
            with open(file_path, 'r', encoding=encoding, errors='replace') as f:
                return f.read(), encoding
        except Exception as e:
            pass
    return None, None

# 编码修复映射
ENCODING_FIXES = {
    # 常见双字截断模式
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
    '表??': '列表',
    '集??': '集合',
    '对??': '对象',
    '实??': '实例',
    '模??': '模块',
    '组??': '组件',
    '系??': '系统',
    '程??': '程序',
    '流??': '流程',
    '器??': '管理器',
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
    '复??': '复制',
    '撤??': '撤销',
    '重??': '重做',
    '恢??': '恢复',
    '进??': '进度',
    '错??': '错误',
    '异??': '异常',
    '警??': '警告',
    '提??': '提示',
    '消??': '消息',
    '日??': '日志',
    '记??': '记录',
    '监??': '监控',
    '测??': '测试',
    '验??': '验证',
    '检??': '检查',
    '确??': '确认',
    '取??': '取消',
    '禁??': '禁止',
    '过??': '过滤',
    '排??': '排序',
    '分??': '分组',
    '统??': '统计',
    '计??': '计算',
    '处??': '处理',
    '解??': '解析',
    '转??': '转换',
    '格??': '格式',
    '编??': '编码',
    '压??': '压缩',
    '连??': '连接',
    '断??': '断开',
    '发??': '发送',
    '节??': '节点',
    '元??': '元素',
    '条??': '条件',
    '规??': '规则',
    '策??': '策略',
    '算??': '算法',
    '视??': '视图',
    '控??': '控件',
    '窗??': '窗口',
    '工??': '工具',
    '状??': '状态',
    '图??': '图像',
    '效??': '效果',
    '事??': '事件',
    '命??': '命令',
    '绑??': '绑定',
    '线??': '线程',
    '同??': '同步',
    '异??': '异步',
    '队??': '队列',
    '缓??': '缓存',
    '池??': '池',
    '内??': '内存',
    '磁??': '磁盘',
    '网??': '网络',
    '键??': '键',
    '值??': '值',
    '空??': '空值',
    '默??': '默认',
    '自??': '自动',
    '手??': '手动',
    '全??': '全局',
    '局??': '局部',
    '公??': '公共',
    '私??': '私有',
    '静??': '静态',
    '动??': '动态',
    '常??': '常量',
    '只??': '只读',
    '可??': '可写',
    '输??': '输入',
    '返??': '返回',
    '泛??': '泛型',
    '继??': '继承',
    '重??': '重写',
    '覆??': '覆盖',
    '封??': '封装',
    '抽??': '抽象',
    '虚??': '虚方法',
    '枚??': '枚举',
    '索??': '索引',
    '构??': '构造',
    '析??': '析构',
    '源??': '源',
    '目??': '目标',
    '路??': '路径',
    '地??': '地址',
    '端??': '端口',
    '客??': '客户端',
    '服??': '服务',
    '响??': '响应',
    '请??': '请求',
    '接??': '接收',
    '登??': '登录',
    '注??': '注册',
    '退??': '退出',
    '导??': '导航',
    '布??': '布局',
    '样??': '样式',
    '主??': '主题',
    '资??': '资源',
    '动??': '动画',
    '过??': '过渡',
    '触??': '触发',
    '回??': '回调',
    '委??': '委托',
    '任??': '任务',
    '并??': '并行',
    '串??': '串行',
    '栈??': '栈',
    '堆??': '堆',
    '资??': '资源',
    '字??': '字段',
    '索??': '索引',
    '唯??': '唯一',
    '外??': '外键',
    '主??': '主键',
}

# 上下文修复模式
CONTEXT_FIXES = [
    (r'用于开发调\?\?', '用于开发调试'),
    (r'一般信\?\?', '一般信息'),
    (r'配置管理器接\?\?', '配置管理器接口'),
    (r'工作流执行结\?\?', '工作流执行结果'),
    (r'是否被停\?\?', '是否被停止'),
    (r'进度百分\?\?', '进度百分比'),
    (r'总迭代次\?\?', '总迭代次数'),
    (r'执行开始时\?\?', '执行开始时间'),
    (r'中文乱码问\?\?', '中文乱码问题'),
    (r'绑定信\?\?', '绑定信息'),
    (r'所有配\?\?', '所有配置'),
    (r'保存配置到文\?\?', '保存配置到文件'),
    (r'从文件加载配\?\?', '从文件加载配置'),
    (r'加载所有配\?\?', '加载所有配置'),
]

def fix_content(content: str) -> Tuple[str, int]:
    """修复内容中的编码问题"""
    fix_count = 0
    
    # 检查是否有编码问题
    if '\ufffd' not in content:
        return content, 0
    
    # 应用简单替换
    for wrong, correct in ENCODING_FIXES.items():
        if wrong in content:
            count = content.count(wrong)
            content = content.replace(wrong, correct)
            fix_count += count
    
    # 应用上下文修复
    for pattern, correct in CONTEXT_FIXES:
        matches = re.findall(pattern, content)
        if matches:
            content = re.sub(pattern, correct, content)
            fix_count += len(matches)
    
    return content, fix_count

def find_cs_files(root_dir: Path) -> List[Path]:
    """查找所有C#源文件"""
    cs_files = []
    for root, dirs, files in os.walk(root_dir):
        dirs[:] = [d for d in dirs if d not in ['obj', 'bin', '.git']]
        for file in files:
            if file.endswith('.cs'):
                cs_files.append(Path(root) / file)
    return cs_files

def main():
    print("=" * 70)
    print("SunEyeVision 编码问题批量修复脚本 v3")
    print("支持自动检测文件编码")
    print("=" * 70)
    
    src_dir = Path(__file__).parent / 'src'
    if not src_dir.exists():
        print(f"错误: 找不到src目录")
        return
    
    print(f"\n扫描目录: {src_dir}")
    cs_files = find_cs_files(src_dir)
    print(f"找到 {len(cs_files)} 个C#文件")
    
    # 分析文件
    print("\n分析编码问题...")
    problem_files = []
    total_fixes = 0
    
    for file_path in cs_files:
        content, encoding = read_file_with_encoding(file_path)
        if content is None:
            print(f"  无法读取: {file_path.name}")
            continue
        
        if '\ufffd' in content:
            fixed_content, fix_count = fix_content(content)
            if fix_count > 0:
                rel_path = file_path.relative_to(src_dir.parent)
                problem_files.append((file_path, rel_path, encoding, fix_count, fixed_content))
                total_fixes += fix_count
    
    print(f"\n发现 {len(problem_files)} 个文件存在编码问题")
    print(f"总计可修复 {total_fixes} 处问题")
    
    if not problem_files:
        print("\n没有发现可修复的编码问题!")
        return
    
    # 显示问题文件
    print("\n问题文件列表:")
    print("-" * 70)
    problem_files.sort(key=lambda x: x[3], reverse=True)
    for _, rel_path, encoding, fix_count, _ in problem_files[:30]:
        print(f"  {rel_path} [{encoding}] ({fix_count}处)")
    if len(problem_files) > 30:
        print(f"  ... 还有 {len(problem_files) - 30} 个文件")
    
    # 询问是否修复
    print("\n" + "=" * 70)
    print("警告: 运行修复将覆盖原文件!")
    print("=" * 70)
    
    response = input("\n是否执行修复? (y/n): ").strip().lower()
    if response != 'y':
        print("已取消修复")
        return
    
    # 执行修复
    print("\n开始修复...")
    fixed_count = 0
    for file_path, rel_path, encoding, fix_count, fixed_content in problem_files:
        try:
            # 统一转换为UTF-8编码保存
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(fixed_content)
            print(f"  已修复: {rel_path} ({fix_count}处)")
            fixed_count += 1
        except Exception as e:
            print(f"  修复失败: {rel_path} - {e}")
    
    print(f"\n修复完成! 共修复 {fixed_count} 个文件")
    print("所有文件已转换为UTF-8编码")

if __name__ == '__main__':
    main()
