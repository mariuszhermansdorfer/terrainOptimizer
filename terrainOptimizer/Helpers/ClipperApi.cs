using System;
using System.Runtime.InteropServices;

namespace terrainOptimizer.Helpers
{
    public class ClipperApi
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct RawPolylineArrays
        {
            public IntPtr Vertices;
            public int VerticesLength;
        }

        public enum Dim { TwoD, ThreeD };

        public enum JoinType { Square, Round, Miter };

        public enum EndType { Polygon, Joined, Butt, Square, Round };

        [DllImport("ClipperApi.dll")]
        public static extern IntPtr CreateClipperPath(double[] coordinates, int numCoordinates, Dim dim);
        
        [DllImport("ClipperApi.dll")]
        public static extern void DeleteClipperPath(IntPtr path);

        [DllImport("ClipperApi.dll")]
        public static extern RawPolylineArrays Simplify(IntPtr path, Dim dim, double epsilon);
        
        [DllImport("ClipperApi.dll")]
        public static extern RawPolylineArrays Offset(IntPtr path, Dim dim, double delta, double miterLimit, int precision, bool simplify, double epsilon, JoinType joinType, EndType endType);
        
        [DllImport("ClipperApi.dll")]
        public static extern RawPolylineArrays VariableOffset3d(IntPtr path, double[] delta, double miterLimit, int precision, bool simplify, double epsilon, JoinType joinType, EndType endType);

    }
}
