#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
SunEyeVision 编码问题批量修复脚本
解决GBK/GB2312与UTF-8编码冲突问题
"""

import os
import re
import chardet
from pathlib import Path
from typing import Tuple, List

# 常见中文词汇的GBK乱码模式（用于辅助检测）
GARBLED_PATTERNS = [
    r'缂栫爜',  # 编码
    r'闂',      # 问题/问
    r'鎵',      # 扫/所
    r'鏂囦欢',   # 文件
    r'鏂规硶',   # 方法
    r'鍙橀噺',   # 变量
    r'鍑芥暟',   # 函数
    r'绫',      # 类
    r'鎺ュ彛',   # 接口
    r'鍙傛暟',   # 参数
    r'灞炴',     # 属
    r'鏂规',     # 方
    r'瑙ｅ喅',   # 解决
    r'鍒涘缓',   # 创建
    r'鍒犻櫎',   # 删除
    r'鏇存柊',   # 更新
    r'鏌ヨ',     # 查
    r'璁剧疆',   # 设置
    r'鑾峰彇',   # 获取
    r'杩斿洖',   # 返回
    r'寮傚父',   # 异常
    r'閿欒',     # 错误
    r'璁＄畻',   # 计算
    r'鏁版嵁',   # 数据
    r'澶勭悊',   # 处理
    r'鍒濆鍖',   # 初始化
    r'楠岃瘉',   # 验证
    r'閰嶇疆',   # 配置
    r'鎵ц',     # 执行
    r'缁撴灉',   # 结果
    r'杈撳嚭',   # 输出
    r'杈撳叆',   # 输入
    r'瀵硅薄',   # 对象
    r'瀹炰緥',   # 实例
    r'绫诲瀷',   # 类型
    r'瀹氫箟',   # 定义
    r'缁ф壙',   # 继承
    r'瀹炵幇',   # 实现
    r'璋冪敤',   # 调用
    r'寮曠敤',   # 引用
    r'浠诲姟',   # 任务
    r'绾跨▼',   # 线程
    r'寮傛',     # 异步
    r'鍚屾',     # 同步
    r'浜嬩欢',   # 事件
    r'鍥炶皟',   # 回调
    r'娑堟伅',   # 消息
    r'閫氱煡',   # 通知
    r'鍗＄墖',   # 卡片
    r'宸ヤ綔',   # 工作
    r'娴佺▼',   # 流程
    r'鑺傜偣',   # 节点
    r'杩炴帴',   # 连接
    r'绔彛',     # 端口
    r'鐩爣',     # 目标
    r'婧',      # 源
    r'璺緞',   # 路径
    r'浣嶇疆',   # 位置
    r'鍧愭爣',   # 坐标
    r'灏哄',     # 尺寸
    r'澶у皬',   # 大小
    r'瀹藉害',   # 宽度
    r'楂樺害',   # 高度
    r'杈圭晫',   # 边界
    r'鍖哄煙',   # 区域
    r'鑼冨洿',   # 范围
    r'鏄剧ず',   # 显示
    r'闅愯',     # 隐藏
    r'鍒锋柊',   # 刷新
    r'娓叉煋',   # 渲染
    r'缁樺埗',   # 绘制
    r'鐢诲竷',   # 画布
    r'瑙嗗浘',   # 视图
    r'妯℃澘',   # 模板
    r'鏍峰紡',   # 样式
    r'璧勬簮',   # 资源
    r'鍏冪礌',   # 元素
    r'鎺т欢',   # 控件
    r'鎸夐挳',   # 按钮
    r'鑿滃崟',   # 菜单
    r'宸ュ叿',   # 工具
    r'甯冨眬',   # 布局
    r'瀹瑰櫒',   # 容器
    r'鐣岄潰',   # 界面
    r'绐楀彛',   # 窗口
    r'瀵硅瘽',   # 对话
    r'寮瑰嚭',   # 弹出
    r'鎻愮ず',   # 提示
    r'璀﹀憡',   # 警告
    r'纭',      # 确
    r'鍙栨秷',   # 取消
    r'淇濆',     # 保
    r'鎵撳紑',   # 打开
    r'鍏抽棴',   # 关闭
    r'鍚敤',     # 启用
    r'绂佺敤',   # 禁用
    r'寮€濮',   # 开始
    r'缁撴潫',   # 结束
    r'鏆傚仠',   # 暂停
    r'缁х画',   # 继续
    r'閲嶈瘯',   # 重试
    r'璺宠繃',   # 跳过
    r'涓嬩竴',   # 下一
    r'涓婁竴',   # 上一
    r'绗竴',   # 第一
    r'鏈€鍚',   # 最后
    r'褰撳墠',   # 当前
    r'鍏ㄩ儴',   # 全部
    r'閮ㄥ垎',   # 部分
    r'鍏朵粬',   # 其他
    r'鏇村',     # 更多
    r'灏戣',     # 少
    r'澶氱',     # 多
    r'绌',      # 空
    r'闈炵┖',   # 非空
    r'鏈夋晥',   # 有效
    r'鏃犳晥',   # 无效
    r'鎴愬姛',   # 成功
    r'澶辫触',   # 失败
    r'姝ｇ',     # 正确
    r'閿欒',     # 错误
    r'瀹屾暣',   # 完成
    r'绛夊緟',   # 等待
    r'杩涜',     # 进行
    r'杩涘害',   # 进度
    r'鐘舵',     # 态
    r'鏉′欢',   # 条件
    r'寰�鐜',   # 循环
    r'閫掑綊',   # 递归
    r'杩唬',   # 迭代
    r'绱㈠紩',   # 索引
    r'閿',      # 键
    r'鍊',      # 值
    r'鍒楄〃',   # 列表
    r'闆嗗悎',   # 集合
    r'瀛楀吀',   # 字典
    r'鏁扮粍',   # 数组
    r'搴忓垪',   # 序列
    r'闃熷垪',   # 队列
    r'鏍堥',     # 栈
    r'鏍戔',      # 树
    r'鍥',      # 图
    r'缁撴瀯',   # 结构
    r'缁勭粐',   # 组织
    r'妯″潡',   # 模块
    r'鎻掍欢',   # 插件
    r'鎵╁睍',   # 扩展
    r'鑷畾涔',   # 自定义
    r'閫氱敤',   # 通用
    r'涓撶敤',   # 专用
    r'鍩虹',     # 基础
    r'鏍稿績',   # 核心
    r'鏀寔',   # 支持
    r'甯姪',   # 辅助
    r'宸ュ叿',   # 工具
    r'鏂规',     # 方
    r'瑙',      # 解
]

def detect_encoding(file_path: str) -> Tuple[str, float]:
    """检测文件编码"""
    with open(file_path, 'rb') as f:
        raw_data = f.read()
    
    result = chardet.detect(raw_data)
    return result['encoding'], result['confidence']

def has_garbled_content(content: str) -> bool:
    """检查内容是否包含典型的GBK乱码模式"""
    for pattern in GARBLED_PATTERNS:
        if re.search(pattern, content):
            return True
    return False

def try_fix_encoding(file_path: str) -> Tuple[bool, str]:
    """
    尝试修复文件编码
    返回: (是否修复成功, 消息)
    """
    # 读取原始字节
    with open(file_path, 'rb') as f:
        raw_data = f.read()
    
    # 检测编码
    detected_encoding, confidence = detect_encoding(file_path)
    
    # 尝试多种编码
    encodings_to_try = []
    
    # 如果检测到GBK相关编码
    if detected_encoding and detected_encoding.lower() in ['gb2312', 'gbk', 'gb18030']:
        encodings_to_try = ['gb18030', 'gbk', 'gb2312']
    # 如果检测到UTF-8但内容有乱码
    elif detected_encoding and detected_encoding.lower().startswith('utf'):
        # 先检查是否有乱码
        try:
            content = raw_data.decode('utf-8')
            if has_garbled_content(content):
                encodings_to_try = ['gb18030', 'gbk', 'gb2312']
            else:
                return False, "UTF-8编码正常，无需修复"
        except:
            encodings_to_try = ['gb18030', 'gbk', 'gb2312', 'utf-8']
    else:
        # 未知编码，尝试常见中文编码
        encodings_to_try = ['gb18030', 'gbk', 'gb2312', 'utf-8', 'latin-1']
    
    # 尝试各种编码
    for encoding in encodings_to_try:
        try:
            content = raw_data.decode(encoding)
            
            # 检查是否有乱码
            if has_garbled_content(content):
                continue
            
            # 如果成功解码且没有乱码，转换为UTF-8
            if encoding.lower() not in ['utf-8', 'utf8']:
                # 检查是否包含中文字符
                if re.search(r'[\u4e00-\u9fff]', content):
                    # 写入UTF-8格式
                    with open(file_path, 'w', encoding='utf-8', newline='') as f:
                        f.write(content)
                    return True, f"已从 {encoding} 转换为 UTF-8"
            
            return False, f"编码正常 ({encoding})"
            
        except (UnicodeDecodeError, UnicodeEncodeError):
            continue
    
    return False, "无法确定正确编码"

def fix_files_in_directory(directory: str, extensions: List[str]) -> dict:
    """
    修复目录下所有指定扩展名的文件
    """
    stats = {
        'total': 0,
        'fixed': 0,
        'skipped': 0,
        'failed': 0,
        'details': []
    }
    
    for root, dirs, files in os.walk(directory):
        # 跳过某些目录
        skip_dirs = ['.git', '.vs', 'bin', 'obj', 'node_modules', 'packages', 'nupkg']
        dirs[:] = [d for d in dirs if d not in skip_dirs]
        
        for file in files:
            if any(file.endswith(ext) for ext in extensions):
                file_path = os.path.join(root, file)
                stats['total'] += 1
                
                try:
                    success, message = try_fix_encoding(file_path)
                    if success:
                        stats['fixed'] += 1
                        stats['details'].append((file_path, 'FIXED', message))
                        print(f"[FIXED] {file_path}: {message}")
                    else:
                        stats['skipped'] += 1
                except Exception as e:
                    stats['failed'] += 1
                    stats['details'].append((file_path, 'FAILED', str(e)))
                    print(f"[FAILED] {file_path}: {e}")
    
    return stats

def main():
    """主函数"""
    print("=" * 60)
    print("    SunEyeVision 编码修复工具")
    print("=" * 60)
    
    # 要处理的目录
    directories = ['src', 'tools', 'docs', 'PluginDevKit']
    
    # 要处理的文件扩展名
    extensions = ['.cs', '.xaml', '.csproj', '.xml', '.json', '.md', '.ps1', '.bat']
    
    total_stats = {
        'total': 0,
        'fixed': 0,
        'skipped': 0,
        'failed': 0
    }
    
    for directory in directories:
        if os.path.exists(directory):
            print(f"\n处理目录: {directory}")
            print("-" * 40)
            stats = fix_files_in_directory(directory, extensions)
            
            for key in ['total', 'fixed', 'skipped', 'failed']:
                total_stats[key] += stats[key]
    
    print("\n" + "=" * 60)
    print("修复完成！统计信息:")
    print(f"  总文件数: {total_stats['total']}")
    print(f"  已修复: {total_stats['fixed']}")
    print(f"  已跳过: {total_stats['skipped']}")
    print(f"  失败: {total_stats['failed']}")
    print("=" * 60)

if __name__ == '__main__':
    main()
