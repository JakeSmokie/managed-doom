using System;
using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace ManagedDoom.Extensions
{
    public static class GeometryHelper
    {
        public static bool LinesIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2 intersectionPoint)
        {
            var axd = a1.X - a2.X;
            var byd = b1.Y - b2.Y;
            var ayd = a1.Y - a2.Y;
            var bxd = b1.X - b2.X;
            var d = axd * byd - ayd * bxd;

            if (d > -float.Epsilon && d < float.Epsilon)
            {
                intersectionPoint = Vector2.Zero;
                return false;
            }

            var a = a1.X * a2.Y - a1.Y * a2.X;
            var b = b1.X * b2.Y - b1.Y * b2.X;
            var x = (a * bxd - b * axd) / d;
            var y = (a * byd - b * ayd) / d;
            
            intersectionPoint = new(x, y);

            return true;
            
            var aMinX = MathF.Min(a1.X, a2.X);
            var aMaxX = MathF.Max(a1.X, a2.X);
            var aMinY = MathF.Min(a1.Y, a2.Y);
            var aMaxY = MathF.Max(a1.Y, a2.Y);
            var bMinX = MathF.Min(b1.X, b2.X);
            var bMaxX = MathF.Max(b1.X, b2.X);
            var bMinY = MathF.Min(b1.Y, b2.Y);
            var bMaxY = MathF.Max(b1.Y, b2.Y);

            if (x >= aMinX && x >= bMinX
                && x <= aMaxX && x <= bMaxX
                && y >= aMinY && y >= bMinY
                && y <= aMaxY && y <= bMaxY)
            {
                return true;
            }

            return false;
        }
    }
}