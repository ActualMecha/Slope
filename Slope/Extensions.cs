using Slope.Model;
using System;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace Slope
{

    public static class Extensions
    {
        public static Side GetSide(this Vector3d forward, Vector3d direction)
        {
            var dot = new Vector2d(forward.Y, -forward.X).DotProduct(new Vector2d(direction.X, direction.Y));
            return dot > 0 ? Side.Left
                : dot == 0 ? Side.Front
                : Side.Right;
        }

        public static bool Intersects(this Entity entity, Entity other, int allowedCount)
        {
            using (var intersection = new Point3dCollection())
            {
                entity.IntersectWith(other, Intersect.OnBothOperands, intersection, IntPtr.Zero, IntPtr.Zero);
                return intersection.Count > allowedCount;
            }
        }
    }
}
