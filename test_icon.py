from PIL import Image, ImageDraw

# 创建一个简单的测试图标
size = 256
image = Image.new('RGBA', (size, size), (0, 0, 0, 0))
draw = ImageDraw.Draw(image)

# 绘制一个简单的圆形
draw.ellipse([50, 50, 206, 206], fill='#3498db', outline='#2980b9', width=3)

# 保存
image.save('test_icon.png')
print('Test icon created successfully!')
