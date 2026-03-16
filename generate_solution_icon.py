#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
SunEyeVision Solution Icon Generator
生成符合视觉软件气质的专业图标
"""

from PIL import Image, ImageDraw, ImageFont
import os
import math

def draw_sun_rays(draw, center, radius, ray_count, ray_length, color):
    """绘制太阳光芒"""
    angle_step = 360 / ray_count
    for i in range(ray_count):
        angle = math.radians(i * angle_step)
        start_x = center[0] + math.cos(angle) * radius
        start_y = center[1] + math.sin(angle) * radius
        end_x = center[0] + math.cos(angle) * (radius + ray_length)
        end_y = center[1] + math.sin(angle) * (radius + ray_length)
        
        # 光芒渐变效果
        for offset in range(ray_length):
            alpha = int(255 * (1 - offset / ray_length))
            ray_x = center[0] + math.cos(angle) * (radius + offset)
            ray_y = center[1] + math.sin(angle) * (radius + offset)
            
            # 绘制小圆点形成光芒
            if offset % 3 == 0:
                draw.ellipse([
                    ray_x - 1, ray_y - 1,
                    ray_x + 1, ray_y + 1
                ], fill=color)

def draw_eye_shape(draw, center, size, color, stroke_color):
    """绘制抽象眼睛形状"""
    x, y = center
    half_size = size // 2
    
    # 外眼睑 - 优雅的曲线
    draw.arc([
        x - half_size, y - half_size // 2,
        x + half_size, y + half_size // 2
    ], start=180, end=0, fill=stroke_color, width=2)
    
    # 下眼睑 - 更柔和的曲线
    draw.arc([
        x - half_size, y - half_size // 3,
        x + half_size, y + half_size // 3
    ], start=0, end=180, fill=stroke_color, width=2)
    
    # 虹膜 - 渐变圆形
    iris_radius = half_size // 2
    for i in range(iris_radius, 0, -1):
        alpha = int(255 * (i / iris_radius))
        draw.ellipse([
            x - i, y - i,
            x + i, y + i
        ], fill=color)
    
    # 瞳孔 - 深色中心
    pupil_radius = iris_radius // 3
    draw.ellipse([
        x - pupil_radius, y - pupil_radius,
        x + pupil_radius, y + pupil_radius
    ], fill="#1a1a2e")
    
    # 高光 - 增加立体感
    highlight_radius = pupil_radius // 2
    draw.ellipse([
        x - highlight_radius - pupil_radius // 4, 
        y - highlight_radius - pupil_radius // 4,
        x - highlight_radius + pupil_radius // 4, 
        y - highlight_radius + pupil_radius // 4
    ], fill="#ffffff")

def create_gradient_background(size):
    """创建渐变背景"""
    image = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    
    # 创建蓝色到橙色渐变（代表太阳到科技）
    center = size // 2
    max_radius = int(size * 0.8)
    
    for radius in range(max_radius, 0, -1):
        ratio = radius / max_radius
        
        # 颜色插值
        r = int(59 + (255 - 59) * ratio)
        g = int(130 + (200 - 130) * ratio)
        b = int(246 + (97 - 246) * ratio)
        a = int(255 * (1 - (radius / max_radius) ** 0.5))
        
        draw.ellipse([
            center - radius, center - radius,
            center + radius, center + radius
        ], fill=(r, g, b, a))
    
    return image

def create_icon(size):
    """创建指定尺寸的图标"""
    # 创建画布
    image = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    
    center = size // 2
    
    # 1. 背景 - 渐变圆角矩形
    margin = size // 8
    draw.rounded_rectangle([
        margin, margin,
        size - margin, size - margin
    ], radius=size // 6, fill="#f8f9fa", outline="#3498db", width=2)
    
    # 2. 太阳光芒 - 从中心辐射
    sun_radius = size // 8
    ray_count = 12
    ray_length = size // 10
    draw_sun_rays(draw, (center, center), sun_radius, ray_count, ray_length, "#f39c12")
    
    # 3. 太阳核心 - 渐变圆形
    for r in range(sun_radius, 0, -1):
        ratio = r / sun_radius
        red = int(255 * ratio + 243 * (1 - ratio))
        green = int(200 * ratio + 156 * (1 - ratio))
        blue = int(50 * ratio + 18 * (1 - ratio))
        draw.ellipse([
            center - r, center - r,
            center + r, center + r
        ], fill=(red, green, blue))
    
    # 4. 眼睛 - 在中心叠加
    eye_size = size // 3
    draw_eye_shape(draw, (center, center), eye_size, "#3498db", "#2980b9")
    
    # 5. 装饰元素 - 科技感线条
    decor_size = size // 6
    # 左上角装饰
    draw.line([margin + 5, margin + 5, margin + 5 + decor_size, margin + 5], 
              fill="#3498db", width=2)
    draw.line([margin + 5, margin + 5, margin + 5, margin + 5 + decor_size], 
              fill="#3498db", width=2)
    # 右下角装饰
    draw.line([size - margin - 5, size - margin - 5, size - margin - 5 - decor_size, size - margin - 5], 
              fill="#3498db", width=2)
    draw.line([size - margin - 5, size - margin - 5, size - margin - 5, size - margin - 5 - decor_size], 
              fill="#3498db", width=2)
    
    return image

def generate_ico_file(output_path, sizes=[16, 32, 48, 64, 128, 256]):
    """生成多尺寸ICO文件"""
    images = []
    
    print("🎨 开始生成 SunEyeVision 图标...")
    print(f"📐 生成尺寸: {', '.join(map(str, sizes))} px")
    
    for size in sizes:
        print(f"   - 正在生成 {size}x{size} 图标...")
        image = create_icon(size)
        images.append(image)
    
    # 保存为ICO文件
    print(f"💾 保存到: {output_path}")
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
    # 设置输出路径
    icon_dir = "src/UI/Icons"
    os.makedirs(icon_dir, exist_ok=True)
    
    icon_path = os.path.join(icon_dir, "solution.ico")
    preview_path = os.path.join(icon_dir, "solution_preview.png")
    
    # 生成图标
    generate_ico_file(icon_path)
    
    # 生成预览图
    generate_preview_png(preview_path)
    
    print("\n🎉 全部完成！")
    print(f"   📁 图标文件: {icon_path}")
    print(f"   🖼️  预览图片: {preview_path}")
    print("\n💡 现在您可以在文件资源管理器中预览 .solution 文件的图标了！")

if __name__ == "__main__":
    main()
