using System;
using System.Runtime.InteropServices;

namespace terrainOptimizer.Helpers
{
    public class NativePolylineMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct RawPolylineArrays
        {
            public IntPtr Vertices;
            public int VerticesLength;
        }

        [DllImport("ClipperApi.dll")]
        public static extern RawPolylineArrays Offset3d(double[] coordinates, int numCoordinates, double delta, double miterLimit, int precision, bool simplify, double epsilon);

        [DllImport("ClipperApi.dll")]
        public static extern RawPolylineArrays Offset2d(double[] coordinates, int numCoordinates, double delta, double miterLimit, int precision, bool simplify, double epsilon);

        [DllImport("ClipperApi.dll")]
        public static extern RawPolylineArrays VariableOffset3d(double[] coordinates, int numCoordinates, double[] delta, double miterLimit, int precision, bool simplify, double epsilon);

    }
}
