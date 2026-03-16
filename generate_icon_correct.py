from PIL import Image, ImageDraw
import math
import os

def create_suneye_icon(size):
    """创建 SunEyeVision 图标"""
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    c = size // 2
    
    # 背景 - 圆角矩形
    m = max(4, size // 16)
    bg_rect = [m, m, size - m, size - m]
    
    # 绘制背景矩形（简单版，不使用圆角）
    draw.rectangle(bg_rect, fill='#f8f9fa', outline='#3498db', width=max(1, size // 32))
    
    # 太阳核心
    sr = size // 8
    
    # 太阳渐变（多层圆形）
    for r in range(sr, 0, -1):
        ratio = r / sr
        color = (
            int(255 * ratio + 243 * (1-ratio)),
            int(200 * ratio + 156 * (1-ratio)),
            int(50 * ratio + 18 * (1-ratio))
        )
        draw.ellipse([c-r, c-r, c+r, c+r], fill=color)
    
    # 太阳光芒
    ray_count = 12
    ray_length = max(2, sr // 2)
    for i in range(ray_count):
        angle = math.radians(i * 30)
        sx = c + math.cos(angle) * (sr + 2)
        sy = c + math.sin(angle) * (sr + 2)
        ex = c + math.cos(angle) * (sr + ray_length)
        ey = c + math.sin(angle) * (sr + ray_length)
        width = max(1, size // 64)
        draw.line([sx, sy, ex, ey], fill='#f39c12', width=width)
    
    # 眼睛
    es = size // 3
    ey_h = es // 2
    
    # 眼白
    draw.ellipse([c-es, c-ey_h, c+es, c+ey_h], fill='white', outline='#2980b9', width=max(1, size // 64))
    
    # 虹膜
    ir = int(ey_h * 0.7)
    draw.ellipse([c-ir, c-ir, c+ir, c+ir], fill='#3498db')
    
    # 瞳孔
    pr = max(1, ir // 3)
    draw.ellipse([c-pr, c-pr, c+pr, c+pr], fill='#1a1a2e')
    
    # 高光
    hr = max(1, pr // 2)
    hx = c - pr - max(1, pr // 4)
    hy = c - pr - max(1, pr // 4)
    draw.ellipse([hx-hr, hy-hr, hx+hr, hy+hr], fill='white')
    
    # 四角装饰
    cs = size // 8
    lw = max(1, size // 64)
    for (x, y, dx, dy) in [(m,m,1,1), (size-m,m,-1,1), (m,size-m,1,-1), (size-m,size-m,-1,-1)]:
        draw.line([x, y, x+cs*dx, y], fill='#3498db', width=lw)
        draw.line([x, y, x, y+cs*dy], fill='#3498db', width=lw)
    
    # 底部文字（仅大尺寸）
    if size >= 64:
        try:
            from PIL import ImageFont
            font_size = max(8, size // 16)
            try:
                font = ImageFont.truetype("arial.ttf", font_size)
            except:
                font = ImageFont.load_default()
            
            text = "SEV"
            bbox = draw.textbbox((0, 0), text, font=font)
            text_w = bbox[2] - bbox[0]
            text_h = bbox[3] - bbox[1]
            
            text_x = c - text_w // 2
            text_y = size - m - text_h - max(2, size // 64)
            
            draw.text((text_x, text_y), text, fill='#2c3e50', font=font)
        except:
            pass
    
    return img

def generate_icons():
    """生成图标"""
    print("=" * 60)
    print("  SunEyeVision Icon Generator (Corrected)")
    print("=" * 60)
    print()
    
    # 创建输出目录
    icon_dir = "src/UI/Icons"
    os.makedirs(icon_dir, exist_ok=True)
    
    # 生成尺寸列表
    sizes = [16, 32, 48, 64, 128, 256]
    
    print(f"📐 生成 {len(sizes)} 种尺寸的图标...")
    images = []
    
    for size in sizes:
        print(f"   ⚙️  {size:3d}x{size:3d}", end=" ... ")
        try:
            img = create_suneye_icon(size)
            images.append(img)
            print("✅")
        except Exception as e:
            print(f"❌ {e}")
            raise
    
    # 保存 ICO 文件
    print()
    print("💾 保存 ICO 文件...")
    ico_path = os.path.join(icon_dir, "solution.ico")
    
    # 使用 PIL 的 ICO 保存功能
    try:
        images[0].save(
            ico_path,
            format='ICO',
            sizes=[(img.width, img.height) for img in images],
            append_images=images[1:]
        )
        
        # 检查文件大小
        file_size = os.path.getsize(ico_path)
        print(f"   ✅ ICO 文件已保存: {ico_path}")
        print(f"   📊 文件大小: {file_size:,} 字节 ({file_size/1024:.2f} KB)")
        
        if file_size < 1000:
            print(f"   ⚠️  警告: 文件大小异常小，可能生成失败!")
            
    except Exception as e:
        print(f"   ❌ 保存失败: {e}")
        raise
    
    # 保存预览图
    print()
    print("💾 保存预览图...")
    preview_path = os.path.join(icon_dir, "solution_preview.png")
    images[-1].save(preview_path, 'PNG')
    preview_size = os.path.getsize(preview_path)
    print(f"   ✅ 预览图已保存: {preview_path}")
    print(f"   📊 文件大小: {preview_size:,} 字节 ({preview_size/1024:.2f} KB)")
    
    print()
    print("=" * 60)
    print("✅ 图标生成完成！")
    print("=" * 60)

if __name__ == "__main__":
    generate_icons()
