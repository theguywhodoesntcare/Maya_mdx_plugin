using Autodesk.Maya.OpenMaya;
using MdxLib.Primitives;
using System;

namespace wc3ToMaya
{
    public static class CVector3Extensions
    {
        private static readonly double scaleFactor = 30; // The vector will be reduced by the specified amount
        public static MVector ToMVector(this CVector3 vector)
        {            
            // Warcraft 3 CVector3 to Maya MVector

            // Swap Y and Z
            double x = vector.X;
            double y = vector.Z;
            double z = vector.Y;

            // Rotate to pi/2 around Y
            double newX = x * Math.Cos(Math.PI / 2) - z * Math.Sin(Math.PI / 2);
            double newZ = x * Math.Sin(Math.PI / 2) + z * Math.Cos(Math.PI / 2);

            // Invert X
            // Apply scale factor (divisor)
            return new MVector(-newX / scaleFactor, y / scaleFactor, newZ / scaleFactor);
        }


        public static MPoint ToMPoint(this CVector3 vector)
        {
            // Warcraft 3 CVector3 to Maya MPoint
            // Same as ToMVector

            return new MPoint(-(vector.X * Math.Cos(Math.PI / 2) - vector.Y * Math.Sin(Math.PI / 2)) / scaleFactor, vector.Z / scaleFactor, (vector.X * Math.Sin(Math.PI / 2) + vector.Y * Math.Cos(Math.PI / 2)) / scaleFactor);
        }
    }
}
