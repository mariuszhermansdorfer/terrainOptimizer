using System;
using System.Runtime.InteropServices;


namespace MeshAPI
{
    public class Structs
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct RawMeshArrays
        {
            public IntPtr Faces;
            public int FacesLength;
            public IntPtr Vertices;
            public int VerticesLength;
            public IntPtr VertexValues;
            public int VertexValuesLength;
            public float Cut;
            public float Fill;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RawPolylineArrays
        {
            public IntPtr ContourVertices;
            public IntPtr ContourVerticesLengths;
            public int ContourCount;
            public IntPtr LabelVertices;
            public IntPtr LabelNormals;
            public int LabelCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RawMeshPointers
        {
            public IntPtr Faces;
            public int FacesLength;
            public IntPtr Vertices;
            public int VerticesLength;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct RawPolylinePointers
        {
            public IntPtr Vertices;
            public int VerticesLength;
        };

        public enum BooleanOperation
        {
            InsideA,
            InsideB,
            OutsideA,
            OutsideB,
            Union,
            Intersection,
            DifferenceBA,
            DifferenceAB
        }

        public enum CuttingOperation { DeleteInside, DeleteOutside, None }
    }
}
