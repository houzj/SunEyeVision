from PIL import Image, ImageDraw
import math
import os

def create_icon(size):
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    c = size // 2
    m = max(4, size // 16)
    
    # 背景
    draw.rectangle([m, m, size-m, size-m], fill='#f8f9fa', outline='#3498db', width=max(1, size//32))
    
    # 太阳
    sr = size // 8
    for r in range(sr, 0, -1):
        ratio = r / sr
        draw.ellipse([c-r, c-r, c+r, c+r], fill=(
            int(255*ratio + 243*(1-ratio)),
            int(200*ratio + 156*(1-ratio)),
            int(50*ratio + 18*(1-ratio))
        ))
    
    # 光芒
    for i in range(12):
        angle = math.radians(i * 30)
        sx = c + math.cos(angle) * (sr + 2)
        sy = c + math.sin(angle) * (sr + 2)
        rl = max(2, sr//2)
        ex = c + math.cos(angle) * (sr + rl)
        ey = c + math.sin(angle) * (sr + rl)
        draw.line([sx, sy, ex, ey], fill='#f39c12', width=max(1, size//64))
    
    # 眼睛
    es = size // 3
    ey_h = es // 2
    draw.ellipse([c-es, c-ey_h, c+es, c+ey_h], fill='white', outline='#2980b9', width=max(1, size//64))
    
    ir = int(ey_h * 0.7)
    draw.ellipse([c-ir, c-ir, c+ir, c+ir], fill='#3498db')
    
    pr = max(1, ir//3)
    draw.ellipse([c-pr, c-pr, c+pr, c+pr], fill='#1a1a2e')
    
    hr = max(1, pr//2)
    draw.ellipse([c-pr-max(1,pr//4), c-pr-max(1,pr//4), c-pr+hr, c-pr+hr], fill='white')
    
    # 四角装饰
    cs = size // 8
    lw = max(1, size//64)
    for (x, y, dx, dy) in [(m,m,1,1), (size-m,m,-1,1), (m,size-m,1,-1), (size-m,size-m,-1,-1)]:
        draw.line([x, y, x+cs*dx, y], fill='#3498db', width=lw)
        draw.line([x, y, x, y+cs*dy], fill='#3498db', width=lw)
    
    return img

# 创建输出目录
os.makedirs('src/UI/Icons', exist_ok=True)

# 生成并保存单独的 ICO 文件（Windows 会自动选择最合适的）
sizes = [16, 32, 48, 64, 128, 256]
icons = {}

for size in sizes:
    img = create_icon(size)
    
    # 保存为单独的 ICO 文件
    ico_path = f'src/UI/Icons/solution_{size}x{size}.ico'
    img.save(ico_path, format='ICO')
    icons[size] = ico_path
    print(f'Created: {ico_path} ({os.path.getsize(ico_path)} bytes)')

# 创建一个主要的 48x48 ICO（这是 Windows 文件资源管理器的常用尺寸）
main_size = 48
main_img = create_icon(main_size)
main_path = 'src/UI/Icons/solution.ico'
main_img.save(main_path, format='ICO')
print(f'Main icon: {main_path} ({os.path.getsize(main_path)} bytes)')

# 保存预览
preview_path = 'src/UI/Icons/solution_preview.png'
create_icon(256).save(preview_path, 'PNG')
print(f'Preview: {preview_path} ({os.path.getsize(preview_path)} bytes)')

print('\nDone!')
