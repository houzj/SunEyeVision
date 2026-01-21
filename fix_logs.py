import os
import re

def fix_log_calls(directory):
    """修复目录中所有CS文件的Logger方法调用"""
    for root, dirs, files in os.walk(directory):
        for file in files:
            if file.endswith('.cs'):
                filepath = os.path.join(root, file)
                with open(filepath, 'r', encoding='utf-8') as f:
                    content = f.read()

                # 替换日志方法调用
                content = re.sub(r'\.Info\(', '.LogInfo(', content)
                content = re.sub(r'\.Warning\(', '.LogWarning(', content)
                content = re.sub(r'\.Error\(', '.LogError(', content)
                content = re.sub(r'\.Debug\(', '.LogDebug(', content)

                # 替换CreateFailure为CreateError
                content = re.sub(r'\.CreateFailure\(', '.CreateError(', content)

                with open(filepath, 'w', encoding='utf-8') as f:
                    f.write(content)

                print(f"已修复: {filepath}")

if __name__ == '__main__':
    # 修复DeviceDriver
    fix_log_calls(r'VisionMaster.DeviceDriver')
    # 修复Workflow
    fix_log_calls(r'VisionMaster.Workflow')
    print("所有文件修复完成!")
