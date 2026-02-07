#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
SunEyeVision 插件系统重构脚本 - 三层架构迁移
将单一PluginSystem项目拆分为：
1. SunEyeVision.PluginSystem.Base (基础框架，无WPF依赖)
2. SunEyeVision.PluginSystem (插件管理+UI支持)
3. SunEyeVision.Tools (工具插件独立项目)
"""

import os
import sys
import shutil
import re
from pathlib import Path

# 设置输出编码为UTF-8
if sys.platform == 'win32':
    import codecs
    sys.stdout = codecs.getwriter('utf-8')(sys.stdout.detach())
    sys.stderr = codecs.getwriter('utf-8')(sys.stderr.detach())

# 项目根目录
ROOT_DIR = Path("d:/MyWork/SunEyeVision/SunEyeVision")

# 文件映射：源文件 -> 目标位置
FILES_TO_MOVE_TO_BASE = {
    # Interfaces
    "SunEyeVision.PluginSystem/Core/Interfaces/IPluginManager.cs": "SunEyeVision.PluginSystem.Base/Interfaces/IPluginManager.cs",
    "SunEyeVision.PluginSystem/Core/Interfaces/IToolPlugin.cs": "SunEyeVision.PluginSystem.Base/Interfaces/IToolPlugin.cs",
    "SunEyeVision.PluginSystem/Core/Interfaces/IVisionPlugin.cs": "SunEyeVision.PluginSystem.Base/Interfaces/IVisionPlugin.cs",

    # Models
    "SunEyeVision.PluginSystem/Core/Models/ToolMetadata.cs": "SunEyeVision.PluginSystem.Base/Models/ToolMetadata.cs",
    "SunEyeVision.PluginSystem/Core/Models/ParameterMetadata.cs": "SunEyeVision.PluginSystem.Base/Models/ParameterMetadata.cs",
    "SunEyeVision.PluginSystem/Core/Models/ValidationResult.cs": "SunEyeVision.PluginSystem.Base/Models/ValidationResult.cs",

    # Services
    "SunEyeVision.PluginSystem/Core/Services/PluginLoader.cs": "SunEyeVision.PluginSystem.Base/Services/PluginLoader.cs",
    "SunEyeVision.PluginSystem/Core/Services/ToolRegistry.cs": "SunEyeVision.PluginSystem.Base/Services/ToolRegistry.cs",

    # Base (ObservableObject - 移除WPF依赖)
    "SunEyeVision.PluginSystem/Infrastructure/Base/ObservableObject.cs": "SunEyeVision.PluginSystem.Base/Base/ObservableObject.cs",

    # Parameters (ParameterItem - 移除WPF依赖)
    "SunEyeVision.PluginSystem/Parameters/ParameterItem.cs": "SunEyeVision.PluginSystem.Base/Base/ParameterItem.cs",
}

# 文件映射：Tools文件夹 -> SunEyeVision.Tools项目
FILES_TO_MOVE_TO_TOOLS = {
    "SunEyeVision.PluginSystem/Tools": "SunEyeVision.Tools",
}

# 命名空间映射
NAMESPACE_REPLACEMENTS = {
    "SunEyeVision.PluginSystem.Core": "SunEyeVision.PluginSystem.Base",
    "SunEyeVision.PluginSystem.Infrastructure": "SunEyeVision.PluginSystem",
}

# WPF依赖需要移除的内容
WPF_DEPENDENCIES = [
    "using System.Windows.Controls;",
    "using System.Windows;",
    "using System.Windows.Data;",
    "using System.Windows.Input;",
    "using System.Windows.Media;",
    "using System.Windows.Shapes;",
    "using System.Windows.Documents;",
    "public Control? Control { get; set; }",
    "Control? Control",
]

def create_project_structure():
    """创建项目目录结构"""
    print("[DIR] 创建项目目录结构...")

    # Base项目目录
    base_dirs = [
        "SunEyeVision.PluginSystem.Base/Interfaces",
        "SunEyeVision.PluginSystem.Base/Models",
        "SunEyeVision.PluginSystem.Base/Services",
        "SunEyeVision.PluginSystem.Base/Base",
    ]
    for dir_path in base_dirs:
        (ROOT_DIR / dir_path).mkdir(parents=True, exist_ok=True)
        print(f"  [OK] {dir_path}")

    # Tools项目目录（保留Tools文件夹结构）
    tools_src = ROOT_DIR / "SunEyeVision.PluginSystem/Tools"
    if tools_src.exists():
        print(f"  [OK] SunEyeVision.Tools 将使用现有Tools文件夹")

def create_base_csproj():
    """创建 SunEyeVision.PluginSystem.Base.csproj"""
    print("[CS] 创建 SunEyeVision.PluginSystem.Base.csproj...")

    csproj_content = r"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0-windows</TargetFramework>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <UseWPF>false</UseWPF>
    <RootNamespace>SunEyeVision.PluginSystem.Base</RootNamespace>
    <AssemblyName>SunEyeVision.PluginSystem.Base</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\\SunEyeVision.Core\\SunEyeVision.Core.csproj" />
  </ItemGroup>
</Project>
"""

    csproj_path = ROOT_DIR / "SunEyeVision.PluginSystem.Base/SunEyeVision.PluginSystem.Base.csproj"
    csproj_path.write_text(csproj_content, encoding='utf-8')
    print(f"  [OK] {csproj_path}")

