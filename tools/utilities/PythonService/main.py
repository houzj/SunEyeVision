"""
VisionMaster Python深度学习服务
提供PyTorch模型推理的HTTP API接口
"""

from flask import Flask, request, jsonify
from flask_cors import CORS
import torch
import numpy as np
from PIL import Image
import io
import base64
import logging
from typing import Dict, List, Optional

# 配置日志
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

app = Flask(__name__)
CORS(app)  # 允许跨域请求


class ModelManager:
    """模型管理器"""

    def __init__(self):
        self.models: Dict[str, torch.nn.Module] = {}
        logger.info("ModelManager initialized")

    def load_model(self, model_name: str, model_path: str, model_type: str = "yolo") -> bool:
        """
        加载模型

        Args:
            model_name: 模型名称
            model_path: 模型路径
            model_type: 模型类型 (yolo, segmentation, ocr等)

        Returns:
            是否加载成功
        """
        try:
            if model_type == "yolo":
                # 加载YOLO模型示例
                # 注意: 实际使用时需要根据具体YOLO版本调整
                from ultralytics import YOLO
                model = YOLO(model_path)
            else:
                # 通用模型加载
                model = torch.load(model_path)
                model.eval()

            self.models[model_name] = {
                "model": model,
                "type": model_type,
                "path": model_path
            }

            logger.info(f"Model {model_name} loaded successfully from {model_path}")
            return True

        except Exception as e:
            logger.error(f"Failed to load model {model_name}: {str(e)}")
            return False

    def get_model(self, model_name: str) -> Optional[Dict]:
        """获取模型"""
        return self.models.get(model_name)

    def list_models(self) -> List[str]:
        """列出所有已加载的模型"""
        return list(self.models.keys())


# 全局模型管理器
model_manager = ModelManager()


@app.route('/api/health', methods=['GET'])
def health_check():
    """健康检查接口"""
    return jsonify({
        "status": "healthy",
        "models_loaded": len(model_manager.list_models()),
        "models": model_manager.list_models()
    })


@app.route('/api/models/load', methods=['POST'])
def load_model():
    """
    加载模型接口

    Request:
        {
            "model_name": "yolov8n",
            "model_path": "models/yolov8n.pt",
            "model_type": "yolo"
        }
    """
    try:
        data = request.json
        model_name = data.get("model_name")
        model_path = data.get("model_path")
        model_type = data.get("model_type", "yolo")

        if not model_name or not model_path:
            return jsonify({"error": "model_name and model_path are required"}), 400

        success = model_manager.load_model(model_name, model_path, model_type)

        if success:
            return jsonify({
                "message": f"Model {model_name} loaded successfully",
                "models": model_manager.list_models()
            })
        else:
            return jsonify({"error": f"Failed to load model {model_name}"}), 500

    except Exception as e:
        logger.error(f"Error loading model: {str(e)}")
        return jsonify({"error": str(e)}), 500


@app.route('/api/models/list', methods=['GET'])
def list_models():
    """列出所有已加载的模型"""
    return jsonify({
        "models": model_manager.list_models(),
        "count": len(model_manager.list_models())
    })


@app.route('/api/inference/object_detection', methods=['POST'])
def object_detection():
    """
    目标检测接口

    Request:
        {
            "model_name": "yolov8n",
            "image": "base64_encoded_image",
            "confidence": 0.5,
            "iou": 0.45
        }
    """
    try:
        data = request.json
        model_name = data.get("model_name")
        image_base64 = data.get("image")
        confidence = data.get("confidence", 0.5)
        iou = data.get("iou", 0.45)

        # 检查模型是否存在
        model_info = model_manager.get_model(model_name)
        if not model_info:
            return jsonify({"error": f"Model {model_name} not found"}), 404

        # 解码图像
        image_bytes = base64.b64decode(image_base64)
        image = Image.open(io.BytesIO(image_bytes))

        # 转换为numpy数组
        image_np = np.array(image)

        # 执行推理
        model = model_info["model"]

        if model_info["type"] == "yolo":
            results = model.predict(
                image_np,
                conf=confidence,
                iou=iou,
                verbose=False
            )

            # 解析结果
            detections = []
            for result in results:
                boxes = result.boxes
                for box in boxes:
                    x1, y1, x2, y2 = box.xyxy[0].cpu().numpy()
                    conf = box.conf[0].cpu().numpy()
                    cls = int(box.cls[0].cpu().numpy())
                    label = model.names[cls]

                    detections.append({
                        "class": label,
                        "confidence": float(conf),
                        "bbox": [float(x1), float(y1), float(x2), float(y2)]
                    })

            return jsonify({
                "success": True,
                "detections": detections,
                "count": len(detections)
            })
        else:
            return jsonify({"error": "Model type not supported for object detection"}), 400

    except Exception as e:
        logger.error(f"Error in object detection: {str(e)}")
        return jsonify({"error": str(e)}), 500


@app.route('/api/inference/segmentation', methods=['POST'])
def image_segmentation():
    """
    图像分割接口

    Request:
        {
            "model_name": "segmentation_model",
            "image": "base64_encoded_image"
        }
    """
    try:
        data = request.json
        model_name = data.get("model_name")
        image_base64 = data.get("image")

        # 检查模型是否存在
        model_info = model_manager.get_model(model_name)
        if not model_info:
            return jsonify({"error": f"Model {model_name} not found"}), 404

        # 解码图像
        image_bytes = base64.b64decode(image_base64)
        image = Image.open(io.BytesIO(image_bytes))
        image_np = np.array(image)

        # TODO: 实现分割逻辑
        # 这里需要根据具体的分割模型实现推理逻辑

        return jsonify({
            "success": True,
            "message": "Segmentation completed",
            "note": "Segmentation implementation pending"
        })

    except Exception as e:
        logger.error(f"Error in segmentation: {str(e)}")
        return jsonify({"error": str(e)}), 500


@app.route('/api/inference/ocr', methods=['POST'])
def ocr():
    """
    OCR文字识别接口

    Request:
        {
            "model_name": "ocr_model",
            "image": "base64_encoded_image"
        }
    """
    try:
        data = request.json
        model_name = data.get("model_name")
        image_base64 = data.get("image")

        # 检查模型是否存在
        model_info = model_manager.get_model(model_name)
        if not model_info:
            return jsonify({"error": f"Model {model_name} not found"}), 404

        # TODO: 实现OCR逻辑
        # 这里需要根据具体的OCR模型实现推理逻辑

        return jsonify({
            "success": True,
            "message": "OCR completed",
            "note": "OCR implementation pending"
        })

    except Exception as e:
        logger.error(f"Error in OCR: {str(e)}")
        return jsonify({"error": str(e)}), 500


def main():
    """主函数"""
    logger.info("Starting VisionMaster Python Service...")
    logger.info("Flask server will be available at http://localhost:5000")

    # 开发模式
    app.run(host='0.0.0.0', port=5000, debug=True)


if __name__ == '__main__':
    main()
