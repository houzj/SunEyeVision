#pragma once

// C++ 标准库
#include <cmath>
#include <algorithm>

// C++/CLI 必需的头文件
#include <vcclr.h>

// 托管类型必需
using namespace System;
using namespace System::Collections::Generic;

namespace SunEyeVision {

    namespace LibavoidWrapper {

        // Managed point structure
        public value struct ManagedPoint
        {
        public:
            double X;
            double Y;

            ManagedPoint(double x, double y)
            {
                X = x;
                Y = y;
            }

            static ManagedPoint operator+(ManagedPoint p1, ManagedPoint p2)
            {
                return ManagedPoint(p1.X + p2.X, p1.Y + p2.Y);
            }

            static ManagedPoint operator-(ManagedPoint p1, ManagedPoint p2)
            {
                return ManagedPoint(p1.X - p2.X, p1.Y - p2.Y);
            }

            static double Distance(ManagedPoint p1, ManagedPoint p2)
            {
                return std::sqrt(std::pow(p2.X - p1.X, 2) + std::pow(p2.Y - p1.Y, 2));
            }
        };

        // Managed rectangle structure
        public value struct ManagedRect
        {
        public:
            double X;
            double Y;
            double Width;
            double Height;

            ManagedRect(double x, double y, double width, double height)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }

            property double Left
            {
                double get() { return X; }
            }

            property double Top
            {
                double get() { return Y; }
            }

            property double Right
            {
                double get() { return X + Width; }
            }

            property double Bottom
            {
                double get() { return Y + Height; }
            }

            bool ContainsPoint(ManagedPoint point)
            {
                return point.X >= Left && point.X <= Right &&
                       point.Y >= Top && point.Y <= Bottom;
            }

            bool IntersectsWith(ManagedRect other)
            {
                return !(Right < other.Left || Left > other.Right ||
                         Bottom < other.Top || Top > other.Bottom);
            }
        };

        // Port direction enumeration
        public enum class PortDirection
        {
            Top = 0,
            Bottom = 1,
            Left = 2,
            Right = 3,
            None = 4
        };

        // Forward declarations
        ref class RouterConfiguration;
        ref class RoutingResult;
        ref class LibavoidRouter;

        // Router configuration class
        public ref class RouterConfiguration
        {
        public:
            RouterConfiguration()
            {
                IdealSegmentLength = 50.0;
                SegmentPenalty = 0.0;
                RegionPenalty = 0.0;
                CrossingPenalty = 0.0;
                FixedSharedPathPenalty = 0.0;
                PortDirectionPenalty = 0.0;
                UseOrthogonalRouting = true;
                ImproveHyperedges = true;
                RoutingTimeLimit = 5000;
            }

            property double IdealSegmentLength;
            property double SegmentPenalty;
            property double RegionPenalty;
            property double CrossingPenalty;
            property double FixedSharedPathPenalty;
            property double PortDirectionPenalty;
            property bool UseOrthogonalRouting;
            property bool ImproveHyperedges;
            property int RoutingTimeLimit;
        };

        // Routing result class
        public ref class RoutingResult
        {
        public:
            RoutingResult()
            {
                PathPoints = gcnew List<ManagedPoint>();
                Success = false;
                ErrorMessage = "";
                Iterations = 0;
            }

            property List<ManagedPoint>^ PathPoints;
            property bool Success;
            property String^ ErrorMessage;
            property int Iterations;
        };

        // Main libavoid router wrapper class
        public ref class LibavoidRouter
        {
        private:
            RouterConfiguration^ config;
            List<RoutingResult^>^ cachedResults;

            // Internal helper methods
            bool PointInRect(ManagedPoint point, ManagedRect rect);
            bool LineIntersectsRect(ManagedPoint p1, ManagedPoint p2, ManagedRect rect);
            ManagedPoint GetPortPosition(ManagedRect rect, PortDirection direction);
            ManagedPoint AdjustPointForDirection(ManagedPoint point, PortDirection direction, double offset);
            List<ManagedPoint>^ CalculateOrthogonalPathInternal(
                ManagedPoint source,
                ManagedPoint target,
                PortDirection sourceDir,
                PortDirection targetDir,
                List<ManagedRect>^ obstacles);
            List<ManagedPoint>^ ApplyNodeAvoidance(
                List<ManagedPoint>^ path,
                List<ManagedRect>^ obstacles);
            bool PathIntersectsObstacles(List<ManagedPoint>^ path, List<ManagedRect>^ obstacles);

        public:
            LibavoidRouter();
            LibavoidRouter(RouterConfiguration^ configuration);
            ~LibavoidRouter();
            !LibavoidRouter();

            // Main routing methods
            RoutingResult^ RoutePath(
                ManagedPoint source,
                ManagedPoint target,
                PortDirection sourceDirection,
                PortDirection targetDirection,
                ManagedRect sourceRect,
                ManagedRect targetRect,
                List<ManagedRect>^ obstacles);

            // Batch routing methods
            List<RoutingResult^>^ RouteMultiplePaths(
                List<Tuple<ManagedPoint, ManagedPoint, PortDirection, PortDirection>^>^ connections,
                List<ManagedRect>^ nodes);

            // Clear cache
            void ClearCache();
        };

    } // namespace LibavoidWrapper

} // namespace SunEyeVision
