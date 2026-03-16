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

# 生成所有尺寸
sizes = [16, 32, 48, 64, 128, 256]
images = [create_icon(s) for s in sizes]

# 保存 ICO
ico_path = 'src/UI/Icons/solution.ico'
images[0].save(
    ico_path,
    format='ICO',
    sizes=[(s, s) for s in sizes],
    append_images=images[1:]
)

# 保存预览
preview_path = 'src/UI/Icons/solution_preview.png'
images[-1].save(preview_path, 'PNG')

# 检查文件大小
ico_size = os.path.getsize(ico_path)
preview_size = os.path.getsize(preview_path)

print(f'ICO file: {ico_size} bytes ({ico_size/1024:.2f} KB)')
print(f'Preview file: {preview_size} bytes ({preview_size/1024:.2f} KB)')
print('Done!')
