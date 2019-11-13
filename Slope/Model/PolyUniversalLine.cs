using System;
using System.Collections.Generic;
using System.Linq;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace Slope.Model
{
    public class PolyUniversalLine : UniversalLine
    {
        private List<UniversalLine> Lines = new List<UniversalLine>();

        #region Public methods

        public bool TryAddSegment(UniversalLine line)
        {
            var allowance = Segments.LastOrDefault()?.Length * 0.1 ?? 0.0;

            if (!Lines.Any())
                Lines.Add(line);

            else if (VisuallyClosed || line.VisuallyClosed && Lines.Any())
                return false;

            else if (AlmostEqual(line.EndPoint, StartPoint, allowance))
                Lines.Insert(0, line);
            else if (AlmostEqual(line.StartPoint, EndPoint, allowance))
                Lines.Add(line);
            else if (AlmostEqual(line.StartPoint, StartPoint, allowance))
            {
                line.Reverse();
                Lines.Insert(0, line);
            }
            else if (AlmostEqual(line.EndPoint, EndPoint, allowance))
            {
                line.Reverse();
                Lines.Add(line);
            }
            else
                return false;

            return true;
        }

        public void ChopIntersections()
        {
            foreach (var pair in Lines.Zip(Lines.Skip(1),
                                           (prev, next) => new
                                           {
                                               prev = prev.Segments.Last(),
                                               next = next.Segments.First()
                                           }))
            {
                var intersection = pair.prev.IntersectWith(pair.next, Intersect.OnBothOperands);
                if (intersection.Count == 1)
                {
                    var point = intersection[0];
                    pair.prev.SetEndPoint(point);
                    pair.next.SetStartPoint(point);
                }
            }
        }

        #endregion

        #region IUniversalLine

        public override Point3d StartPoint => Lines.First().StartPoint;
        public override Point3d EndPoint => Lines.Last().EndPoint;
        public override bool VisuallyClosed => Lines.Count == 1 && Lines.First().VisuallyClosed || StartPoint == EndPoint;
        public override IReadOnlyCollection<UniversalLineSegment> Segments =>
            Lines.SelectMany(line => line.Segments).ToList().AsReadOnly();
        public override void Reverse()
        {
            Lines.Reverse();
            Lines.ForEach(line => line.Reverse());
        }

        public override bool Intersects(Entity entity, int allowedCount) =>
            Lines.Any(line => line.Intersects(entity, allowedCount));

        public override Vector3d GetFirstDerivative(PointOnLine pointOnLine) =>
            pointOnLine.Parent.GetFirstDerivative(pointOnLine);

        public override void Hightlight() =>
            Lines.ForEach(line => line.Hightlight());

        public override void Unhighlight() =>
            Lines.ForEach(line => line.Unhighlight());


        public override Point3dCollection IntersectWith(Entity entity, Intersect intersectType)
        {
            var intersections = new Point3dCollection();
            foreach (var line in Lines)
            {
                using (var lineIntersections = line.IntersectWith(entity, intersectType))
                {
                    foreach (Point3d intersection in lineIntersections)
                    {
                        intersections.Add(intersection);
                    }
                }
            }
            return intersections;
        }

        #endregion

        #region Private methods

        private static bool AlmostEqual(Point3d a, Point3d b, double allowance) =>
            a.GetVectorTo(b).LengthSqrd < (allowance * allowance);

        #endregion

        #region IDisposable

        public override void Dispose()
        {
            foreach (var line in Lines)
                line.Dispose();
            Lines.Clear();
        }

        #endregion
    }
}
