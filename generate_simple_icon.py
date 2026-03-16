from PIL import Image, ImageDraw
import math
import os

def create_suneye_icon(size):
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    c = size // 2
    
    # Background
    m = max(4, size // 16)
    draw.rectangle([m, m, size-m, size-m], fill='#f8f9fa', outline='#3498db', width=2)
    
    # Sun core
    sr = size // 8
    for r in range(sr, 0, -1):
        ratio = r / sr
        color = (
            int(255 * ratio + 243 * (1-ratio)),
            int(200 * ratio + 156 * (1-ratio)),
            int(50 * ratio + 18 * (1-ratio))
        )
        draw.ellipse([c-r, c-r, c+r, c+r], fill=color)
    
    # Sun rays
    for i in range(12):
        angle = math.radians(i * 30)
        sx = c + math.cos(angle) * (sr + 2)
        sy = c + math.sin(angle) * (sr + 2)
        ex = c + math.cos(angle) * (sr + sr//3)
        ey = c + math.sin(angle) * (sr + sr//3)
        draw.line([sx, sy, ex, ey], fill='#f39c12', width=2)
    
    # Eye
    es = size // 3
    ey_h = es // 2
    draw.ellipse([c-es, c-ey_h, c+es, c+ey_h], fill='white', outline='#2980b9', width=2)
    
    # Iris
    ir = int(ey_h * 0.7)
    draw.ellipse([c-ir, c-ir, c+ir, c+ir], fill='#3498db')
    
    # Pupil
    pr = ir // 3
    draw.ellipse([c-pr, c-pr, c+pr, c+pr], fill='#1a1a2e')
    
    # Highlight
    draw.ellipse([c-pr-2, c-pr-2, c-pr+2, c-pr+2], fill='white')
    
    # Corner decorations
    cs = size // 6
    for (x, y, dx, dy) in [(m,m,1,1), (size-m,m,-1,1), (m,size-m,1,-1), (size-m,size-m,-1,-1)]:
        draw.line([x, y, x+cs*dx, y], fill='#3498db', width=2)
        draw.line([x, y, x, y+cs*dy], fill='#3498db', width=2)
    
    return img

# Generate icons
os.makedirs('src/UI/Icons', exist_ok=True)

print('Generating SunEyeVision icons...')
sizes = [16, 32, 48, 64, 128, 256]
images = [create_suneye_icon(s) for s in sizes]

# Save ICO
images[0].save('src/UI/Icons/solution.ico', format='ICO', 
               sizes=[(s, s) for s in sizes],
               append_images=images[1:])

# Save preview
create_suneye_icon(256).save('src/UI/Icons/solution_preview.png', 'PNG')

print('Done! Icons saved to src/UI/Icons/')
