using System.Collections.Generic;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using System;

namespace Slope.Model
{
    public class UniversalLineSegment : UniversalLine
    {
        private readonly LineSegment3d ncadSegment;
        private readonly SimpleUniversalLine parent;

        private bool reversed;
        public double Length => ncadSegment.Length;

        public UniversalLineSegment(LineSegment3d segment, SimpleUniversalLine parent)
        {
            ncadSegment = segment;
            this.parent = parent;
        }

        public PointOnLine EvaluatePoint(double offset) =>
            new PointOnLine
            {
                Point = ncadSegment.EvaluatePoint(reversed ? 1.0 - offset : offset),
                Parent = parent
            };

        #region IUniversalLine

        public override Point3d StartPoint => reversed ? ncadSegment.EndPoint : ncadSegment.StartPoint;
        public override Point3d EndPoint => reversed ? ncadSegment.StartPoint : ncadSegment.EndPoint;

        public override bool VisuallyClosed => false;
        public override IReadOnlyCollection<UniversalLineSegment> Segments =>
            new List<UniversalLineSegment> { this }.AsReadOnly();

        public override void Hightlight()
        {
        }

        public override void Unhighlight()
        {
        }

        public override void Reverse()
        {
            reversed = true;
        }

        public void SetStartPoint(Point3d startPoint)
        {
            if (reversed)
                ncadSegment.Set(EndPoint, startPoint);
            else
                ncadSegment.Set(startPoint, EndPoint);
        }

        public void SetEndPoint(Point3d endPoint)
        {
            if (reversed)
                ncadSegment.Set(endPoint, StartPoint);
            else
                ncadSegment.Set(StartPoint, endPoint);
        }

        public override Vector3d GetFirstDerivative(PointOnLine pointOnLine) => ncadSegment.Direction;

        public override bool Intersects(Entity entity, int allowedCount)
        {
            using (var line = new Line(ncadSegment.StartPoint, ncadSegment.EndPoint))
            {
                return line.Intersects(entity, allowedCount);
            }
        }

        public override Point3dCollection IntersectWith(Entity entity, Intersect intersectType)
        {
            var intersection = new Point3dCollection();
            using (var line = new Line(ncadSegment.StartPoint, ncadSegment.EndPoint))
                line.IntersectWith(entity, intersectType, intersection, IntPtr.Zero, IntPtr.Zero);
            return intersection;
        }

        public Point3dCollection IntersectWith(UniversalLineSegment other, Intersect intersectType)
        {
            using (var otherLine = new Line(other.ncadSegment.StartPoint, other.ncadSegment.EndPoint))
            {
                return IntersectWith(otherLine, intersectType);
            }
        }

        #endregion

        #region IDisposable

        public override void Dispose() =>
            ncadSegment.Dispose();

        #endregion
    }
}
