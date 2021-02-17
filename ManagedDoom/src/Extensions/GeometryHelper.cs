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
        
        public static bool GetLineIntersection(Vector2 v1, Vector2 v2, Vector2 v3, Vector2 v4, out float ray, bool bounded)
        {
            return GetLineIntersection(v1, v2, v3.X, v3.Y, v4.X, v4.Y, out ray, out _, bounded);
        }

        private static bool GetLineIntersection(Vector2 v1, Vector2 v2, float x3, float y3, float x4, float y4, out float ray, out float line, bool bounded)
        {
            // Calculate divider
            var div = (y4 - y3) * (v2.X - v1.X) - (x4 - x3) * (v2.Y - v1.Y);

            // Can this be tested?
            if(div != 0.0f)
            {
                // Calculate the intersection distance from the line
                line = ((x4 - x3) * (v1.Y - y3) - (y4 - y3) * (v1.X - x3)) / div;

                // Calculate the intersection distance from the ray
                ray = ((v2.X - v1.X) * (v1.Y - y3) - (v2.Y - v1.Y) * (v1.X - x3)) / div;

                // Return if intersecting
                if(bounded && (ray < 0.0f || ray > 1.0f || line < 0.0f || line > 1.0f)) return false; //mxd
                return true;
            }

            // Unable to detect intersection
            line = float.NaN;
            ray = float.NaN;
            return false;
        }
    }
}