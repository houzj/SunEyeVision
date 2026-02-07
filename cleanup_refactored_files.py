#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
清理脚本 - 删除SunEyeVision.PluginSystem中已移动到Base和Tools的文件
"""

import os
import shutil
from pathlib import Path

# 项目根目录
ROOT_DIR = Path("d:/MyWork/SunEyeVision/SunEyeVision")

# 需要删除的文件和目录
FILES_TO_DELETE = [
    # PluginSystem项目中已移动到Base的文件
    "SunEyeVision.PluginSystem/Core",
    "SunEyeVision.PluginSystem/Parameters",
    "SunEyeVision.PluginSystem/Infrastructure/Base/ObservableObject.cs",
    
    # PluginSystem项目中的Tools文件夹（已移动到独立的Tools项目）
    "SunEyeVision.PluginSystem/Tools",
]

def delete_files_and_dirs():
    """删除文件和目录"""
    print("[CLEAN] 开始清理...")
    
    for item in FILES_TO_DELETE:
        path = ROOT_DIR / item
        if not path.exists():
            print(f"  [SKIP] {item} 不存在")
            continue
        
        try:
            if path.is_dir():
                shutil.rmtree(path)
                print(f"  [DEL] 目录: {item}")
            else:
                path.unlink()
                print(f"  [DEL] 文件: {item}")
        except Exception as e:
            print(f"  [ERROR] 删除 {item} 失败: {e}")

if __name__ == "__main__":
    delete_files_and_dirs()
    print("\n[DONE] 清理完成！")
    print("\n请手动检查并确保以下项目正确编译：")
    print("  1. SunEyeVision.PluginSystem.Base")
    print("  2. SunEyeVision.PluginSystem")
    print("  3. SunEyeVision.Tools")
