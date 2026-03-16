#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
SunEyeVision Solution Icon Generator v2
生成符合视觉软件气质的专业图标（兼容版本）
"""

from PIL import Image, ImageDraw, ImageFont
import math

def create_gradient_circle(draw, center, radius, start_color, end_color):
    """创建渐变圆形"""
    cx, cy = center
    for r in range(radius, 0, -1):
        ratio = r / radius
        
        # 颜色插值
        r_val = int(start_color[0] * ratio + end_color[0] * (1 - ratio))
        g_val = int(start_color[1] * ratio + end_color[1] * (1 - ratio))
        b_val = int(start_color[2] * ratio + end_color[2] * (1 - ratio))
        
        draw.ellipse([
            cx - r, cy - r,
            cx + r, cy + r
        ], fill=(r_val, g_val, b_val))

def draw_sun_rays(draw, center, outer_radius, ray_count, color):
    """绘制太阳光芒"""
    cx, cy = center
    ray_length = outer_radius // 6
    angle_step = 360 / ray_count
    
    for i in range(ray_count):
        angle = math.radians(i * angle_step)
        
        # 计算光芒线条
        start_x = cx + math.cos(angle) * outer_radius
        start_y = cy + math.sin(angle) * outer_radius
        end_x = cx + math.cos(angle) * (outer_radius + ray_length)
        end_y = cy + math.sin(angle) * (outer_radius + ray_length)
        
        # 绘制光芒（ tapered 效果）
        width = 2
        draw.line([start_x, start_y, end_x, end_y], fill=color, width=width)
        
        # 光芒末端小圆点
        dot_radius = 2
        draw.ellipse([
            end_x - dot_radius, end_y - dot_radius,
            end_x + dot_radius, end_y + dot_radius
        ], fill=color)

def draw_eye_symbol(draw, center, size, main_color, stroke_color):
    """绘制眼睛符号"""
    cx, cy = center
    half_size = size // 2
    
    # 1. 外轮廓 - 椭圆形眼睛
    eye_width = half_size
    eye_height = half_size // 2
    
    # 绘制眼白（浅色背景）
    draw.ellipse([
        cx - eye_width, cy - eye_height,
        cx + eye_width, cy + eye_height
    ], fill="#ffffff", outline=stroke_color, width=2)
    
    # 2. 虹膜 - 渐变蓝色
    iris_radius = int(eye_height * 0.7)
    create_gradient_circle(draw, (cx, cy), iris_radius, (59, 130, 246), (41, 98, 255))
    
    # 3. 瞳孔 - 深色中心
    pupil_radius = iris_radius // 3
    draw.ellipse([
        cx - pupil_radius, cy - pupil_radius,
        cx + pupil_radius, cy + pupil_radius
    ], fill="#1a1a2e")
    
    # 4. 高光 - 增加立体感
    highlight_radius = pupil_radius // 2
    highlight_x = cx - pupil_radius // 3
    highlight_y = cy - pupil_radius // 3
    draw.ellipse([
        highlight_x - highlight_radius, highlight_y - highlight_radius,
        highlight_x + highlight_radius, highlight_y + highlight_radius
    ], fill="#ffffff", width=0)

def draw_decorative_corners(draw, size, color, margin=10):
    """绘制装饰性角落"""
    # 左上角
    corner_size = size // 6
    draw.line([margin, margin, margin + corner_size, margin], fill=color, width=3)
    draw.line([margin, margin, margin, margin + corner_size], fill=color, width=3)
    
    # 右上角
    draw.line([size - margin, margin, size - margin - corner_size, margin], fill=color, width=3)
    draw.line([size - margin, margin, size - margin, margin + corner_size], fill=color, width=3)
    
    # 左下角
    draw.line([margin, size - margin, margin + corner_size, size - margin], fill=color, width=3)
    draw.line([margin, size - margin, margin, size - margin - corner_size], fill=color, width=3)
    
    # 右下角
    draw.line([size - margin, size - margin, size - margin - corner_size, size - margin], fill=color, width=3)
    draw.line([size - margin, size - margin, size - margin, size - margin - corner_size], fill=color, width=3)

def create_icon(size):
    """创建指定尺寸的图标"""
    # 创建透明背景的画布
    image = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    
    center = size // 2
    margin = max(4, size // 16)
    
    # 1. 背景圆角矩形（使用圆角矩形替代方案）
    bg_size = size - margin * 2
    
    # 绘制浅色背景
    draw.rectangle([
        margin, margin,
        size - margin, size - margin
    ], fill="#f8f9fa", outline="#3498db", width=2)
    
    # 2. 太阳核心（在中心位置）
    sun_radius = size // 8
    
    # 太阳渐变：从浅橙色到深橙色
    create_gradient_circle(draw, (center, center), sun_radius, (255, 200, 50), (243, 156, 18))
    
    # 太阳外圈
    draw.ellipse([
        center - sun_radius - 2, center - sun_radius - 2,
        center + sun_radius + 2, center + sun_radius + 2
    ], outline="#e67e22", width=2)
    
    # 3. 太阳光芒
    draw_sun_rays(draw, (center, center), sun_radius + 4, 16, "#f39c12")
    
    # 4. 眼睛符号（叠加在太阳上方）
    eye_size = size // 3
    draw_eye_symbol(draw, (center, center), eye_size, "#3498db", "#2980b9")
    
    # 5. 装饰性角落
    draw_decorative_corners(draw, size, "#3498db", margin)
    
    # 6. 底部添加文字（可选，小尺寸时省略）
    if size >= 64:
        try:
            # 尝试使用默认字体
            font_size = max(8, size // 16)
            font = ImageFont.truetype("arial.ttf", font_size)
        except:
            font = ImageFont.load_default()
        
        text = "SEV"
        bbox = draw.textbbox((0, 0), text, font=font)
        text_width = bbox[2] - bbox[0]
        text_height = bbox[3] - bbox[1]
        
        text_x = center - text_width // 2
        text_y = size - margin - text_height - 4
        
        draw.text((text_x, text_y), text, fill="#2c3e50", font=font)
    
    return image

def generate_ico_file(output_path, sizes=[16, 32, 48, 64, 128, 256]):
    """生成多尺寸ICO文件"""
    print("🎨 开始生成 SunEyeVision 图标...")
    print(f"📐 生成尺寸: {', '.join(map(str, sizes))} px\n")
    
    images = []
    
    for size in sizes:
        print(f"   ⚙️  正在生成 {size:3d}x{size:3d} 图标...", end=" ")
        image = create_icon(size)
        images.append(image)
        print("✅")
    
    # 保存为ICO文件
    print(f"\n💾 保存到: {output_path}")
    images[0].save(
        output_path,
        format='ICO',
        sizes=[(img.width, img.height) for img in images],
        append_images=images[1:]
    )
    
    print("✅ 图标生成完成！")
    return output_path

def generate_preview_png(output_path, size=256):
    """生成预览PNG文件"""
    print(f"🖼️  生成预览图: {size}x{size}")
    image = create_icon(size)
    image.save(output_path, 'PNG')
    print(f"💾 预览图保存到: {output_path}")
    return output_path

def main():
    """主函数"""
    print("=" * 60)
    print("  SunEyeVision Solution Icon Generator v2")
    print("=" * 60)
    print()
    
    # 设置输出路径
    icon_dir = "src/UI/Icons"
    import os
    os.makedirs(icon_dir, exist_ok=True)
    
    icon_path = os.path.join(icon_dir, "solution.ico")
    preview_path = os.path.join(icon_dir, "solution_preview.png")
    
    # 生成图标
    generate_ico_file(icon_path)
    
    # 生成预览图
    generate_preview_png(preview_path)
    
    print()
    print("=" * 60)
    print("🎉 全部完成！")
    print("=" * 60)
    print(f"   📁 图标文件: {icon_path}")
    print(f"   🖼️  预览图片: {preview_path}")
    print()
    print("💡 现在您可以：")
    print("   1. 运行 SunEyeVision.UI.exe")
    print("   2. 注册文件关联")
    print("   3. 在文件资源管理器中查看 .solution 文件的图标")
    print()

if __name__ == "__main__":
    main()