def create_tools_csproj():
    """创建 SunEyeVision.Tools.csproj"""
    print("[CS] 创建 SunEyeVision.Tools.csproj...")

    # 确保Tools目录存在
    tools_dir = ROOT_DIR / "SunEyeVision.Tools"
    tools_dir.mkdir(parents=True, exist_ok=True)

    csproj_content = r"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0-windows</TargetFramework>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <UseWPF>false</UseWPF>
    <RootNamespace>SunEyeVision.Tools</RootNamespace>
    <AssemblyName>SunEyeVision.Tools</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\\SunEyeVision.Core\\SunEyeVision.Core.csproj" />
    <ProjectReference Include="..\\SunEyeVision.PluginSystem.Base\\SunEyeVision.PluginSystem.Base.csproj" />
    <!-- Optional: add when UI support is needed -->
    <!-- <ProjectReference Include="..\\SunEyeVision.PluginSystem\\SunEyeVision.PluginSystem.csproj" /> -->
  </ItemGroup>
</Project>
"""

    csproj_path = tools_dir / "SunEyeVision.Tools.csproj"
    csproj_path.write_text(csproj_content, encoding='utf-8')
    print(f"  [OK] {csproj_path}")

def move_file(src_path, dst_path, update_namespace=True):
    """移动文件并更新命名空间"""
    src = ROOT_DIR / src_path
    dst = ROOT_DIR / dst_path

    if not src.exists():
        print(f"  [WARN] 源文件不存在: {src_path}")
        return False

    # 创建目标目录
    dst.parent.mkdir(parents=True, exist_ok=True)

    # 读取文件内容，尝试多种编码
    content = None
    for encoding in ['utf-8', 'gbk', 'latin-1']:
        try:
            content = src.read_text(encoding=encoding)
            break
        except (UnicodeDecodeError, UnicodeError):
            continue

    if content is None:
        print(f"  [ERROR] 无法读取文件: {src_path}")
        return False

    # 更新命名空间
    if update_namespace:
        for old_ns, new_ns in NAMESPACE_REPLACEMENTS.items():
            content = content.replace(f"namespace {old_ns}", f"namespace {new_ns}")
            content = content.replace(f"using {old_ns}", f"using {new_ns}")

    # 移除WPF依赖
    for wpf_dep in WPF_DEPENDENCIES:
        content = content.replace(wpf_dep, f"// Removed: {wpf_dep}")

    # 写入目标文件
    dst.write_text(content, encoding='utf-8')
    print(f"  [OK] {src_path} -> {dst_path}")
    return True

def move_files_to_base():
    """移动文件到Base项目"""
    print("\n[MOVE] 移动文件到 SunEyeVision.PluginSystem.Base...")

    moved_count = 0
    for src, dst in FILES_TO_MOVE_TO_BASE.items():
        if move_file(src, dst):
            moved_count += 1

    print(f"  [STAT] 共移动 {moved_count}/{len(FILES_TO_MOVE_TO_BASE)} 个文件")

def move_tools_to_tools_project():
    """移动Tools文件夹到Tools项目"""
    print("\n[MOVE] 移动Tools文件夹到 SunEyeVision.Tools...")

    tools_src = ROOT_DIR / "SunEyeVision.PluginSystem/Tools"
    tools_dst = ROOT_DIR / "SunEyeVision.Tools/Tools"

    if not tools_src.exists():
        print(f"  [WARN] Tools文件夹不存在")
        return

    # 复制Tools文件夹
    if tools_dst.exists():
        shutil.rmtree(tools_dst)

    shutil.copytree(tools_src, tools_dst)
    print(f"  [OK] {tools_src} -> {tools_dst}")

    # 更新命名空间
    for cs_file in tools_dst.rglob("*.cs"):
        content = cs_file.read_text(encoding='utf-8')
        original = content

        # 更新命名空间
        content = re.sub(
            r'namespace SunEyeVision\.PluginSystem\.Tools',
            'namespace SunEyeVision.Tools',
            content
        )

        # 更新using语句
        content = re.sub(
            r'using SunEyeVision\.PluginSystem\.Core',
            'using SunEyeVision.PluginSystem.Base',
            content
        )

        if content != original:
            cs_file.write_text(content, encoding='utf-8')
            print(f"  [OK] 更新命名空间: {cs_file.relative_to(ROOT_DIR)}")

def update_pluginsystem_csproj():
    """更新 PluginSystem.csproj，添加对Base的引用"""
    print("\n[REF] 更新 SunEyeVision.PluginSystem.csproj...")

    csproj_path = ROOT_DIR / "SunEyeVision.PluginSystem/SunEyeVision.PluginSystem.csproj"
    if not csproj_path.exists():
        print(f"  [WARN] csproj文件不存在")
        return

    content = csproj_path.read_text(encoding='utf-8')

    # 添加对Base的引用
    base_ref = r'    <ProjectReference Include="..\\SunEyeVision.PluginSystem.Base\\SunEyeVision.PluginSystem.Base.csproj" />'

    if "SunEyeVision.PluginSystem.Base" not in content:
        # 找到 </ItemGroup> 的最后一个位置插入
        lines = content.split('\n')
        insert_idx = -1

        for i in range(len(lines)-1, -1, -1):
            if '</ItemGroup>' in lines[i] and insert_idx == -1:
                insert_idx = i

        if insert_idx > 0:
            lines.insert(insert_idx, base_ref)
            content = '\n'.join(lines)
            csproj_path.write_text(content, encoding='utf-8')
            print(f"  [OK] 添加对SunEyeVision.PluginSystem.Base的引用")
        else:
            print(f"  [WARN] 无法找到插入点")
    else:
        print(f"  [OK] 已经包含对Base的引用")

def remove_old_files():
    """删除已移动的源文件"""
    print("\n[DEL] 删除已移动的源文件（可选）...")
    print("  [INFO] 跳过删除步骤，保留原始文件")
    print("  [INFO] 如需删除，请手动执行以下命令：")
    for src in FILES_TO_MOVE_TO_BASE.keys():
        print(f"    del {src}")

def update_solution_file():
    """更新解决方案文件，添加新项目"""
    print("\n[INFO] 更新解决方案文件...")

    sln_path = ROOT_DIR / "SunEyeVision.sln"
    if not sln_path.exists():
        print(f"  [WARN] 解决方案文件不存在")
        return

    content = sln_path.read_text(encoding='utf-8')

    # TODO: 需要生成实际的GUID
    print(f"  [INFO] 需要手动添加项目到解决方案文件")
    print(f"  [INFO] 或使用Visual Studio打开时自动添加")

def print_summary():
    """打印重构总结"""
    print("\n" + "="*60)
    print("[SUCCESS] 重构完成！")
    print("="*60)
    print("\n[STEPS] 后续步骤：")
    print("\n[1] 在Visual Studio中打开解决方案")
    print("   - 右键解决方案 -> 添加 -> 现有项目")
    print("   - 选择 SunEyeVision.PluginSystem.Base.csproj")
    print("   - 选择 SunEyeVision.Tools.csproj")
    print("\n[2] 更新项目引用")
    print("   - SunEyeVision.PluginSystem 引用 SunEyeVision.PluginSystem.Base")
    print("   - SunEyeVision.Tools 引用 SunEyeVision.PluginSystem.Base")
    print("   - SunEyeVision.UI 引用 SunEyeVision.PluginSystem.Base")
    print("   - SunEyeVision.Workflow 引用 SunEyeVision.PluginSystem.Base")
    print("\n[3] 编译验证")
    print("   - 修复编译错误")
    print("   - 更新using语句")
    print("\n[4] 测试")
    print("   - 运行单元测试")
    print("   - 验证功能正常")
    print("\n[5] 清理旧文件（可选）")
    print("   - 删除SunEyeVision.PluginSystem中已移动的文件")
    print("\n" + "="*60)

def main():
    """主函数"""
    print("[START] 开始SunEyeVision插件系统重构...")
    print("="*60)

    try:
        # 步骤1：创建目录结构
        create_project_structure()

        # 步骤2：创建Base项目文件
        create_base_csproj()

        # 步骤3：创建Tools项目文件
        create_tools_csproj()

        # 步骤4：移动文件到Base项目
        move_files_to_base()

        # 步骤5：移动Tools文件夹
        move_tools_to_tools_project()

        # 步骤6：更新PluginSystem.csproj
        update_pluginsystem_csproj()

        # 步骤7：删除旧文件（跳过）
        remove_old_files()

        # 步骤8：更新解决方案文件
        update_solution_file()

        # 打印总结
        print_summary()

    except Exception as e:
        print(f"\n[ERROR] 重构过程中发生错误: {e}")
        import traceback
        traceback.print_exc()

if __name__ == "__main__":
    main()
