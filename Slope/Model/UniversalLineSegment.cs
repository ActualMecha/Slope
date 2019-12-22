using System.Collections.Generic;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using System;

namespace Slope.Model
{
    public abstract class UniversalCurveSegment : UniversalLine
    {
        protected bool Reversed;

        public override Point3d StartPoint => Reversed ? OriginalEndPoint : OriginalStartPoint;
        public override Point3d EndPoint => Reversed ? OriginalStartPoint : OriginalEndPoint;
        public override bool VisuallyClosed => StartPoint == EndPoint;
        public override IReadOnlyCollection<UniversalCurveSegment> Segments =>
            new List<UniversalCurveSegment> { this }.AsReadOnly();
        public abstract PointOnLine EvaluatePoint(double offset);
        public abstract Vector3d GetFirstDerivative(double offset);
        public abstract double Length { get; }

        protected abstract Point3d OriginalStartPoint { get; }
        protected abstract Point3d OriginalEndPoint { get; }
        protected readonly SimpleUniversalLine Parent;

        public override void Reverse()
        {
            Reversed = !Reversed;
        }

        public override void Hightlight()
        {
        }

        public override void Unhighlight()
        {
        }
        
        public UniversalCurveSegment(SimpleUniversalLine parent)
        {
            Parent = parent;
        }
    }

    public class UniversalArcSegment : UniversalCurveSegment
    {
        private readonly Arc ncadArc;
        protected override Point3d OriginalStartPoint => ncadArc.StartPoint;
        protected override Point3d OriginalEndPoint => ncadArc.EndPoint;

        public override double Length => ncadArc.Length;

        public UniversalArcSegment(Arc arc, SimpleUniversalLine parent) : base(parent)
        {
            ncadArc = arc;
        }

        public override Vector3d GetFirstDerivative(double offset)
        {
            var trueOffset = Reversed ? 1.0 - offset : offset;
            return ncadArc.GetFirstDerivative((1.0 - trueOffset) * ncadArc.StartParam + trueOffset * ncadArc.EndParam);
        }

        public override Vector3d GetFirstDerivative(PointOnLine pointOnLine) =>
            ncadArc.GetFirstDerivative(pointOnLine.Point);

        public override bool Intersects(Entity entity, int allowedCount) =>
            ncadArc.Intersects(entity, allowedCount);

        public override Point3dCollection IntersectWith(Entity entity, Intersect intersectType)
        {
            var intersection = new Point3dCollection();
            using (var line = new Line(ncadArc.StartPoint, ncadArc.EndPoint))
                line.IntersectWith(entity, intersectType, intersection, IntPtr.Zero, IntPtr.Zero);
            return intersection;
        }

        public override PointOnLine EvaluatePoint(double offset)
        {
            var trueOffset = Reversed ? 1.0 - offset : offset;
            return new PointOnLine {
                Point = ncadArc.GetPointAtParameter(
                    (1 - trueOffset) * ncadArc.StartParam +
                    trueOffset * ncadArc.EndParam),
                Parent = Parent
            };
        }

        public override void Dispose()
        {
            ncadArc.Dispose();
        }
    }

    public class UniversalLineSegment : UniversalCurveSegment
    {
        private readonly LineSegment3d ncadSegment;

        protected override Point3d OriginalStartPoint => ncadSegment.StartPoint;
        protected override Point3d OriginalEndPoint => ncadSegment.EndPoint;

        public override double Length => ncadSegment.Length;

        public UniversalLineSegment(LineSegment3d segment, SimpleUniversalLine parent) : base(parent)
        {
            ncadSegment = segment;
        }

        public override PointOnLine EvaluatePoint(double offset) =>
            new PointOnLine
            {
                Point = ncadSegment.EvaluatePoint(Reversed ? 1.0 - offset : offset),
                Parent = Parent
            };

        #region IUniversalLine

        public void SetStartPoint(Point3d startPoint)
        {
            if (Reversed)
                ncadSegment.Set(EndPoint, startPoint);
            else
                ncadSegment.Set(startPoint, EndPoint);
        }

        public void SetEndPoint(Point3d endPoint)
        {
            if (Reversed)
                ncadSegment.Set(endPoint, StartPoint);
            else
                ncadSegment.Set(StartPoint, endPoint);
        }

        public override Vector3d GetFirstDerivative(PointOnLine pointOnLine)
        {
            var derivative = ncadSegment.Direction;
            return Reversed ? -derivative : derivative;
        }

        public override Vector3d GetFirstDerivative(double offset)
        {
            var derivative = ncadSegment.Direction;
            return Reversed ? -derivative : derivative;
        }

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
