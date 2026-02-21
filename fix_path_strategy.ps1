$filePath = 'src\UI\Services\PathCalculators\OrthogonalPathCalculator.cs'
$encoding = [System.Text.Encoding]::UTF8
$content = [System.IO.File]::ReadAllText($filePath, $encoding)

# 使用正则表达式匹配方法签名
$pattern = '(\s+/// <summary>\r?\n\s+/// 为同向场景选择策略（简化版本：不涉及碰撞检测）\r?\n\s+/// Left-Left, Right-Right, Top-Top, Bottom-Bottom\r?\n\s+/// </summary>\r?\n\s+private PathStrategy SelectStrategyForSameDirectionSimple\([^)]+\)\r?\n\s+\{)'

$match = [regex]::Match($content, $pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)

if ($match.Success) {
    Write-Host "Found match at index: $($match.Index)"
    Write-Host "Match length: $($match.Length)"
    
    # 找到方法体的结束位置
    $startIdx = $match.Index
    $braceStart = $content.IndexOf('{', $startIdx)
    $braceCount = 1
    $idx = $braceStart + 1
    
    while ($braceCount -gt 0 -and $idx -lt $content.Length) {
        if ($content[$idx] -eq '{') { $braceCount++ }
        elseif ($content[$idx] -eq '}') { $braceCount-- }
        $idx++
    }
    
    $endIdx = $idx
    Write-Host "Method body ends at index: $endIdx"
    
    # 提取原方法内容
    $originalMethod = $content.Substring($startIdx, $endIdx - $startIdx)
    
    # 新方法内容
    $newMethod = @'
        /// <summary>
        /// 为同向场景选择策略（优化版本：考虑节点相对位置）
        /// Left-Left, Right-Right, Top-Top, Bottom-Bottom
        /// 
        /// 优化原则：
        /// 1. 优先判断连线方向与端口朝向是否一致
        /// 2. 如果目标在端口朝向方向且端口朝向一致，使用简单路径
        /// 3. 只有在真正复杂的场景才使用多段路径
        /// </summary>
        private PathStrategy SelectStrategyForSameDirectionSimple(
            SceneComplexity sceneComplexity,
            GeometricAlignment geometricAlignment,
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double dx, double dy,
            double horizontalDistance,
            double verticalDistance,
            Rect sourceNodeRect,
            Rect targetNodeRect)
        {
            // 新增：检查是否是简单的相邻节点连接
            // 如果连线方向与端口朝向一致，即使同向端口也使用简单路径
            if (IsSimpleAdjacentConnection(sourceDirection, targetDirection, dx, dy, horizontalDistance, verticalDistance))
            {
                return SelectSimplestStrategy(sourceDirection, targetDirection, horizontalDistance, verticalDistance);
            }

            // 极简场景或简单场景：选择简单策略
            if (sceneComplexity == SceneComplexity.Direct || sceneComplexity == SceneComplexity.Simple)
            {
                var simpleStrategy = SelectSimplestStrategy(
                    sourceDirection, targetDirection, horizontalDistance, verticalDistance);
                
                return simpleStrategy;
            }

            // 新增：判断是否需要复杂路径
            // 如果两个节点位置关系简单，仍然使用简单策略
            if (IsSimpleRelativePosition(sourceDirection, targetDirection, dx, dy, horizontalDistance, verticalDistance))
            {
                return SelectSimplestStrategy(sourceDirection, targetDirection, horizontalDistance, verticalDistance);
            }

            // 默认：基于几何对齐选择
            if (geometricAlignment == GeometricAlignment.VerticalAligned && sourceDirection.IsHorizontal())
            {
                return PathStrategy.HorizontalFirst;
            }
            else if (geometricAlignment == GeometricAlignment.HorizontalAligned && sourceDirection.IsVertical())
            {
                return PathStrategy.VerticalFirst;
            }
            else
            {
                // 不对齐：使用三段式
                return PathStrategy.ThreeSegment;
            }
        }

        /// <summary>
        /// 判断是否是简单的相邻节点连接
        /// 条件：连线方向与端口朝向一致，且距离不远
        /// </summary>
        private bool IsSimpleAdjacentConnection(
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double dx, double dy,
            double horizontalDistance,
            double verticalDistance)
        {
            const double SimpleDistanceThreshold = 300.0; // 简单连接的距离阈值

            // 水平同向端口（Right-Right 或 Left-Left）
            if (sourceDirection == PortDirection.Right && targetDirection == PortDirection.Right)
            {
                // 目标在右侧，且距离不远 → 简单连接
                if (dx > 0 && horizontalDistance < SimpleDistanceThreshold)
                {
                    return true;
                }
            }
            else if (sourceDirection == PortDirection.Left && targetDirection == PortDirection.Left)
            {
                // 目标在左侧，且距离不远 → 简单连接
                if (dx < 0 && horizontalDistance < SimpleDistanceThreshold)
                {
                    return true;
                }
            }
            // 垂直同向端口（Bottom-Bottom 或 Top-Top）
            else if (sourceDirection == PortDirection.Bottom && targetDirection == PortDirection.Bottom)
            {
                // 目标在下方，且距离不远 → 简单连接
                if (dy > 0 && verticalDistance < SimpleDistanceThreshold)
                {
                    return true;
                }
            }
            else if (sourceDirection == PortDirection.Top && targetDirection == PortDirection.Top)
            {
                // 目标在上方，且距离不远 → 简单连接
                if (dy < 0 && verticalDistance < SimpleDistanceThreshold)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断节点相对位置是否简单（不需要复杂路径）
        /// </summary>
        private bool IsSimpleRelativePosition(
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double dx, double dy,
            double horizontalDistance,
            double verticalDistance)
        {
            const double SimpleDistanceThreshold = 200.0;

            // 对于水平端口：如果垂直距离不大，使用简单路径
            if (sourceDirection.IsHorizontal() && verticalDistance < SimpleDistanceThreshold)
            {
                return true;
            }

            // 对于垂直端口：如果水平距离不大，使用简单路径
            if (sourceDirection.IsVertical() && horizontalDistance < SimpleDistanceThreshold)
            {
                return true;
            }

            return false;
        }
'@

    # 替换内容
    $newContent = $content.Substring(0, $startIdx) + $newMethod + $content.Substring($endIdx)
    
    # 写入文件
    [System.IO.File]::WriteAllText($filePath, $newContent, $encoding)
    Write-Host "Successfully replaced the method!"
} else {
    Write-Host "Pattern not found"
}
