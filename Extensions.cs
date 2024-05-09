using Autodesk.Maya.OpenMaya;
using MdxLib.Primitives;
using System;
using System.Text.RegularExpressions;

namespace wc3ToMaya
{
    public static class DoubleExtensions
    {
        public static double ToDeg(this double rad)
        {
            return rad * 180.0 / Math.PI;
        }
    }
    public static class StringExtensions
    {
        public static string Standardize(this string s)
        {
            // Many names in Maya only support letters, numbers, and underscores. Some strings cannot start with a number

            string newS = s;
            if (char.IsDigit(newS[0]))
            {
                newS = newS.Insert(0, "i");
            }

            Regex rgx = new Regex("[^a-zA-Z0-9_]");
            newS = rgx.Replace(newS, "");

            if (string.IsNullOrEmpty(newS))
            {
                newS = "InvalidName";
            }

            return newS;
        }
    }


    public static class CVector3Extensions
    {
        private static readonly double scaleFactor = 30; // The vector will be reduced by the specified amount (if need)
        public static MVector ToMVector(this CVector3 vector, bool needScaling = true)
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
            double scale = needScaling ? scaleFactor : 1;
            return new MVector(-newX / scale, y / scale, newZ / scale);
        }

        public static MPoint ToMPoint(this CVector3 vector, bool needScaling = true)
        {
            // Warcraft 3 CVector3 to Maya MPoint
            // Same as ToMVector
            double scale = needScaling ? scaleFactor : 1;
            return new MPoint(-(vector.X * Math.Cos(Math.PI / 2) - vector.Y * Math.Sin(Math.PI / 2)) / scale, vector.Z / scale, (vector.X * Math.Sin(Math.PI / 2) + vector.Y * Math.Cos(Math.PI / 2)) / scale);
        }
    }
    public static class CVector4Extensions
    {
        public static MQuaternion ToMQuaternion(this CVector4 vector)
        {
            // Warcraft 3 CVector4 to Maya MQuaternion

            // Swap Y and Z
            double x = vector.X;
            double y = vector.Z;
            double z = vector.Y;
            double w = vector.W;

            // Rotate to pi/2 around Y
            double newX = x * Math.Cos(Math.PI / 2) - z * Math.Sin(Math.PI / 2);
            double newZ = x * Math.Sin(Math.PI / 2) + z * Math.Cos(Math.PI / 2);

            // Invert X
            return new MQuaternion(-newX, y, newZ, w);
        }
        public static MEulerRotation ToEuler(this CVector4 vector)
        {
            // Warcraft 3 CVector4 to Maya MEulerRotation

            MEulerRotation eulerRot = vector.ToMQuaternion().asEulerRotation;
            eulerRot.setValue(eulerRot.x.ToDeg(), eulerRot.y.ToDeg(), eulerRot.z.ToDeg());
            return eulerRot;
        }
    }
}
