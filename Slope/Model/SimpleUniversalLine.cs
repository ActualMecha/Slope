using System;
using System.Collections.Generic;
using System.Linq;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace Slope.Model
{
    public class SimpleUniversalLine : UniversalLine
    {
        private readonly Curve ncadLine;
        private List<UniversalCurveSegment> segments;
        private bool reversed;

        public override IReadOnlyCollection<UniversalCurveSegment> Segments => segments.AsReadOnly();
        public override Point3d StartPoint => Segments.First().StartPoint;
        public override Point3d EndPoint => Segments.Last().EndPoint;
        public override bool VisuallyClosed { get; }

        public SimpleUniversalLine(Curve curve)
        {
            ncadLine = curve;
            switch (curve)
            {
                case Polyline polyline:
                    segments = ExtractSegments(polyline);
                    VisuallyClosed = polyline.Closed || StartPoint == EndPoint;
                    break;

                case Line line:
                    segments = new List<UniversalCurveSegment>
                    {
                        new UniversalLineSegment(new LineSegment3d(line.StartPoint, line.EndPoint), this)
                    };
                    VisuallyClosed = false;
                    break;

                case Arc arc:
                    segments = new List<UniversalCurveSegment>
                    {
                        new UniversalArcSegment(arc, this)
                    };
                    VisuallyClosed = arc.Closed || StartPoint == EndPoint;
                    break;

                default:
                    ncadLine.Dispose();
                    throw new NotSupportedException($"{curve.GetType().Name} is not supported");
            }
        }

        public override void Reverse()
        {
            reversed = !reversed;
            segments.Reverse();
            segments.ForEach(segment => segment.Reverse());
        }

        public override bool Intersects(Entity entity, int allowedCount) =>
            ncadLine.Intersects(entity, allowedCount);

        public override void Hightlight() => ncadLine.Highlight();

        public override void Unhighlight() => ncadLine.Unhighlight();

        public override Point3dCollection IntersectWith(Entity entity, Intersect intersectType)
        {
            var intersection = new Point3dCollection();
            ncadLine.IntersectWith(entity, intersectType, intersection, IntPtr.Zero, IntPtr.Zero);
            return intersection;
        }

        public override void Dispose()
        {
            foreach (var segment in Segments)
                segment.Dispose();
            segments.Clear();
            ncadLine.Dispose();
        }

        private List<UniversalCurveSegment> ExtractSegments(Polyline line) =>
            Enumerable.Range(0, line.NumberOfVertices - (line.Closed ? 0 : 1))
            .Select(i => line.GetLineSegmentAt(i))
            .Select(segment => (UniversalCurveSegment) new UniversalLineSegment(segment, this))
            .ToList();

        public override Vector3d GetFirstDerivative(PointOnLine pointOnLine) =>
            ncadLine.GetFirstDerivative(pointOnLine.Point) * (reversed ? -1.0 : 1.0);
    }
}
