import os
import subprocess

# 执行编译
result = subprocess.run(
    ['dotnet', 'build', 'SunEyeVision.New.sln', '/p:Configuration=Debug', '/nologo', '/v:m'],
    cwd='d:/MyWork/SunEyeVision/SunEyeVision',
    capture_output=True,
    text=True
)

# 查找错误
for line in result.stdout.split('\n'):
    if 'error' in line.lower() and 'cs' in line:
        print(line)
