# Markdown 转 Word 文档转换工具

## 功能说明

此工具可以将包含 Mermaid 图表的 Markdown 文档自动转换为 Word 文档,并自动将 Mermaid 图表转换为图片嵌入到 Word 文档中。

## 特性

✅ **自动识别 Mermaid 图表** - 提取 Markdown 中的 Mermaid 代码块
✅ **在线生成图片** - 使用 Mermaid Live Editor API 将图表转换为 SVG/PNG
✅ **保留文档结构** - 保留标题、段落、列表等格式
✅ **中文支持** - 自动设置中文字体(宋体、微软雅黑)
✅ **一键运行** - 提供批处理脚本,简化操作流程

## 前置要求

1. **Python 3.7+**
   - 下载地址: https://www.python.org/downloads/
   - 安装时请勾选 "Add Python to PATH"

2. **Python 依赖包**
   - 自动通过 pip 安装
   - 包含: python-docx, requests, cairosvg

3. **网络连接**
   - 需要访问 Mermaid Live Editor API (https://mermaid.ink)

## 快速开始

### 方法1: 使用批处理脚本(推荐)

双击运行 `convert_to_word.bat` 文件,脚本会自动:
1. 检查 Python 环境
2. 安装必要的依赖包(首次运行)
3. 执行转换
4. 生成 Word 文档

### 方法2: 手动运行 Python 脚本

#### Step 1: 安装依赖

```bash
pip install -r requirements_word_converter.txt
```

或手动安装:

```bash
pip install python-docx requests cairosvg
```

#### Step 2: 运行转换脚本

**转换默认文件 (docs/COMPLETE_ARCHITECTURE.md):**

```bash
python convert_md_to_word.py
```

**转换指定文件:**

```bash
python convert_md_to_word.py "路径/到/你的/文件.md"
```

## 使用示例

### 示例1: 转换架构文档

```bash
python convert_md_to_word.py docs/COMPLETE_ARCHITECTURE.md
```

输出文件: `docs/COMPLETE_ARCHITECTURE.docx`

### 示例2: 转换其他 Markdown 文件

```bash
python convert_md_to_word.py docs/项目架构实施指南.md
```

## 工作原理

```
Markdown 文件
    ↓
[提取 Mermaid 代码块]
    ↓
[调用在线 API 生成 SVG 图片]
    ↓
[转换为 PNG 图片]
    ↓
[创建 Word 文档]
    ↓
[插入文本和图片]
    ↓
Word 文档 (.docx)
```

## 输出文件

转换后的 Word 文档会保存在与 Markdown 文件相同的目录下,文件名相同但扩展名为 `.docx`。

示例:
- 输入: `docs/COMPLETE_ARCHITECTURE.md`
- 输出: `docs/COMPLETE_ARCHITECTURE.docx`

## 注意事项

### 1. 字体支持

- 默认使用"宋体"作为正文
- 标题使用"微软雅黑"
- 如果系统未安装这些字体,Word 会使用默认字体

### 2. 图片质量

- SVG 转 PNG 的分辨率为 1200px
- 图片宽度设置为 6 英寸
- 如需调整图片大小,可修改代码中的 `width=Inches(6)` 参数

### 3. 网络依赖

- Mermaid 图表需要在线 API 生成
- 如果网络连接不稳定,可能导致图表生成失败
- 建议在网络良好的环境下运行

### 4. 临时文件

- 转换过程中会生成临时 SVG/PNG 文件
- 保存在 `temp_images/` 目录下
- 转换完成后可选择是否删除这些临时文件

## 常见问题

### Q1: 运行提示 "未找到命令 'python'"

**解决方案:**
1. 确认已安装 Python
2. 将 Python 添加到系统 PATH 环境变量
3. 或者尝试使用 `python3` 命令

### Q2: 安装依赖时失败

**解决方案:**
1. 更新 pip: `python -m pip install --upgrade pip`
2. 使用国内镜像源:
   ```bash
   pip install -r requirements_word_converter.txt -i https://pypi.tuna.tsinghua.edu.cn/simple
   ```

### Q3: 图表生成失败

**解决方案:**
1. 检查网络连接
2. 确认 Mermaid 代码语法正确
3. 查看控制台输出的错误信息

### Q4: 生成的 Word 文档乱码

**解决方案:**
1. 确认系统已安装中文字体
2. 打开 Word 文档时选择正确的编码(UTF-8)
3. 检查源 Markdown 文件是否为 UTF-8 编码

## 手动安装 cairosvg (Linux/Mac)

在 Linux 或 Mac 系统上,cairosvg 可能需要额外的系统依赖:

### Ubuntu/Debian:
```bash
sudo apt-get install python3-cairo
pip install cairosvg
```

### macOS:
```bash
brew install cairo pango
pip install cairosvg
```

### Windows:
```bash
pip install cairosvg
```

## 高级用法

### 修改图片大小

编辑 `convert_md_to_word.py`,找到 `_add_image` 方法,修改 `width` 参数:

```python
doc.add_picture(png_path, width=Inches(8))  # 改为 8 英寸宽
```

### 修改字体设置

编辑 `convert_md_to_word.py`,找到 `_setup_document_styles` 方法:

```python
font.name = '黑体'  # 改为黑体
font.size = Pt(12)  # 改为 12 号字
```

### 使用离线方式生成图表

如果需要离线使用,可以安装 Mermaid CLI:

```bash
npm install -g @mermaid-js/mermaid-cli
```

然后修改代码中的 `_convert_mermaid_to_svg` 方法,使用本地 mmdc 命令。

## 技术支持

如有问题,请检查:
1. Python 版本是否为 3.7+
2. 所有依赖包是否已正确安装
3. Markdown 文件路径是否正确
4. Mermaid 代码语法是否正确

## 更新日志

### v1.0.0 (2026-01-28)
- 初始版本
- 支持 Markdown 转 Word
- 支持 Mermaid 图表自动转换
- 支持中文字体
- 提供批处理脚本

## 许可证

本工具用于 SunEyeVision 项目内部使用。
