using System;

namespace SunEyeVision.Plugin.SDK.Extensions
{
    /// <summary>
    /// 角度系统转换工具类
    /// 提供数学角度系统和OpenCV角度系统之间的转换
    /// </summary>
    /// <remarks>
    /// <para>数学角度系统：逆时针为正，从X轴正方向开始测量，范围[-180°, 180°]</para>
    /// <para>OpenCV角度系统：顺时针为正，从X轴正方向开始测量，范围[0°, 360°)或(-90°, 90°]</para>
    /// </remarks>
    public static class AngleConverter
    {
        /// <summary>
        /// 数学角度转OpenCV角度
        /// </summary>
        /// <param name="mathAngle">数学角度（逆时针为正，范围[-180°, 180°]）</param>
        /// <returns>OpenCV角度（顺时针为正，范围[0°, 360°)）</returns>
        public static double MathToOpenCV(double mathAngle)
        {
            // 数学角度取负得到OpenCV角度
            var openCVAngle = -mathAngle;
            return NormalizeAngle(openCVAngle, 0, 360);
        }

        /// <summary>
        /// OpenCV角度转数学角度
        /// </summary>
        /// <param name="openCVAngle">OpenCV角度（顺时针为正，范围[0°, 360°)）</param>
        /// <returns>数学角度（逆时针为正，范围[-180°, 180°]）</returns>
        public static double OpenCVToMath(double openCVAngle)
        {
            // OpenCV角度取负得到数学角度
            var mathAngle = -openCVAngle;
            return NormalizeAngle(mathAngle, -180, 180);
        }

        /// <summary>
        /// 将角度规范化到指定范围
        /// </summary>
        /// <param name="angle">任意角度值</param>
        /// <param name="min">范围下限（包含）</param>
        /// <param name="max">范围上限（不包含）</param>
        /// <returns>规范化后的角度</returns>
        public static double NormalizeAngle(double angle, double min, double max)
        {
            var range = max - min;
            while (angle < min) angle += range;
            while (angle >= max) angle -= range;
            return angle;
        }

        /// <summary>
        /// 将角度规范化到数学角度范围[-180°, 180°]
        /// </summary>
        /// <param name="angle">任意角度值</param>
        /// <returns>规范化后的数学角度</returns>
        public static double NormalizeToMathRange(double angle)
        {
            return NormalizeAngle(angle, -180, 180);
        }

        /// <summary>
        /// 将角度规范化到OpenCV范围[0°, 360°)
        /// </summary>
        /// <param name="angle">任意角度值</param>
        /// <returns>规范化后的OpenCV角度</returns>
        public static double NormalizeToOpenCVRange(double angle)
        {
            return NormalizeAngle(angle, 0, 360);
        }

        /// <summary>
        /// 将角度规范化到OpenCV旋转矩形范围(-90°, 90°]
        /// </summary>
        /// <param name="angle">任意角度值</param>
        /// <returns>规范化后的OpenCV旋转矩形角度</returns>
        public static double NormalizeToRotatedRectRange(double angle)
        {
            // 先模360得到[0, 360)
            angle = angle % 360;
            if (angle < 0) angle += 360;

            // 转换到(-90°, 90°]
            if (angle > 270)
                angle -= 360;
            else if (angle > 90)
                angle -= 180;

            return angle;
        }

        /// <summary>
        /// 角度转弧度
        /// </summary>
        /// <param name="degrees">角度值</param>
        /// <returns>弧度值</returns>
        public static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        /// <summary>
        /// 弧度转角度
        /// </summary>
        /// <param name="radians">弧度值</param>
        /// <returns>角度值</returns>
        public static double ToDegrees(double radians)
        {
            return radians * 180.0 / Math.PI;
        }
    }
}
