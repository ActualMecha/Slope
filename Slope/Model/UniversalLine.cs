using System;
using System.Collections.Generic;
using System.Linq;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace Slope.Model
{
    public abstract class UniversalLine : IDisposable
    {
        public abstract Point3d StartPoint { get; }
        public abstract Point3d EndPoint { get; }
        public abstract bool VisuallyClosed { get; }
        public abstract IReadOnlyCollection<UniversalLineSegment> Segments { get; }

        public abstract void Hightlight();
        public abstract void Unhighlight();
        public abstract void Reverse();
        public abstract void Dispose();
        public abstract Vector3d GetFirstDerivative(PointOnLine pointOnLine);
        public abstract bool Intersects(Entity entity, int allowedCount);
        public abstract Point3dCollection IntersectWith(Entity entity, Intersect intersectType);

        public bool ChopLine(Line line, bool extend)
        {
            var forward = line.StartPoint.GetVectorTo(line.EndPoint);
            using (var intersection = IntersectWith(line, extend ? Intersect.ExtendArgument : Intersect.OnBothOperands))
            {
                Point3d? first = null;
                var shortestLength = double.PositiveInfinity;
                for (var i = 0; i < intersection.Count; ++i)
                {
                    var point = intersection[i];
                    if (line.StartPoint.GetVectorTo(point).DotProduct(forward) < 0)
                        continue;
                    var length = line.StartPoint.GetVectorTo(point).LengthSqrd;
                    if (length < shortestLength)
                    {
                        shortestLength = length;
                        first = point;
                    }
                }
                if (first.HasValue)
                    line.EndPoint = first.Value;
                return first.HasValue;
            }
        }
        public Side GetSide(UniversalLine otherLine)
        {
            if (VisuallyClosed || otherLine.VisuallyClosed)
                return GetSideClosedCase(otherLine);
            else
                return GetSideOpenedCase(otherLine);
        }

        private Side GetSideClosedCase(UniversalLine other)
        {
            Point3d getPointMaxY(UniversalLine line) =>
                line.Segments
                    .Select(segment => segment.EndPoint)
                    .Aggregate(line.StartPoint, (p1, p2) => p1.Y > p2.Y ? p1 : p2);

            Side GetTurnDirectionAt(UniversalLine line, Point3d point)
            {
                Point3d previous = default(Point3d), next = default(Point3d);
                bool hasNext = false, hasPrevious = false;
                foreach (var segment in Segments)
                {
                    if (point == segment.StartPoint)
                    {
                        next = segment.EndPoint;
                        hasNext = true;
                        if (hasPrevious)
                            break;
                    }
                    if (point == segment.EndPoint)
                    {
                        previous = segment.StartPoint;
                        hasPrevious = true;
                        if (hasNext)
                            break;
                    }
                }

                if (!hasNext || !hasPrevious)
                    throw new InvalidOperationException("Bottom line is strange");

                return previous.GetVectorTo(point).GetSide(point.GetVectorTo(next));
            }

            var pointMaxY = getPointMaxY(this);
            var isOutside = pointMaxY.Y > getPointMaxY(other).Y;
            var turnDirection = GetTurnDirectionAt(this, pointMaxY);
            return isOutside ? turnDirection : (Side)(-(int)turnDirection);
        }

        private Side GetSideOpenedCase(UniversalLine otherLine)
        {
            bool codirectional(UniversalLine line, UniversalLine other)
            {
                using (var connectedFronts = new Line(line.StartPoint, other.StartPoint))
                using (var connectedEnds = new Line(line.EndPoint, other.EndPoint))
                {
                    return !connectedFronts.Intersects(connectedEnds, 0) &&
                        !line.Intersects(connectedFronts, 1) &&
                        !other.Intersects(connectedFronts, 1) &&
                        !line.Intersects(connectedEnds, 1) &&
                        !other.Intersects(connectedEnds, 1);
                }
            }

            var thisSegment = Segments.First();
            var otherSegment = codirectional(this, otherLine)
                ? otherLine.Segments.First()
                : otherLine.Segments.Last();

            if (thisSegment.StartPoint == otherSegment.StartPoint)
                return thisSegment.StartPoint.GetVectorTo(thisSegment.EndPoint).GetSide(
                    otherSegment.StartPoint.GetVectorTo(otherSegment.EndPoint));
            else if (thisSegment.StartPoint == otherSegment.EndPoint)
                return thisSegment.StartPoint.GetVectorTo(thisSegment.EndPoint).GetSide(
                    otherSegment.EndPoint.GetVectorTo(otherSegment.StartPoint));

            var tangent = thisSegment.StartPoint.GetVectorTo(thisSegment.EndPoint);
            var normal = new Vector3d(tangent.Y, -tangent.X, 0);
            using (var normalLine = new Line(thisSegment.StartPoint, thisSegment.StartPoint + normal))
            using (var otherLineSegment = new Line(otherSegment.StartPoint, otherSegment.EndPoint))
            using (var intersection = new Point3dCollection())
            {
                normalLine.IntersectWith(otherLineSegment, Intersect.ExtendBoth, intersection, IntPtr.Zero, IntPtr.Zero);
                if (intersection.Count == 0)
                    throw new InvalidOperationException("Top line does not intersect bottom's normal");
                return tangent.GetSide(thisSegment.StartPoint.GetVectorTo(intersection[0]));
            }

        }
    }
}
