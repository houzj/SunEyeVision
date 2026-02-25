# VisionMaster Python Service

VisionMaster机器视觉平台的深度学习服务模块,提供PyTorch模型推理的HTTP API接口。

## 功能特性

- 支持多种深度学习模型推理
  - 目标检测 (YOLO系列)
  - 图像分割
  - OCR文字识别
- RESTful API接口
- 模型热加载
- GPU加速支持

## 环境要求

- Python 3.8+
- CUDA 11.0+ (用于GPU加速)
- PyTorch 2.0+

## 安装步骤

### 1. 创建虚拟环境 (推荐)

```bash
python -m venv venv

# Windows
venv\Scripts\activate

# Linux/Mac
source venv/bin/activate
```

### 2. 安装依赖

```bash
pip install -r requirements.txt
```

### 3. 配置模型

编辑 `config.yaml` 文件,设置模型路径:

```yaml
models:
  yolo:
    path: models/yolov8n.pt
    enabled: true
```

### 4. 启动服务

```bash
python main.py
```

服务将在 `http://localhost:5000` 启动。

## API文档

### 健康检查

```http
GET /api/health
```

响应:
```json
{
  "status": "healthy",
  "models_loaded": 1,
  "models": ["yolov8n"]
}
```

### 加载模型

```http
POST /api/models/load
Content-Type: application/json

{
  "model_name": "yolov8n",
  "model_path": "models/yolov8n.pt",
  "model_type": "yolo"
}
```

### 列出模型

```http
GET /api/models/list
```

### 目标检测

```http
POST /api/inference/object_detection
Content-Type: application/json

{
  "model_name": "yolov8n",
  "image": "base64_encoded_image_string",
  "confidence": 0.5,
  "iou": 0.45
}
```

响应:
```json
{
  "success": true,
  "detections": [
    {
      "class": "person",
      "confidence": 0.95,
      "bbox": [100, 200, 300, 400]
    }
  ],
  "count": 1
}
```

### 图像分割

```http
POST /api/inference/segmentation
Content-Type: application/json

{
  "model_name": "segmentation_model",
  "image": "base64_encoded_image_string"
}
```

### OCR识别

```http
POST /api/inference/ocr
Content-Type: application/json

{
  "model_name": "ocr_model",
  "image": "base64_encoded_image_string"
}
```

## 模型准备

### YOLO模型

下载预训练模型:

```bash
# YOLOv8n (nano)
wget https://github.com/ultralytics/assets/releases/download/v0.0.0/yolov8n.pt -O models/yolov8n.pt

# YOLOv8s (small)
wget https://github.com/ultralytics/assets/releases/download/v0.0.0/yolov8s.pt -O models/yolov8s.pt
```

### 自定义模型训练

训练自定义模型:

```python
from ultralytics import YOLO

# 加载预训练模型
model = YOLO('yolov8n.pt')

# 训练
model.train(data='data.yaml', epochs=100, imgsz=640)

# 导出
model.export(format='onnx')
```

## C# 调用示例

```csharp
using System.Net.Http;
using System.Text;
using System.Text.Json;

public class PythonServiceClient
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://localhost:5000/api";

    public async Task<List<DetectionResult>> DetectObjectsAsync(
        string imageName,
        byte[] imageBytes)
    {
        var imageBase64 = Convert.ToBase64String(imageBytes);

        var request = new
        {
            model_name = "yolov8n",
            image = imageBase64,
            confidence = 0.5,
            iou = 0.45
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(
            $"{BaseUrl}/inference/object_detection", content);

        var result = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<DetectionResponse>(result).Detections;
    }
}
```

## 性能优化

1. **GPU加速**: 确保安装了CUDA版本的PyTorch
2. **批处理**: 设置合理的 `batch_size` 提高吞吐量
3. **模型量化**: 使用量化后的模型减少内存占用
4. **多进程**: 调整 `max_workers` 参数

## 故障排查

### 模型加载失败

- 检查模型路径是否正确
- 确认模型文件格式
- 查看日志文件 `logs/service.log`

### CUDA相关错误

- 检查CUDA版本是否匹配
- 确认GPU驱动是否安装正确
- 尝试使用CPU模式 (设置 `use_gpu: false`)

### 端口占用

修改 `config.yaml` 中的端口配置:

```yaml
server:
  port: 5001
```

## 许可证

MIT License

## 联系方式

如有问题,请提交Issue或联系开发团队。
